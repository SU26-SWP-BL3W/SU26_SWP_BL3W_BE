using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.FinalResults.Commands.PublishRoundResults
{
    /// <summary>
    /// CÔNG BỐ kết quả một vòng thi: bật IsPublished = true cho TRỌN BỘ FinalResult của vòng
    /// (chuyển từ NHÁP sang công khai). Dùng sau khi EventCoordinator đã rà soát kết quả tính ra.
    /// </summary>
    public class PublishRoundResultsCommand : IRequest<Result<bool>>
    {
        public string RoundId { get; }

        public PublishRoundResultsCommand(string roundId)
        {
            RoundId = roundId;
        }
    }
}

