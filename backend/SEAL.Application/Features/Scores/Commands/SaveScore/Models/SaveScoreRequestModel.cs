using System.Collections.Generic;

namespace SEAL_Application.Features.Scores.Commands.SaveScore.Models
{
    /// <summary>
    /// Model gộp: lưu (tạo mới hoặc cập nhật) một phiếu chấm kèm toàn bộ điểm chi tiết trong 1 lần gọi.
    /// Khóa định danh phiếu chấm = (EventRoleId, SubmitResultId).
    /// </summary>
    public class SaveScoreRequestModel
    {
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public bool IsSubmitted { get; set; } = false;

        /// <summary>
        /// Toàn bộ điểm chi tiết theo từng tiêu chí. Danh sách này là nguồn dữ liệu chuẩn:
        /// tiêu chí có trong danh sách -> tạo/cập nhật; tiêu chí cũ không còn -> xóa.
        /// </summary>
        public List<SaveScoreDetailItem> Details { get; set; } = new();
    }

    public class SaveScoreDetailItem
    {
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
