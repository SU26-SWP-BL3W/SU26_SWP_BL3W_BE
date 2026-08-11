using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Domain.Entity
{
    public class Prize : BaseEntity
    {
        public string EventId { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên giải thưởng. Ví dụ: Giải Nhất, Giải Khuyến khích.
        /// </summary>
        public string PrizeName { get; set; } = string.Empty;

        /// <summary>
        /// Giá trị giải thưởng. Ví dụ: 10,000,000 VNĐ.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Số lượng giải này dự kiến trao.
        /// </summary>
        public int Quantity { get; set; } = 1;

        // Navigation Properties
        public virtual Event Event { get; set; } = null!;
        public virtual ICollection<FinalResult> FinalResults { get; set; } = new List<FinalResult>();
    }
}
