// [FLOW3-DOITHI][Entity] Team: đội thi (3-5 thành viên) đăng ký tham gia 1 sự kiện.

﻿using System;
using System.Collections.Generic;
using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Domain.Entity
{
    public class Team : BaseEntity
    {
        public string EventId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Trạng thái đội: Forming (đang chiêu mộ) -> Registered (đã khóa đăng ký) / Disqualified.</summary>
        public TeamStatus Status { get; set; } = TeamStatus.Forming;

        /// <summary>
        /// Lý do EC/Admin từ chối duyệt đội ở lần chốt danh sách gần nhất.
        /// Chỉ có giá trị khi đội đang mang một lần từ chối chưa xử lý xong; được xóa (null)
        /// ngay khi đội chốt danh sách lại. Trước đây lý do chỉ nằm trong email gửi trưởng nhóm,
        /// không check mail thì trên web không biết đội bị từ chối vì sao.
        /// </summary>
        public string? LastRejectReason { get; set; }

        // Navigation Properties
        public virtual Event Event { get; set; } = null!; 
        public virtual ICollection<EventRole> EventRoles { get; set; } = new List<EventRole>();
        public virtual ICollection<SubmitResult> SubmitResults { get; set; } = new List<SubmitResult>();
    }
}