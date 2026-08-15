using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.AssignPrize
{
    public class AssignPrizeCommandHandler : IRequestHandler<AssignPrizeCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AssignPrizeCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(AssignPrizeCommand request, CancellationToken cancellationToken)
        {
            var result = await _unitOfWork.GetRepository<FinalResult>().GetByIdAsync(request.FinalResultId);
            if (result == null) return BaseException.BadRequestNotFoundResponse("FinalResult not found");

            if (!string.IsNullOrEmpty(request.Payload.PrizeId))
            {
                var prize = await _unitOfWork.GetRepository<Prize>().GetByIdAsync(request.Payload.PrizeId);
                if (prize == null) return BaseException.BadRequestNotFoundResponse("Prize not found");

                var resultEventId = await ResolveEventIdAsync(result);
                if (!string.IsNullOrEmpty(resultEventId) && prize.EventId != resultEventId)
                {
                    return BaseException.BadRequestInvaildInputResponse("Giải thưởng không thuộc cùng sự kiện với kết quả này.");
                }

                if (result.PrizeId != prize.Id)
                {
                    var currentAssignedCount = _unitOfWork.GetRepository<FinalResult>()
                        .Entities.Count(x => x.PrizeId == prize.Id);

                    if (currentAssignedCount >= prize.Quantity)
                    {
                        return BaseException.BadRequestResponse($"Số lượng giải thưởng '{prize.PrizeName}' đã đạt giới hạn tối đa ({prize.Quantity}).");
                    }
                }

                result.PrizeId = prize.Id;
            }
            else
            {
                result.PrizeId = null;
            }

            await _unitOfWork.GetRepository<FinalResult>().UpdateAsync(result);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>FinalResult chỉ mang đúng 1 trong 3 phạm vi (RoundId/EventId/TrackId) — resolve về EventId thật.</summary>
        private async Task<string?> ResolveEventIdAsync(FinalResult finalResult)
        {
            if (!string.IsNullOrEmpty(finalResult.EventId))
            {
                return finalResult.EventId;
            }

            if (!string.IsNullOrEmpty(finalResult.RoundId))
            {
                var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(finalResult.RoundId);
                return round?.EventId;
            }

            if (!string.IsNullOrEmpty(finalResult.TrackId))
            {
                var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(finalResult.TrackId);
                return track?.EventId;
            }

            return null;
        }
    }
}
