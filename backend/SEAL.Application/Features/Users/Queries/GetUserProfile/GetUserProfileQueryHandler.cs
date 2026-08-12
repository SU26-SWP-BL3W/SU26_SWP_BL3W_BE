using MediatR;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Queries.GetUserProfile
{
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetUserProfileQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UserModel>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Người dùng chưa đăng nhập hoặc token không hợp lệ.");
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy thông tin tài khoản.");
            }

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


