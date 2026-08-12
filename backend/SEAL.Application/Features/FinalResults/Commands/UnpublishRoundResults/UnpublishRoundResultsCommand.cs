using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.FinalResults.Commands.UnpublishRoundResults
{
    /// <summary>
    /// Hủy công bố kết quả một vòng thi: xóa TRỌN BỘ FinalResult của vòng đó (không xóa lẻ dòng).
    /// </summary>
    public class UnpublishRoundResultsCommand : IRequest<Result<bool>>
    {
        public string RoundId { get; }

        public UnpublishRoundResultsCommand(string roundId)
        {
            RoundId = roundId;
        }
    }
}

