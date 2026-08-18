// [FLOW3-NOPBAI][Storage] Implementation luu file XUONG DIA CUC BO — chi dung khi
// ASPNETCORE_ENVIRONMENT=Development va CHUA co key S3 that (xem dieu kien dang ky
// trong Program.cs). Giup test upload/xem anh tren may local ma khong can key
// CloudFly that (thuong chi ton tai tren Render production, khong ai co san).

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SEAL_Application.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Services
{
    public class LocalFileStorageService : ICloudStorageService
    {
        private readonly string _uploadsRoot;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _uploadsRoot = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(_uploadsRoot);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string objectName, string contentType, CancellationToken cancellationToken = default)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Object name must not be empty.", nameof(objectName));

            var relativePath = objectName.Trim().TrimStart('/').Replace('\\', '/');
            var fullPath = Path.Combine(_uploadsRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            using (var fileOutput = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileOutput, cancellationToken);
            }

            return BuildPublicUrl(relativePath);
        }

        public Task<Stream> DownloadFileAsync(string objectUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectUrl))
                throw new ArgumentException("Object URL must not be empty.", nameof(objectUrl));

            var relativePath = ExtractRelativePath(objectUrl);
            var fullPath = Path.Combine(_uploadsRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File khong ton tai trong local storage: {relativePath}", fullPath);

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(stream);
        }

        private string BuildPublicUrl(string relativePath)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}"
                : "http://localhost:5138";

            var escapedPath = string.Join('/', relativePath.Split('/').Select(Uri.EscapeDataString));
            return $"{baseUrl}/uploads/{escapedPath}";
        }

        private static string ExtractRelativePath(string url)
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.TrimStart('/');
            if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                path = path.Substring("uploads/".Length);

            return Uri.UnescapeDataString(path);
        }
    }
}
