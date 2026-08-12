using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Users.Commands.CreateUser.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Users.Commands.CreateUser
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public CreateUserCommandHandler(IUnitOfWork unitOfWork, SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CreateUserResponseModel>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // 0. Kiểm duyệt quyền Admin
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new SEAL_Domain.Base.BaseException.UnauthorizedException("Người dùng chưa đăng nhập.");
            }
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                throw new SEAL_Domain.Base.BaseException.ForbiddenException("Chỉ Admin mới có quyền tạo người dùng trực tiếp.");
            }

            // 1. Kiểm tra email tồn tại
            var isEmailExist = await _unitOfWork.GetRepository<User>().AnyAsync(
                u => u.Email.ToLower() == request.Model.Email.ToLower(),
                cancellationToken);
            if (isEmailExist)
            {
                return BaseException.BadRequestDupplicationResponse($"Email '{request.Model.Email}' đã tồn tại trong hệ thống.");
            }

            // 2. Tạo user (Admin tạo nên lưu thông tin nhập trực tiếp và mặc định Active)
            var user = new User
            {
                SchoolId = request.Model.SchoolId,
                StudentCode = request.Model.StudentCode,
                Email = request.Model.Email,
                PasswordHash = FixedSaltPasswordHasher.HashPassword(request.Model.Password),
                FullName = request.Model.FullName,
                IsStudent = request.Model.IsStudent,
                IsAdmin = request.Model.IsAdmin,
                IsApproved = true, // Admin tạo nên mặc định duyệt luôn
                IsEmailVerified = true, // Admin tạo nên mặc định xác thực luôn
                IsFpt = request.Model.IsFpt,
                PhotoStudentCardUrl = request.Model.PhotoStudentCardUrl
            };

            await _unitOfWork.GetRepository<User>().AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateUserResponseModel
            {
                Id = user.Id,
                SchoolId = user.SchoolId,
                StudentCode = user.StudentCode,
                Email = user.Email,
                FullName = user.FullName,
                IsStudent = user.IsStudent,
                IsAdmin = user.IsAdmin,
                CreatedTime = user.CreatedTime,
                IsFpt = user.IsFpt,
                PhotoStudentCardUrl = user.PhotoStudentCardUrl
            };
        }
    }
}
