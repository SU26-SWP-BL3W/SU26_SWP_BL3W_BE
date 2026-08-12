using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Criterias.Commands.DeleteCriteria
{
    public class DeleteCriteriaCommandHandler : IRequestHandler<DeleteCriteriaCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public DeleteCriteriaCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(DeleteCriteriaCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Criteria cần xóa
            var criteria = await _unitOfWork.GetRepository<Criteria>().GetByIdAsync(request.Id);
            if (criteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí có ID '{request.Id}' không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isOwner = criteria.CreatedBy == currentUserId;

            if (!isAdmin && !isOwner)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa tiêu chí này.");
            }

            // Kiểm tra xem tiêu chí có đang nằm trong Template nào không
            var isUsedInTemplates = await _unitOfWork.GetRepository<TemplateCriteria>().AnyAsync(
                tc => tc.CriteriaId == request.Id, cancellationToken);
            
            if (isUsedInTemplates)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Tiêu chí này đang nằm trong một hoặc nhiều bộ tiêu chí (Template), không thể xóa. Hãy gỡ tiêu chí khỏi các bộ tiêu chí trước khi xóa.");
            }

            // 2. Xóa cứng thực thể Criteria (Cascade delete trên TrackCriterias đã được cấu hình trong EF Core)
            await _unitOfWork.GetRepository<Criteria>().DeleteAsync(criteria);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


