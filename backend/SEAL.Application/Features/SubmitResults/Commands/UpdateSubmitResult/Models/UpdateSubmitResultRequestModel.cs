using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult.Models
{
    public class UpdateSubmitResultRequestModel
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>Bỏ trống/null = giữ URL cũ.</summary>
        public string? SubmissionUrl { get; set; }

        /// <summary>null = giữ mô tả cũ; chuỗi rỗng = xóa mô tả.</summary>
        public string? Description { get; set; }

        /// <summary>null = giữ nguyên. Nếu client quên gửi mà mặc định false thì bài bị TẮT NGẦM
        /// (loại khỏi tính kết quả) — vì vậy phải nullable. Chỉ Event Coordinator được đổi.</summary>
        public bool? IsActive { get; set; }
    }
}
