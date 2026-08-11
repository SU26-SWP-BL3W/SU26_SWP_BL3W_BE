using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    public class FinalResult : BaseEntity
    {
        public string TeamId { get; set; } = string.Empty;

        // Phạm vi của kết quả: theo Vòng (RoundId), theo cả Sự kiện (EventId),
        // hoặc theo 1 Hạng mục/Track (TrackId). Tùy loại kết quả mà set 1 trong 3.
        public string? RoundId { get; set; }
        public string? EventId { get; set; }
        public string? TrackId { get; set; }
        public string? PrizeId { get; set; }

        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// Đã CÔNG BỐ ra bảng xếp hạng công khai chưa.
        /// false = kết quả NHÁP (chỉ EventCoordinator/Admin xem để rà soát trước khi công bố);
        /// true = đã công bố (mọi người xem được). Calculate tạo ở trạng thái nháp; Publish bật lên true.
        /// </summary>
        public bool IsPublished { get; set; }

        // Navigation Properties
        public virtual Round? Round { get; set; }
        public virtual Event? Event { get; set; }
        public virtual Track? Track { get; set; }
        public virtual Prize? Prize { get; set; }
        public virtual Team Team { get; set; } = null!;
    }
}
