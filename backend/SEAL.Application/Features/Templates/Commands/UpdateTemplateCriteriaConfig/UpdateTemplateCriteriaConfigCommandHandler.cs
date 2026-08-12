using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Commands.UpdateTemplateCriteriaConfig
{
    public class UpdateTemplateCriteriaConfigCommandHandler : IRequestHandler<UpdateTemplateCriteriaConfigCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTemplateCriteriaConfigCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(UpdateTemplateCriteriaConfigCommand request, CancellationToken cancellationToken)
        {
            var templateCriteria = (await _unitOfWork.GetRepository<TemplateCriteria>().FindAsync(
                tc => tc.TemplateId == request.TemplateId && tc.CriteriaId == request.CriteriaId)).FirstOrDefault();

            if (templateCriteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí không tồn tại trong mẫu tiêu chí này.");
            }

            // ĐÓNG BĂNG: đã có phiếu chấm dùng bộ tiêu chí này -> đổi Weight/MaxScore làm phiếu cũ
            // và phiếu mới nằm trên hai thang điểm khác nhau (điểm hệ 10 tính theo Weight/MaxScore).
            var usedInScoring = await _unitOfWork.GetRepository<ScoreDetail>().AnyAsync(
                sd => sd.TemplateId == request.TemplateId, cancellationToken);
            if (usedInScoring)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Bộ tiêu chí đã được dùng để chấm điểm nên không thể thay đổi trọng số/điểm tối đa.");
            }

            // Kiểm tra tổng trọng số (Total Weight) không vượt quá 100
            var existingCriterias = await _unitOfWork.GetRepository<TemplateCriteria>()
                .FindAsync(tc => tc.TemplateId == request.TemplateId);

            var currentTotalWeight = existingCriterias.Where(tc => tc.CriteriaId != request.CriteriaId).Sum(tc => tc.Weight);
            if (currentTotalWeight + request.Model.Weight > 100)
            {
                return BaseException.BadRequestResponse($"Tổng trọng số của mẫu tiêu chí đã vượt quá 100. Tổng trọng số các tiêu chí khác hiện là {currentTotalWeight}.");
            }

            // Đang được hạng mục sử dụng: SaveScore giả định tổng trọng số = 100% -> mọi thay đổi
            // phải giữ ĐÚNG 100% (muốn cơ cấu lại lớn thì gỡ template khỏi hạng mục trước).
            var assignedToTrack = await _unitOfWork.GetRepository<Track>().AnyAsync(
                t => t.TemplateId == request.TemplateId, cancellationToken);
            if (assignedToTrack && currentTotalWeight + request.Model.Weight != 100)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    $"Bộ tiêu chí đang được hạng mục sử dụng nên tổng trọng số sau thay đổi phải đúng 100% (các tiêu chí khác hiện là {currentTotalWeight}%).");
            }

            templateCriteria.Weight = request.Model.Weight;
            templateCriteria.MaxScore = request.Model.MaxScore;

            await _unitOfWork.GetRepository<TemplateCriteria>().UpdateAsync(templateCriteria);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

