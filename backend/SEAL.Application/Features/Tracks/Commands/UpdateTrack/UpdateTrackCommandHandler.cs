using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Tracks.Commands.UpdateTrack.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SEAL_Application.Features.Tracks.Commands.UpdateTrack
{
    public class UpdateTrackCommandHandler : IRequestHandler<UpdateTrackCommand, Result<UpdateTrackResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTrackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateTrackResponseModel>> Handle(UpdateTrackCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Track cần cập nhật
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.Id);
            if (track == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Hạng mục có ID '{request.Id}' không tồn tại.");
            }

            // 2. Kiểm tra Event có tồn tại không
            var targetEventId = string.IsNullOrEmpty(request.Model.EventId) ? track.EventId : request.Model.EventId;
            var parentEvent = await _unitOfWork.GetRepository<Event>().GetByIdAsync(targetEventId);
            if (parentEvent == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{targetEventId}' không tồn tại.");
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

            // 2b. Kiểm tra ĐỔI EVENT: Đã có bài nộp thì cấm đổi Event
            if (!string.IsNullOrEmpty(request.Model.EventId) && request.Model.EventId != track.EventId)
            {
                var trackHasSubmissions = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                    sr => sr.TrackId == track.Id, cancellationToken);
                if (trackHasSubmissions)
                {
                    return BaseException.BadRequestInvaildInputResponse("Hạng mục đã có bài nộp nên không thể chuyển sang sự kiện khác.");
                }
            }

            // 2c. Đã có phiếu chấm trên hạng mục -> cấm đổi bộ tiêu chí (chỉ kiểm tra khi FE có gửi TemplateId mới)
            if (request.Model.TemplateId != null && request.Model.TemplateId != track.TemplateId)
            {
                var trackHasScores = await _unitOfWork.GetRepository<Score>().AnyAsync(
                    s => s.SubmitResult.TrackId == track.Id, cancellationToken);
                if (trackHasScores)
                {
                    return BaseException.BadRequestInvaildInputResponse("Hạng mục đã có phiếu chấm nên không thể đổi bộ tiêu chí.");
                }
            }

            // 3. Kiểm tra trùng tên Track trong cùng một Event (loại trừ chính nó)
            if (!string.IsNullOrWhiteSpace(request.Model.TrackName))
            {
                var isDuplicate = await _unitOfWork.GetRepository<Track>().AnyAsync(
                    t => t.TrackName.ToLower() == request.Model.TrackName.ToLower() && t.EventId == targetEventId && t.Id != request.Id,
                    cancellationToken);

                if (isDuplicate)
                {
                    return BaseException.BadRequestDupplicationResponse($"Hạng mục '{request.Model.TrackName}' đã tồn tại trong sự kiện này.");
                }
            }

            // 4. Kiểm tra Template nếu có gửi
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

            // 5. Cập nhật thông tin (chỉ cập nhật những trường được gửi lên, tránh xóa nhầm dữ liệu)
            if (!string.IsNullOrEmpty(request.Model.EventId)) track.EventId = request.Model.EventId;
            if (!string.IsNullOrWhiteSpace(request.Model.TrackName)) track.TrackName = request.Model.TrackName;
            if (request.Model.TemplateId != null) track.TemplateId = request.Model.TemplateId;
            if (request.Model.Description != null) track.Description = request.Model.Description;
            if (request.Model.SubmissionRuleDescription != null) track.SubmissionRuleDescription = request.Model.SubmissionRuleDescription;
            if (request.Model.StartDate.HasValue) track.StartDate = request.Model.StartDate.Value.ToUniversalTime();
            if (request.Model.EndDate.HasValue) track.EndDate = request.Model.EndDate.Value.ToUniversalTime();
            if (request.Model.ScoringStartDate.HasValue) track.ScoringStartDate = request.Model.ScoringStartDate.Value.ToUniversalTime();
            if (request.Model.ScoringEndDate.HasValue) track.ScoringEndDate = request.Model.ScoringEndDate.Value.ToUniversalTime();
            track.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // 6. Lưu vào Database
            await _unitOfWork.GetRepository<Track>().UpdateAsync(track);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateTrackResponseModel
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
                LastUpdatedTime = track.LastUpdatedTime
            };
        }
    }
}
