using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.FinalResults.Commands.SetRoundResultsPublishStatus
{
    /// <summary>
    /// ĐẶT TRẠNG THÁI CÔNG BỐ của kết quả một vòng thi — chuyển được CẢ HAI CHIỀU:
    ///   IsPublished = true  -> NHÁP sang ĐÃ CÔNG BỐ (mọi người xem được bảng xếp hạng)
    ///   IsPublished = false -> ĐÃ CÔNG BỐ về lại NHÁP (chỉ EC/Admin xem, ẩn khỏi người ngoài)
    ///
    /// GIỮ NGUYÊN toàn bộ Rank/FinalScore/IsAdvanced đã tính ở cả hai chiều — chỉ đổi khả năng
    /// hiển thị, đảo qua đảo lại bao nhiêu lần cũng được.
    ///
    /// KHÁC với DELETE round/{roundId} (UnpublishRoundResults): lệnh kia XÓA TRỌN BỘ kết quả,
    /// buộc phải Calculate lại từ đầu.
    /// </summary>
    public class SetRoundResultsPublishStatusCommand : IRequest<Result<bool>>
    {
        public string RoundId { get; }

        /// <summary>true = công bố; false = thu hồi về nháp.</summary>
        public bool IsPublished { get; }

        public SetRoundResultsPublishStatusCommand(string roundId, bool isPublished)
        {
            RoundId = roundId;
            IsPublished = isPublished;
        }
    }
}

