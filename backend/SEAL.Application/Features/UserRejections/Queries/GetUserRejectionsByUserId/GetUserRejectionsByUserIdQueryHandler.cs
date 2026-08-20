using MediatR;
using SEAL_Application.Features.UserRejections;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId
{
    public class GetUserRejectionsByUserIdQueryHandler : IRequestHandler<GetUserRejectionsByUserIdQuery, Result<List<UserRejectionModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetUserRejectionsByUserIdQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<List<UserRejectionModel>>> Handle(GetUserRejectionsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Kiểm tra xem User có tồn tại không
            var userExist = await _unitOfWork.GetRepository<User>().AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExist)
            {
                return BaseException.BadRequestInvaildInputResponse($"Người dùng có ID '{request.UserId}' không tồn tại trong hệ thống.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isSelf = currentUserId == request.UserId;
            bool isPrivileged = currentUser != null && (
                currentUser.IsAdmin
                || await UserRejectionAccessHelper.IsCoordinatorForUserAsync(
                    _unitOfWork, _eventRoleChecker, currentUserId, request.UserId, cancellationToken));

            if (!isSelf && !isPrivileged)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xem lịch sử từ chối của người dùng này.");
            }

            // 2. Lấy lịch sử từ chối của User
            var rejections = await _unitOfWork.GetRepository<UserRejection>().FindAsync(ur => ur.UserId == request.UserId);

            // 3. Map sang Response Model
            return rejections.Select(ur => new UserRejectionModel
            {
                Id = ur.Id,
                UserId = ur.UserId,
                RejectedBy = ur.RejectedBy,
                Reason = ur.Reason,
                IsActive = ur.IsActive,
                CreatedTime = ur.CreatedTime
            }).ToList();
        }
    }
}



