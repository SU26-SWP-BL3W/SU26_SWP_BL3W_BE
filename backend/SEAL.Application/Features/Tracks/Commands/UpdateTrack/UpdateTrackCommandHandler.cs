using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Tracks.Commands.UpdateTrack.Models;
using System.Threading;
using System.Threading.Tasks;

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

            // 2. Kiểm tra Round mới có tồn tại không
            var newRound = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == request.Model.RoundId, cancellationToken);
            if (newRound == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.Model.RoundId}' không tồn tại.");
            }

            var parentEvent = newRound.Event;
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

            // 2b. ĐỔI VÒNG của hạng mục bị siết: bài nộp/deadline/chuỗi đi tiếp đều bám theo vòng.
            //     - Không được chuyển sang vòng của SỰ KIỆN khác (bứng cả bài nộp lệch sự kiện).
            //     - Đã có bài nộp thì không chuyển vòng (deadline/chống trùng/đi tiếp loạn hết).
            if (request.Model.RoundId != track.RoundId)
            {
                var oldRound = await _unitOfWork.GetRepository<Round>().GetByIdAsync(track.RoundId);
                if (oldRound != null && newRound.EventId != oldRound.EventId)
                {
                    return BaseException.BadRequestInvaildInputResponse("Không thể chuyển hạng mục sang vòng của sự kiện khác.");
                }
                var trackHasSubmissions = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                    sr => sr.TrackId == track.Id, cancellationToken);
                if (trackHasSubmissions)
                {
                    return BaseException.BadRequestInvaildInputResponse("Hạng mục đã có bài nộp nên không thể chuyển sang vòng khác.");
                }
            }

            // 2c. Đã có phiếu chấm trên hạng mục -> cấm đổi bộ tiêu chí (phiếu cũ chấm theo template cũ,
            //     phiếu mới theo template mới -> hai thang điểm trộn lẫn).
            if (track.TemplateId != request.Model.TemplateId)
            {
                var trackHasScores = await _unitOfWork.GetRepository<Score>().AnyAsync(
                    s => s.SubmitResult.TrackId == track.Id, cancellationToken);
                if (trackHasScores)
                {
                    return BaseException.BadRequestInvaildInputResponse("Hạng mục đã có phiếu chấm nên không thể đổi bộ tiêu chí.");
                }
            }

            // 3. Kiểm tra trùng tên Track trong cùng một Round (loại trừ chính nó)
            var isDuplicate = await _unitOfWork.GetRepository<Track>().AnyAsync(
                t => t.TrackName.ToLower() == request.Model.TrackName.ToLower() && t.RoundId == request.Model.RoundId && t.Id != request.Id,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Hạng mục '{request.Model.TrackName}' đã tồn tại trong vòng thi này.");
            }

            // 4. Kiểm tra Template nếu có
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

            // 5. Cập nhật thông tin
            track.RoundId = request.Model.RoundId;
            track.TemplateId = request.Model.TemplateId;
            track.TrackName = request.Model.TrackName;
            track.Description = request.Model.Description;
            track.SubmissionRuleDescription = request.Model.SubmissionRuleDescription;
            track.StartDate = request.Model.StartDate?.ToUniversalTime();
            track.EndDate = request.Model.EndDate?.ToUniversalTime();
            track.ScoringStartDate = request.Model.ScoringStartDate?.ToUniversalTime();
            track.ScoringEndDate = request.Model.ScoringEndDate?.ToUniversalTime();
            track.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // 6. Lưu vào Database
            await _unitOfWork.GetRepository<Track>().UpdateAsync(track);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateTrackResponseModel
            {
                Id = track.Id,
                RoundId = track.RoundId,
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

