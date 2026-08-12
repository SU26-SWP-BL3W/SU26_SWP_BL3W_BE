using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public GetUserByIdQueryHandler(IUnitOfWork unitOfWork, SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UserModel?>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            // Kiểm duyệt quyền
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new SEAL_Domain.Base.BaseException.UnauthorizedException("Người dùng chưa đăng nhập.");
            }

            var isSelf = currentUserId == request.Id;
            if (!isSelf)
            {
                var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
                var isAdmin = currentUser?.IsAdmin == true;
                var isEventCoordinator = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                    er => er.UserId == currentUserId && er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator && (er.ExpiredAt == null || er.ExpiredAt > DateTime.UtcNow),
                    cancellationToken
                );

                if (!isAdmin && !isEventCoordinator)
                {
                    throw new SEAL_Domain.Base.BaseException.ForbiddenException("Bạn không có quyền xem thông tin người dùng này.");
                }
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Id);
            if (user == null) return null;

            return new UserModel
            {
                Id = user.Id,
                SchoolId = user.SchoolId,
                StudentCode = user.StudentCode,
                Email = user.Email,
                FullName = user.FullName,
                IsStudent = user.IsStudent,
                IsAdmin = user.IsAdmin,
                IsApproved = user.IsApproved,
                IsFpt = user.IsFpt,
                PhotoStudentCardUrl = user.PhotoStudentCardUrl
            };
        }
    }
}

