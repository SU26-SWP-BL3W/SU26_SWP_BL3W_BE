using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Commands.RemoveCriteriaFromTemplate
{
    public class RemoveCriteriaFromTemplateCommandHandler : IRequestHandler<RemoveCriteriaFromTemplateCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RemoveCriteriaFromTemplateCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(RemoveCriteriaFromTemplateCommand request, CancellationToken cancellationToken)
        {
            var templateCriteria = (await _unitOfWork.GetRepository<TemplateCriteria>().FindAsync(
                tc => tc.TemplateId == request.TemplateId && tc.CriteriaId == request.CriteriaId)).FirstOrDefault();

            if (templateCriteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí không tồn tại trong mẫu tiêu chí này.");
            }

            // ĐÓNG BĂNG: đã có phiếu chấm dùng bộ tiêu chí này -> bớt tiêu chí làm phiếu cũ
            // thành "thiếu tiêu chí" và thang điểm hệ 10 (tổng trọng số 100%) bị vỡ.
            var usedInScoring = await _unitOfWork.GetRepository<ScoreDetail>().AnyAsync(
                sd => sd.TemplateId == request.TemplateId, cancellationToken);
            if (usedInScoring)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Bộ tiêu chí đã được dùng để chấm điểm nên không thể bớt tiêu chí.");
            }

            // Đang được hạng mục sử dụng -> không cơ cấu lại (tổng trọng số phải giữ đúng 100%).
            var assignedToTrack = await _unitOfWork.GetRepository<Track>().AnyAsync(
                t => t.TemplateId == request.TemplateId, cancellationToken);
            if (assignedToTrack)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Bộ tiêu chí đang được hạng mục sử dụng nên không thể bớt tiêu chí. Hãy gỡ khỏi hạng mục trước khi cơ cấu lại.");
            }

            await _unitOfWork.GetRepository<TemplateCriteria>().DeleteAsync(templateCriteria);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

