using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.ApproveUser
{
    public class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, Result<UserModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public ApproveUserCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<UserModel>> Handle(ApproveUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra người gọi (caller)
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            // 2. Tìm người dùng cần duyệt
            var userToApprove = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Id);
            if (userToApprove == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng có ID '{request.Id}' không tồn tại.");
            }

            // 3. Tìm các vai trò trong sự kiện của người dùng cần duyệt
            var userRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.UserId == userToApprove.Id && er.TeamId != null)
                .ToListAsync(cancellationToken);

            var eventIds = userRoles.Select(er => er.EventId).Distinct().ToList();

            // 4. Kiểm tra quyền phê duyệt
            if (!isAdmin)
            {
                bool isCoordinatorOfAnyEvent = false;
                foreach (var eventId in eventIds)
                {
                    var isCoord = await _eventRoleChecker.HasRoleAsync(
                        currentUserId, eventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
                    if (isCoord)
                    {
                        isCoordinatorOfAnyEvent = true;
                        break;
                    }
                }

                if (!isCoordinatorOfAnyEvent)
                {
                    return new BaseException.ForbiddenException("Chỉ Admin hệ thống hoặc Event Coordinator liên quan mới được phép duyệt hồ sơ.");
                }
            }

            // 5. Duyệt người dùng
            userToApprove.IsApproved = true;
            userToApprove.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<User>().UpdateAsync(userToApprove);

            // 6. (ĐÃ BỎ) Trước đây duyệt tài khoản sẽ TỰ ĐỘNG đưa đội Forming -> Registered.
            //    Nay xét duyệt ở cấp ĐỘI THI: đội tự chốt danh sách (-> PendingApproval) rồi
            //    EC/Admin duyệt qua ApproveTeamRegistration. Giữ lại đây sẽ nhảy cóc bước duyệt đội.

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UserModel
            {
                Id = userToApprove.Id,
                SchoolId = userToApprove.SchoolId ?? string.Empty,
                StudentCode = userToApprove.StudentCode,
                Email = userToApprove.Email,
                FullName = userToApprove.FullName,
                IsStudent = userToApprove.IsStudent,
                IsAdmin = userToApprove.IsAdmin,
                IsApproved = userToApprove.IsApproved,
                IsFpt = userToApprove.IsFpt,
                PhotoStudentCardUrl = userToApprove.PhotoStudentCardUrl
            };
        }
    }
}


