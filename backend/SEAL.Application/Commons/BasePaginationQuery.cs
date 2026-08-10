using System.Collections.Generic;

namespace SEAL_Application.Commons
{
    public abstract class BasePaginationQuery
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsAscending { get; set; } = false;

        /// <summary>
        /// Danh sách các field cho phép sắp xếp. Override tại Query cụ thể để bảo mật.
        /// Sử dụng StringComparer.OrdinalIgnoreCase để hỗ trợ client linh hoạt hơn.
        /// </summary>
        public virtual HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase);
    }
}
