using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults
{
    public class CalculateRoundResultsCommand : IRequest<Result<List<CalculateRoundResultItemModel>>>
    {
        public string RoundId { get; set; } = string.Empty;

        /// <summary>Số đội đầu bảng được thăng vòng (IsAdvanced = true). Round không lưu top_n nên truyền vào đây.</summary>
        public int TopN { get; set; }

        public CalculateRoundResultsCommand(string roundId, int topN)
        {
            RoundId = roundId;
            TopN = topN;
        }
    }
}

