using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Tracks.Commands.CreateTrack.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Tracks.Commands.CreateTrack
{
    public class CreateTrackCommandHandler : IRequestHandler<CreateTrackCommand, Result<CreateTrackResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateTrackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateTrackResponseModel>> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Round có tồn tại không
            var round = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == request.Model.RoundId, cancellationToken);

            if (round == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.Model.RoundId}' không tồn tại.");
            }

            var parentEvent = round.Event;
            if (parentEvent != null)
            {
                if (request.Model.StartDate.HasValue && request.Model.StartDate.Value.ToUniversalTime() < parentEvent.StartDate)
                    return BaseException.BadRequestResponse($"Thời gian bắt đầu nộp bài không được trước ngày bắt đầu sự kiện ({parentEvent.StartDate:dd/MM/yyyy}).");
                if (request.Model.EndDate.HasValue && request.Model.EndDate.Value.ToUniversalTime() > parentEvent.EndDate)
                    return BaseException.BadRequestResponse($"Hạn nộp bài không được vượt quá ngày kết thúc sự kiện ({parentEvent.EndDate:dd/MM/yyyy}).");
                if (request.Model.ScoringStartDate.HasValue && request.Model.ScoringStartDate.Value.ToUniversalTime() > parentEvent.EndDate)
                    return BaseException.BadRequestResponse($"Thời gian bắt đầu chấm không được vượt quá ngày kết thúc sự kiện ({parentEvent.EndDate:dd/MM/yyyy}).");
                if (request.Model.ScoringEndDate.HasValue && request.Model.ScoringEndDate.Value.ToUniversalTime() > parentEvent.EndDate)
                    return BaseException.BadRequestResponse($"Hạn chót chấm điểm không được vượt quá ngày kết thúc sự kiện ({parentEvent.EndDate:dd/MM/yyyy}).");
            }

            // 2. Kiểm tra trùng tên Track trong cùng một Round
            var isDuplicate = await _unitOfWork.GetRepository<Track>().AnyAsync(
                t => t.TrackName.ToLower() == request.Model.TrackName.ToLower() && t.RoundId == request.Model.RoundId,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Hạng mục '{request.Model.TrackName}' đã tồn tại trong vòng thi này.");
            }

            // 3. Kiểm tra Template nếu có
            if (!string.IsNullOrEmpty(request.Model.TemplateId))
            {
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
            }

            var track = new Track
            {
                RoundId = request.Model.RoundId,
                TemplateId = request.Model.TemplateId,
                TrackName = request.Model.TrackName,
                Description = request.Model.Description,
                StartDate = request.Model.StartDate?.ToUniversalTime(),
                EndDate = request.Model.EndDate?.ToUniversalTime(),
                ScoringStartDate = request.Model.ScoringStartDate?.ToUniversalTime(),
                ScoringEndDate = request.Model.ScoringEndDate?.ToUniversalTime()
            };

            await _unitOfWork.GetRepository<Track>().AddAsync(track);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateTrackResponseModel
            {
                Id = track.Id,
                RoundId = track.RoundId,
                TemplateId = track.TemplateId,
                TrackName = track.TrackName,
                Description = track.Description,
                StartDate = track.StartDate,
                EndDate = track.EndDate,
                ScoringStartDate = track.ScoringStartDate,
                ScoringEndDate = track.ScoringEndDate,
                CreatedTime = track.CreatedTime
            };
        }
    }
}

