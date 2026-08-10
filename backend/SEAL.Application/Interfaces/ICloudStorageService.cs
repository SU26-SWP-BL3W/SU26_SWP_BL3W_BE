// [FLOW3-NOPBAI][Storage] Interface dich vu luu tru file dung chung, phuc vu dinh kem file cho bai nop du thi.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Interfaces
{
    public interface ICloudStorageService
    {
        /// <summary>
        /// Upload file lên Cloud Storage.
        /// </summary>
        /// <param name="fileStream">Stream dữ liệu của file.</param>
        /// <param name="objectName">Tên/đường dẫn của object trong Bucket.</param>
        /// <param name="contentType">Định dạng file (MIME type).</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ.</param>
        /// <returns>URL truy cập trực tiếp file sau khi upload thành công.</returns>
        Task<string> UploadFileAsync(Stream fileStream, string objectName, string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tải file từ Cloud Storage.
        /// </summary>
        /// <param name="objectUrl">URL của object cần tải.</param>
        /// <param name="cancellationToken">Token hủy bỏ tác vụ.</param>
        /// <returns>Stream dữ liệu của file đã tải.</returns>
        Task<Stream> DownloadFileAsync(string objectUrl, CancellationToken cancellationToken = default);
    }
}
