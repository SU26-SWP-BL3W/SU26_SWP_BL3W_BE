using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Schools.Commands.DeleteSchool
{
    public class DeleteSchoolCommandHandler : IRequestHandler<DeleteSchoolCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public DeleteSchoolCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(DeleteSchoolCommand request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.GetRepository<School>();
            var school = await repository.GetByIdAsync(request.Id);
            
            if (school == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy trường học");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isOwner = school.CreatedBy == currentUserId;

            if (!isAdmin && !isOwner)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa trường học này.");
            }

            // Kiểm tra xem có User nào đang thuộc School này không
            var hasUsers = await _unitOfWork.GetRepository<User>().AnyAsync(u => u.SchoolId == request.Id, cancellationToken);
            if (hasUsers)
            {
                return BaseException.BadRequestResponse("Không thể xóa trường học này vì đang có sinh viên tham gia.");
            }

            await repository.DeleteAsync(school);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


