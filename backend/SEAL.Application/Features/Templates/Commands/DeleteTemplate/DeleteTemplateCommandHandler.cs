using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Commands.DeleteTemplate
{
    public class DeleteTemplateCommandHandler : IRequestHandler<DeleteTemplateCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public DeleteTemplateCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(DeleteTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _unitOfWork.GetRepository<Template>().GetByIdAsync(request.Id);
            if (template == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Mẫu tiêu chí có ID '{request.Id}' không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isOwner = template.CreatedBy == currentUserId;

            if (!isAdmin && !isOwner)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa mẫu tiêu chí này.");
            }

            // Đang được hạng mục sử dụng -> không xóa (hạng mục mất bộ tiêu chí giữa chừng, không chấm được).
            var assignedToTrack = await _unitOfWork.GetRepository<Track>().AnyAsync(
                t => t.TemplateId == request.Id, cancellationToken);
            if (assignedToTrack)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Mẫu tiêu chí đang được hạng mục sử dụng nên không thể xóa. Hãy gỡ khỏi hạng mục trước.");
            }

            // Đã có phiếu chấm dùng mẫu này -> không xóa (điểm chi tiết mất tham chiếu tiêu chí gốc).
            var usedInScoring = await _unitOfWork.GetRepository<ScoreDetail>().AnyAsync(
                sd => sd.TemplateId == request.Id, cancellationToken);
            if (usedInScoring)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Mẫu tiêu chí đã được dùng để chấm điểm nên không thể xóa.");
            }

            await _unitOfWork.GetRepository<Template>().DeleteAsync(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


