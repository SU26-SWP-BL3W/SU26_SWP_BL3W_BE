using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<PagedResult<UserModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public GetAllUsersQueryHandler(IUnitOfWork unitOfWork, SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<UserModel>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            // Kiểm duyệt quyền
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new SEAL_Domain.Base.BaseException.UnauthorizedException("Người dùng chưa đăng nhập.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            var isAdmin = currentUser?.IsAdmin == true;
            var isEventCoordinator = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId && er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator && (er.ExpiredAt == null || er.ExpiredAt > DateTime.UtcNow),
                cancellationToken
            );

            if (!isAdmin && !isEventCoordinator)
            {
                throw new SEAL_Domain.Base.BaseException.ForbiddenException("Chỉ Admin hoặc EventCoordinator mới có quyền xem danh sách người dùng.");
            }

            var query = _unitOfWork.GetRepository<User>().GetQueryable();

            // Không đưa tài khoản Admin vào danh sách người dùng (ẩn thông tin admin).
            query = query.Where(u => !u.IsAdmin);

            // Áp dụng các bộ lọc
            if (request.IsApproved.HasValue)
            {
                query = query.Where(u => u.IsApproved == request.IsApproved.Value);
            }

            if (request.IsStudent.HasValue)
            {
                query = query.Where(u => u.IsStudent == request.IsStudent.Value);
            }

            if (request.IsFpt.HasValue)
            {
                query = query.Where(u => u.IsFpt == request.IsFpt.Value);
            }

            // Chỉ lấy user ĐÃ NỘP hồ sơ thí sinh (cho màn xét duyệt):
            // loại user chưa từng cập nhật hồ sơ + tài khoản mời (judge/mentor, IsStudent=false).
            // Bất biến "đã nộp hồ sơ" = IsStudent (chỉ UpdateStudentProfile set) + SchoolId (luôn bắt buộc).
            // KHÔNG đòi StudentCode/PhotoStudentCardUrl: MSSV chỉ bắt buộc với SV FPT, ảnh thẻ chỉ với SV ngoài FPT.
            if (request.HasSubmittedProfile.HasValue)
            {
                if (request.HasSubmittedProfile.Value)
                {
                    query = query.Where(u => u.IsStudent && u.SchoolId != null);
                }
                else
                {
                    query = query.Where(u => !u.IsStudent || u.SchoolId == null);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(searchTerm) || 
                                         u.Email.ToLower().Contains(searchTerm) || 
                                         (u.StudentCode != null && u.StudentCode.ToLower().Contains(searchTerm)));
            }

           if (!string.IsNullOrWhiteSpace(request.EventId))
            {
                query = query.Where(u => u.EventRoles.Any(er => er.EventId == request.EventId));
            }

            return await query.ToPagedResultAsync(
                request: request,
                selector: u => new UserModel
                {
                    Id = u.Id,
                    SchoolId = u.SchoolId,
                    StudentCode = u.StudentCode,
                    Email = u.Email,
                    FullName = u.FullName,
                    IsStudent = u.IsStudent,
                    IsAdmin = u.IsAdmin,
                    IsApproved = u.IsApproved,
                    IsFpt = u.IsFpt,
                    IsRejected = u.UserRejections.Any(ur => ur.IsActive),
                    IsTemporary = u.IsTemporary,
                    PhotoStudentCardUrl = u.PhotoStudentCardUrl
                },
                cancellationToken: cancellationToken
            );
        }
    }
}



