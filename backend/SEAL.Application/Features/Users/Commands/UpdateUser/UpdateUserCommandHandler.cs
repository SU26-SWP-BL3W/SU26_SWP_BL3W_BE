using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Users.Commands.UpdateUser.Models;
using SEAL_Domain.Ultis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.UpdateUser
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public UpdateUserCommandHandler(IUnitOfWork unitOfWork, SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UpdateUserResponseModel>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            // 0. Kiểm duyệt quyền
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
                throw new SEAL_Domain.Base.BaseException.ForbiddenException("Chỉ Admin hoặc EventCoordinator mới có quyền cập nhật người dùng.");
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Id);
            if (user == null)
            {
                throw new Exception("User không tồn tại.");
            }

            // Guard chống tự khoá / mất admin cuối cùng của hệ thống.
            var isSelf = user.Id == currentUserId;
            if (isSelf && !request.Model.IsApproved)
            {
                throw SEAL_Domain.Base.BaseException.BadRequestInvaildInputResponse(
                    "Bạn không thể tự vô hiệu hoá tài khoản của chính mình.");
            }
            if (isSelf && currentUser?.IsAdmin == true && !request.Model.IsAdmin)
            {
                throw SEAL_Domain.Base.BaseException.BadRequestInvaildInputResponse(
                    "Bạn không thể tự gỡ quyền admin của chính mình.");
            }
            // Không để thao tác này gỡ quyền / vô hiệu hoá admin đang hoạt động CUỐI CÙNG
            // (áp cả khi người thao tác là EventCoordinator sửa tài khoản admin của người khác).
            if (user.IsAdmin && (!request.Model.IsAdmin || !request.Model.IsApproved))
            {
                var hasOtherActiveAdmin = await _unitOfWork.GetRepository<User>().AnyAsync(
                    u => u.IsAdmin && u.IsApproved && u.Id != user.Id, cancellationToken);
                if (!hasOtherActiveAdmin)
                {
                    throw SEAL_Domain.Base.BaseException.BadRequestInvaildInputResponse(
                        "Không thể gỡ quyền hoặc vô hiệu hoá admin cuối cùng của hệ thống.");
                }
            }

            user.SchoolId = request.Model.SchoolId;
            user.StudentCode = request.Model.StudentCode;
            user.FullName = request.Model.FullName;
            user.IsStudent = request.Model.IsStudent;
            user.IsAdmin = request.Model.IsAdmin;
            user.IsApproved = request.Model.IsApproved;
            user.IsFpt = request.Model.IsFpt;
            user.PhotoStudentCardUrl = request.Model.PhotoStudentCardUrl;
            user.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateUserResponseModel
            {
                Id = user.Id,
                SchoolId = user.SchoolId,
                StudentCode = user.StudentCode,
                FullName = user.FullName,
                IsStudent = user.IsStudent,
                IsAdmin = user.IsAdmin,
                IsApproved = user.IsApproved,
                LastUpdatedTime = user.LastUpdatedTime,
                IsFpt = user.IsFpt,
                PhotoStudentCardUrl = user.PhotoStudentCardUrl
            };
        }
    }
}

