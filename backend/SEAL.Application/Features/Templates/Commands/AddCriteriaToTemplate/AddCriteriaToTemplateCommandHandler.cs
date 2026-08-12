using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Commands.AddCriteriaToTemplate
{
    public class AddCriteriaToTemplateCommandHandler : IRequestHandler<AddCriteriaToTemplateCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AddCriteriaToTemplateCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(AddCriteriaToTemplateCommand request, CancellationToken cancellationToken)
        {
            var templateExists = await _unitOfWork.GetRepository<Template>().AnyAsync(
                t => t.Id == request.TemplateId, cancellationToken);
            if (!templateExists)
            {
                return BaseException.BadRequestNotFoundResponse($"Mẫu tiêu chí có ID '{request.TemplateId}' không tồn tại.");
            }

            var criteriaExists = await _unitOfWork.GetRepository<Criteria>().AnyAsync(
                c => c.Id == request.Model.CriteriaId, cancellationToken);
            if (!criteriaExists)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí có ID '{request.Model.CriteriaId}' không tồn tại.");
            }

            var duplicateBinding = await _unitOfWork.GetRepository<TemplateCriteria>().AnyAsync(
                tc => tc.TemplateId == request.TemplateId && tc.CriteriaId == request.Model.CriteriaId,
                cancellationToken);

            if (duplicateBinding)
            {
                return BaseException.BadRequestDupplicationResponse($"Tiêu chí đã tồn tại trong mẫu tiêu chí này.");
            }

            // ĐÓNG BĂNG: đã có phiếu chấm dùng bộ tiêu chí này -> thêm tiêu chí làm phiếu cũ
            // thành "thiếu tiêu chí" (SaveScore bắt chấm đúng đủ bộ) và thang hệ 10 bị vỡ.
            var usedInScoring = await _unitOfWork.GetRepository<ScoreDetail>().AnyAsync(
                sd => sd.TemplateId == request.TemplateId, cancellationToken);
            if (usedInScoring)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Bộ tiêu chí đã được dùng để chấm điểm nên không thể thêm tiêu chí.");
            }

            // Đang được hạng mục sử dụng -> không cơ cấu lại (tổng trọng số phải giữ đúng 100%).
            var assignedToTrack = await _unitOfWork.GetRepository<Track>().AnyAsync(
                t => t.TemplateId == request.TemplateId, cancellationToken);
            if (assignedToTrack)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Bộ tiêu chí đang được hạng mục sử dụng nên không thể thêm tiêu chí. Hãy gỡ khỏi hạng mục trước khi cơ cấu lại.");
            }

            // Kiểm tra tổng trọng số (Total Weight) không vượt quá 100
            var existingCriterias = await _unitOfWork.GetRepository<TemplateCriteria>()
                .FindAsync(tc => tc.TemplateId == request.TemplateId);
            
            var currentTotalWeight = existingCriterias.Sum(tc => tc.Weight);
            if (currentTotalWeight + request.Model.Weight > 100)
            {
                return BaseException.BadRequestResponse($"Tổng trọng số của mẫu tiêu chí đã vượt quá 100. Trọng số hiện tại là {currentTotalWeight}.");
            }

            var templateCriteria = new TemplateCriteria
            {
                TemplateId = request.TemplateId,
                CriteriaId = request.Model.CriteriaId,
                Weight = request.Model.Weight,
                MaxScore = request.Model.MaxScore
            };

            await _unitOfWork.GetRepository<TemplateCriteria>().AddAsync(templateCriteria);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

