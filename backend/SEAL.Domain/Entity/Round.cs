using SEAL_Domain.Base;
using System;
using System.Collections.Generic;

namespace SEAL_Domain.Entity
{
    public class Round : BaseEntity
    {
        public string EventId { get; set; } = string.Empty;
        public string RoundName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? AdvancementRule { get; set; }

        /// <summary>Thời điểm BẮT ĐẦU cho giám khảo chấm. Null = mặc định = EndDate (chấm ngay khi hết hạn nộp).</summary>
        public DateTime? ScoringStartDate { get; set; }

        /// <summary>Hạn chót (KẾT THÚC) giám khảo phải chấm xong. Null = không giới hạn (giữ hành vi cũ).</summary>
        public DateTime? ScoringEndDate { get; set; }

        /// <summary>Thời điểm BẮT ĐẦU mở phúc khảo.</summary>
        public DateTime? AppealStartDate { get; set; }

        /// <summary>Thời điểm KẾT THÚC phúc khảo.</summary>
        public DateTime? AppealEndDate { get; set; }

        // Navigation Properties
        public virtual Event Event { get; set; } = null!;
        public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
        public virtual ICollection<FinalResult> FinalResults { get; set; } = new List<FinalResult>();
    }
}
