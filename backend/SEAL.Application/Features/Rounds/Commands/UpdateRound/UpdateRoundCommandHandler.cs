using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Rounds.Commands.UpdateRound.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Rounds.Commands.UpdateRound
{
    public class UpdateRoundCommandHandler : IRequestHandler<UpdateRoundCommand, Result<UpdateRoundResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateRoundCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateRoundResponseModel>> Handle(UpdateRoundCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Round cần cập nhật
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.Id);
            if (round == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.Id}' không tồn tại.");
            }

            // 2. Kiểm tra Event có tồn tại không và lấy thông tin thời gian
            var parentEvent = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.Model.EventId);
            if (parentEvent == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{request.Model.EventId}' không tồn tại.");
            }

            // KHÔNG cho chuyển vòng thi sang sự kiện khác — kéo theo track/bài nộp/kết quả lệch sự kiện.
            if (round.EventId != request.Model.EventId)
            {
                return BaseException.BadRequestInvaildInputResponse("Không thể chuyển vòng thi sang sự kiện khác.");
            }

            var startDateUtc = request.Model.StartDate.ToUniversalTime();
            var endDateUtc = request.Model.EndDate.ToUniversalTime();

            // Thời gian bắt đầu phải TRƯỚC thời gian kết thúc.
            if (startDateUtc >= endDateUtc)
            {
                return BaseException.BadRequestResponse("Thời gian bắt đầu vòng thi phải trước thời gian kết thúc.");
            }

            // Ràng buộc thời gian với Event
            if (startDateUtc < parentEvent.StartDate || endDateUtc > parentEvent.EndDate)
            {
                return BaseException.BadRequestResponse($"Thời gian diễn ra vòng thi phải nằm trong khoảng thời gian của sự kiện ({parentEvent.StartDate:dd/MM/yyyy HH:mm} - {parentEvent.EndDate:dd/MM/yyyy HH:mm}).");
            }

            // Ràng buộc cửa sổ chấm điểm đối chiếu với Sự kiện (Event)
            if (request.Model.ScoringStartDate.HasValue && request.Model.ScoringStartDate.Value.ToUniversalTime() > parentEvent.EndDate)
            {
                return BaseException.BadRequestResponse("Thời gian bắt đầu chấm không được vượt quá thời gian kết thúc sự kiện.");
            }
            if (request.Model.ScoringEndDate.HasValue && request.Model.ScoringEndDate.Value.ToUniversalTime() > parentEvent.EndDate)
            {
                return BaseException.BadRequestResponse("Hạn chấm không được vượt quá thời gian kết thúc sự kiện.");
            }

            // SỬA HỒI TỐ: mức độ khóa tăng dần theo dữ liệu đã phát sinh trên vòng.
            var isPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                fr => fr.RoundId == round.Id, cancellationToken);
            var hasScores = await _unitOfWork.GetRepository<Score>().AnyAsync(
                s => s.SubmitResult.RoundId == round.Id, cancellationToken);
            var hasSubmissions = hasScores || await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                sr => sr.RoundId == round.Id, cancellationToken);

            bool timeChanged = round.StartDate != startDateUtc || round.EndDate != endDateUtc;
            bool numberChanged = round.RoundNumber != request.Model.RoundNumber;
            bool scoringTimeChanged = round.ScoringStartDate != request.Model.ScoringStartDate?.ToUniversalTime()
                                   || round.ScoringEndDate != request.Model.ScoringEndDate?.ToUniversalTime();

            if (isPublished && (timeChanged || numberChanged || scoringTimeChanged))
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Kết quả vòng thi đã được tính/công bố nên không thể đổi thời gian hoặc số thứ tự vòng.");
            }
            if (hasScores && (timeChanged || scoringTimeChanged))
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vòng thi đã có phiếu chấm nên không thể đổi thời gian diễn ra hoặc thời gian chấm điểm.");
            }
            if (hasSubmissions)
            {
                if (numberChanged)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Vòng thi đã có bài nộp nên không thể đổi số thứ tự vòng (phá chuỗi xét đi tiếp).");
                }
                // Chỉ được NỚI RỘNG cửa sổ, không được thu hẹp (bài đã nộp hợp lệ bỗng thành ngoài hạn).
                if (startDateUtc > round.StartDate || endDateUtc < round.EndDate)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Vòng thi đã có bài nộp nên chỉ có thể nới rộng thời gian, không thể thu hẹp.");
                }
            }

            // 3. Kiểm tra trùng lặp RoundName trong cùng Event (loại trừ chính nó)
            var isDuplicateName = await _unitOfWork.GetRepository<Round>().AnyAsync(
                r => r.RoundName.ToLower() == request.Model.RoundName.ToLower() && r.EventId == request.Model.EventId && r.Id != request.Id,
                cancellationToken);

            if (isDuplicateName)
            {
                return BaseException.BadRequestDupplicationResponse($"Vòng thi '{request.Model.RoundName}' đã tồn tại trong sự kiện này.");
            }

            // Kiểm tra trùng lặp RoundNumber trong cùng Event (loại trừ chính nó)
            var isDuplicateNumber = await _unitOfWork.GetRepository<Round>().AnyAsync(
                r => r.RoundNumber == request.Model.RoundNumber && r.EventId == request.Model.EventId && r.Id != request.Id,
                cancellationToken);

            if (isDuplicateNumber)
            {
                return BaseException.BadRequestDupplicationResponse($"Số thứ tự vòng thi '{request.Model.RoundNumber}' đã tồn tại trong sự kiện này.");
            }

            // 4. Cập nhật thông tin
            round.EventId = request.Model.EventId;
            round.RoundName = request.Model.RoundName;
            round.RoundNumber = request.Model.RoundNumber;
            round.StartDate = request.Model.StartDate.ToUniversalTime();
            round.EndDate = request.Model.EndDate.ToUniversalTime();
            round.AdvancementRule = request.Model.AdvancementRule;
            round.ScoringStartDate = request.Model.ScoringStartDate?.ToUniversalTime();
            round.ScoringEndDate = request.Model.ScoringEndDate?.ToUniversalTime();
            round.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // 5. Lưu database
            await _unitOfWork.GetRepository<Round>().UpdateAsync(round);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateRoundResponseModel
            {
                Id = round.Id,
                EventId = round.EventId,
                RoundName = round.RoundName,
                RoundNumber = round.RoundNumber,
                StartDate = round.StartDate,
                EndDate = round.EndDate,
                AdvancementRule = round.AdvancementRule,
                ScoringStartDate = round.ScoringStartDate,
                ScoringEndDate = round.ScoringEndDate,
                LastUpdatedTime = round.LastUpdatedTime
            };
        }
    }
}

