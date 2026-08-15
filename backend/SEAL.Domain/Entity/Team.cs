// [FLOW3-DOITHI][Entity] Team: doi thi (3-5 thanh vien) dang ky tham gia 1 su kien va 1 hang muc.

using System;
using System.Collections.Generic;
using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Domain.Entity
{
    public class Team : BaseEntity
    {
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Trạng thái đội: Forming (đang chiêu mộ) -> Registered (đã khóa đăng ký) / Disqualified.</summary>
        public TeamStatus Status { get; set; } = TeamStatus.Forming;

        /// <summary>
        /// Lý do EC/Admin từ chối duyệt đội ở lần chốt danh sách gần nhất.
        /// </summary>
        public string? LastRejectReason { get; set; }

        // Navigation Properties
        public virtual Event Event { get; set; } = null!;
        public virtual Track? Track { get; set; }
        public virtual ICollection<EventRole> EventRoles { get; set; } = new List<EventRole>();
        public virtual ICollection<SubmitResult> SubmitResults { get; set; } = new List<SubmitResult>();
    }
}