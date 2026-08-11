using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models;

namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults
{
    public class CalculateRoundResultsCommand : IRequest<Result<CalculateRoundResultsResponseModel>>
    {
        public string RoundId { get; set; } = string.Empty;

        /// <summary>Số đội đầu bảng được thăng vòng (IsAdvanced = true) khi Track/Round không có AdvancementRule hợp lệ.</summary>
        public int TopN { get; set; }

        public CalculateRoundResultsCommand(string roundId, int topN)
        {
            RoundId = roundId;
            TopN = topN;
        }
    }
}
