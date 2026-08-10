using Microsoft.AspNetCore.Http;

namespace SEAL_Application.Commons
{
    public static class ImageHelper
    {
        // 1. Cấu hình các hằng số chung ở một chỗ
        public const long MaxFileSize = 15 * 1024 * 1024; // 15MB

        public static readonly string[] AllowedExtensions = {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".heic", ".heif"
        };

        public static readonly string[] AllowedContentTypes = {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
            "image/svg+xml", "image/bmp", "image/heic", "image/heif"
        };

        /// <summary>
        /// Kiểm tra file có hợp lệ không. Nếu lỗi sẽ Throw Exception để Handler bắt.
        /// </summary>
        public static void ValidateImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không được để trống.");

            if (file.Length > MaxFileSize)
                throw new ArgumentException($"File quá lớn. Kích thước tối đa là {MaxFileSize / (1024 * 1024)}MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException($"Định dạng file không hỗ trợ. Chỉ chấp nhận: {string.Join(", ", AllowedExtensions)}");

            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new ArgumentException("Content type không hợp lệ.");
        }

        /// <summary>
        /// Tạo tên file duy nhất theo chuẩn chung (để tránh trùng lặp trên S3)
        /// Format: {prefix}_{guid}_{timestamp}.{ext}
        /// </summary>
        public static string GenerateUniqueFileName(string originalFileName, string prefix = "img")
        {
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8); // Lấy 8 ký tự đầu cho ngắn
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            return $"{prefix}_{uniqueId}_{timestamp}{extension}";
        }
    }
}
