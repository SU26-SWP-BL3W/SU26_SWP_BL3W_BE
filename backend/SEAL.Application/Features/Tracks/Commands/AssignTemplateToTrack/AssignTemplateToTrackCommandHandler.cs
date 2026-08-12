using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Tracks.Commands.AssignTemplateToTrack
{
    public class AssignTemplateToTrackCommandHandler : IRequestHandler<AssignTemplateToTrackCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AssignTemplateToTrackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(AssignTemplateToTrackCommand request, CancellationToken cancellationToken)
        {
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.TrackId);
            if (track == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Hạng mục có ID '{request.TrackId}' không tồn tại.");
            }

            var template = await _unitOfWork.GetRepository<Template>().GetByIdAsync(request.Model.TemplateId);
            if (template == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Mẫu tiêu chí có ID '{request.Model.TemplateId}' không tồn tại.");
            }

            var totalWeight = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                .Where(tc => tc.TemplateId == request.Model.TemplateId)
                .SumAsync(tc => (decimal?)tc.Weight, cancellationToken) ?? 0m;

            if (totalWeight != 100m)
            {
                return BaseException.BadRequestResponse($"Template '{template.TemplateName}' hiện có tổng trọng số là {totalWeight}%. Vui lòng cấu hình đủ 100% trước khi đưa vào sử dụng.");
            }

            track.TemplateId = request.Model.TemplateId;
            track.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<Track>().UpdateAsync(track);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

