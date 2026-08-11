namespace SEAL_Domain.Entity.Enums
{
    /// <summary>
    /// Trạng thái đội thi. GIÁ TRỊ SỐ ĐƯỢC GÁN TƯỜNG MINH và KHÔNG ĐƯỢC ĐỔI:
    /// DB đang lưu enum dưới dạng int (Forming=0, Registered=1, Disqualified=2).
    /// Hai trạng thái mới (PendingApproval, Rejected) nhận số MỚI ở cuối — nếu chèn
    /// vào giữa theo thứ tự khai báo thì mọi đội đang Registered (=1) sẽ bị đọc nhầm.
    /// </summary>
    public enum TeamStatus
    {
        /// <summary>Đội mới tạo, đang chiêu mộ thành viên (chưa chốt danh sách).</summary>
        Forming = 0,

        /// <summary>EC/Admin đã DUYỆT — đội chính thức được thi.</summary>
        Registered = 1,

        /// <summary>Đội bị loại khỏi cuộc thi.</summary>
        Disqualified = 2,

        /// <summary>Đội đã chốt danh sách (bị khóa), đang CHỜ EC/Admin duyệt.</summary>
        PendingApproval = 3,

        /// <summary>Đội bị từ chối duyệt.</summary>
        Rejected = 4
    }
}
