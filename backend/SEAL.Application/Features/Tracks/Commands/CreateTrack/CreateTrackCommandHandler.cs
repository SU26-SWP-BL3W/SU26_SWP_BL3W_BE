using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Tracks.Commands.CreateTrack.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
            // 1. Kiểm tra Event có tồn tại không
            var parentEvent = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.Model.EventId);
            if (parentEvent == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{request.Model.EventId}' không tồn tại.");
            }

            if (request.Model.StartDate.HasValue)
            {
                var st = request.Model.StartDate.Value.ToUniversalTime();
                if (st < parentEvent.StartDate || st > parentEvent.EndDate)
                    return BaseException.BadRequestResponse($"Thời gian bắt đầu nộp bài phải nằm trong khoảng diễn ra sự kiện ({parentEvent.StartDate:dd/MM/yyyy} - {parentEvent.EndDate:dd/MM/yyyy}).");
            }
            if (request.Model.EndDate.HasValue)
            {
                var et = request.Model.EndDate.Value.ToUniversalTime();
                if (et < parentEvent.StartDate || et > parentEvent.EndDate)
                    return BaseException.BadRequestResponse($"Hạn nộp bài phải nằm trong khoảng diễn ra sự kiện ({parentEvent.StartDate:dd/MM/yyyy} - {parentEvent.EndDate:dd/MM/yyyy}).");
            }
            if (request.Model.ScoringStartDate.HasValue)
            {
                var sst = request.Model.ScoringStartDate.Value.ToUniversalTime();
                if (sst < parentEvent.StartDate || sst > parentEvent.EndDate)
                    return BaseException.BadRequestResponse($"Thời gian bắt đầu chấm phải nằm trong khoảng diễn ra sự kiện ({parentEvent.StartDate:dd/MM/yyyy} - {parentEvent.EndDate:dd/MM/yyyy}).");
            }
            if (request.Model.ScoringEndDate.HasValue)
            {
                var set = request.Model.ScoringEndDate.Value.ToUniversalTime();
                if (set < parentEvent.StartDate || set > parentEvent.EndDate)
                    return BaseException.BadRequestResponse($"Hạn chót chấm điểm phải nằm trong khoảng diễn ra sự kiện ({parentEvent.StartDate:dd/MM/yyyy} - {parentEvent.EndDate:dd/MM/yyyy}).");
            }

            // 2. Kiểm tra trùng tên Track trong cùng một Event
            var isDuplicate = await _unitOfWork.GetRepository<Track>().AnyAsync(
                t => t.TrackName.ToLower() == request.Model.TrackName.ToLower() && t.EventId == request.Model.EventId,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Hạng mục '{request.Model.TrackName}' đã tồn tại trong sự kiện này.");
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
                EventId = request.Model.EventId,
                TemplateId = request.Model.TemplateId,
                TrackName = request.Model.TrackName,
                Description = request.Model.Description,
                SubmissionRuleDescription = request.Model.SubmissionRuleDescription,
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
                EventId = track.EventId,
                TemplateId = track.TemplateId,
                TrackName = track.TrackName,
                Description = track.Description,
                SubmissionRuleDescription = track.SubmissionRuleDescription,
                StartDate = track.StartDate,
                EndDate = track.EndDate,
                ScoringStartDate = track.ScoringStartDate,
                ScoringEndDate = track.ScoringEndDate,
                CreatedTime = track.CreatedTime
            };
        }
    }
}
