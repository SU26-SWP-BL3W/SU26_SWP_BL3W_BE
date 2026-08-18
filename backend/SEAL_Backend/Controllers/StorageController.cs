// [FLOW3-NOPBAI][Controller] StorageController: endpoint upload file dinh kem dung chung cho luong nop bai.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Interfaces;
using SEAL_Backend.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller hỗ trợ lưu trữ và tải tệp từ Cloud Storage (CloudFly).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StorageController(ICloudStorageService cloudStorageService) : CustomControllerBase
    {
        private readonly ICloudStorageService _cloudStorageService = cloudStorageService;

        /// <summary>
        /// Tải lên một tệp tin bất kỳ lên Cloud Storage.
        /// </summary>
        /// <param name="file">Tệp tin cần tải lên.</param>
        /// <param name="folder">Thư mục lưu trữ trên bucket (mặc định: general).</param>
        /// <param name="cancellationToken">Token hủy tác vụ.</param>
        /// <returns>Đường dẫn URL truy cập trực tiếp file.</returns>
        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromQuery] string folder = "general",
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Tệp tin không được để trống.");
            }

            // Khong tu bat exception o day nua — de GlobalExceptionMiddleware xu ly (da
            // bao BaseResponse<object> voi message chuan). Ban truoc "catch { return
            // StatusCode(500, chuoi tho) }" tra ve body dang string thuan, khong phai
            // JSON object {message: ...}, nen FE (doc err.response.data.message) luon
            // nhan undefined va roi ve thong bao chung chung "Khong the gui ho so",
            // che mat ly do that (vd thieu cau hinh S3 CloudFly that trong local dev).
            var fileExtension = Path.GetExtension(file.FileName);
            // Tạo tên file ngẫu nhiên duy nhất tránh ghi đè
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var objectName = $"{folder.Trim().Trim('/')}/{uniqueFileName}";

            using var stream = file.OpenReadStream();
            var fileUrl = await _cloudStorageService.UploadFileAsync(
                stream,
                objectName,
                file.ContentType,
                cancellationToken
            );

            return OkResponse(new { FileUrl = fileUrl });
        }

        /// <summary>
        /// Tải tệp dữ liệu từ Cloud Storage.
        /// </summary>
        /// <param name="fileUrl">Đường dẫn đầy đủ của tệp cần tải.</param>
        /// <param name="cancellationToken">Token hủy tác vụ.</param>
        /// <returns>Tệp dữ liệu trả về client.</returns>
        [HttpGet("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Download(
            [FromQuery] string fileUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return BadRequest("Đường dẫn tệp tin không được trống.");
            }

            var fileStream = await _cloudStorageService.DownloadFileAsync(fileUrl, cancellationToken);
            var contentType = "application/octet-stream";

            // Đoán Content-Type dựa vào đuôi mở rộng của URL
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;
            var extension = Path.GetExtension(path).ToLowerInvariant();

            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                case ".pdf":
                    contentType = "application/pdf";
                    break;
                case ".zip":
                    contentType = "application/zip";
                    break;
                case ".rar":
                    contentType = "application/x-rar-compressed";
                    break;
                case ".docx":
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
            }

            var fileName = Path.GetFileName(path);
            return File(fileStream, contentType, fileName);
        }
    }
}
