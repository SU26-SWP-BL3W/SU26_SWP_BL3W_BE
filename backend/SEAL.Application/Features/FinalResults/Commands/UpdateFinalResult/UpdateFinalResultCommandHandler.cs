using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult
{
    public class UpdateFinalResultCommandHandler : IRequestHandler<UpdateFinalResultCommand, Result<UpdateFinalResultResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateFinalResultCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateFinalResultResponseModel>> Handle(UpdateFinalResultCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm FinalResult cần cập nhật
            var finalResult = await _unitOfWork.GetRepository<FinalResult>().GetByIdAsync(request.Id);
            if (finalResult == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Kết quả chung cuộc có ID '{request.Id}' không tồn tại.");
            }

            if (finalResult.IsPublished)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Kết quả đã công bố không thể sửa thủ công. Hãy hủy công bố hoặc tính lại kết quả.");
            }

            // 2. Kiểm tra Round mới có tồn tại
            var newRound = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.Model.RoundId);
            if (newRound == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.Model.RoundId}' không tồn tại.");
            }

            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.Model.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Đội thi '{request.Model.TeamId}' không tồn tại.");
            }
            if (team.EventId != newRound.EventId)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Đội thi không thuộc cùng sự kiện với vòng thi được chọn.");
            }

            // 2b. Round mới phải CÙNG SỰ KIỆN với kết quả đang sửa — không cho chuyển kết quả sang
            //     vòng thi thuộc sự kiện khác (làm đội lạ lọt vào bảng xếp hạng sự kiện khác).
            if (!string.IsNullOrEmpty(finalResult.RoundId) && finalResult.RoundId != request.Model.RoundId)
            {
                var oldRound = await _unitOfWork.GetRepository<Round>().GetByIdAsync(finalResult.RoundId);
                if (oldRound != null && oldRound.EventId != newRound.EventId)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Không thể chuyển kết quả sang vòng thi thuộc sự kiện khác.");
                }
            }

            // 3. Kiểm tra trùng Team trong Round (loại trừ chính nó)
            var isDuplicate = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                f => f.TeamId == request.Model.TeamId
                  && f.RoundId == request.Model.RoundId
                  && f.Id != request.Id,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Đội thi '{request.Model.TeamId}' đã có kết quả chung cuộc trong vòng thi này.");
            }

            // 4. Cập nhật
            finalResult.TeamId = request.Model.TeamId;
            finalResult.RoundId = request.Model.RoundId;
            finalResult.FinalScore = request.Model.FinalScore;
            finalResult.Rank = request.Model.Rank;
            finalResult.IsAdvanced = request.Model.IsAdvanced;
            finalResult.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<FinalResult>().UpdateAsync(finalResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateFinalResultResponseModel
            {
                Id = finalResult.Id,
                TeamId = finalResult.TeamId,
                RoundId = finalResult.RoundId,
                FinalScore = finalResult.FinalScore,
                Rank = finalResult.Rank,
                IsAdvanced = finalResult.IsAdvanced,
                LastUpdatedTime = finalResult.LastUpdatedTime
            };
        }
    }
}

