// [FLOW3-NOPBAI][Storage] Implementation upload/xoa file len Cloudfly (S3-compatible).

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SEAL_Application.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Services
{
    public class CloudflyStorageService : ICloudStorageService, IDisposable
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _baseFolder;
        private readonly string _endpoint;
        private readonly ILogger<CloudflyStorageService> _logger;

        public CloudflyStorageService(IConfiguration configuration, ILogger<CloudflyStorageService> logger)
        {
            _logger = logger;
            var section = configuration.GetSection("S3");
            _endpoint = section.GetValue<string>("Endpoint")?.TrimEnd('/') ?? "https://s3.cloudfly.vn";
            _bucketName = section.GetValue<string>("BucketName") ?? throw new InvalidOperationException("S3:BucketName is required.");
            _baseFolder = section.GetValue<string>("BaseFolder") ?? string.Empty;
            var accessKey = section.GetValue<string>("AccessKeyId") ?? throw new InvalidOperationException("S3:AccessKeyId is required.");
            var secretKey = section.GetValue<string>("SecretAccessKey") ?? throw new InvalidOperationException("S3:SecretAccessKey is required.");
            var forcePathStyle = section.GetValue<bool?>("ForcePathStyle") ?? true;
            
            _logger.LogInformation("S3 config: endpoint={Endpoint}, bucket={Bucket}, key={Key}",
                _endpoint, _bucketName, accessKey);
            
            var s3Config = new AmazonS3Config
            {
                ServiceURL = _endpoint,
                ForcePathStyle = forcePathStyle, 
                DisableHostPrefixInjection = true,
                UseHttp = _endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            };

            _s3Client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string objectName, string contentType, CancellationToken cancellationToken = default)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (string.IsNullOrWhiteSpace(objectName))
            {
                throw new ArgumentException("Object name must not be empty.", nameof(objectName));
            }

            var key = BuildObjectKey(objectName);

            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                AutoCloseStream = false,
                UseChunkEncoding = false,
            };
            request.Headers.CacheControl = "no-cache";
            if (fileStream.CanSeek)
            {
                request.Headers.ContentLength = fileStream.Length;
            }

            _logger.LogInformation("Uploading file to S3 bucket {Bucket} with key {Key}", _bucketName, key);
            await _s3Client.PutObjectAsync(request, cancellationToken);
            _logger.LogInformation("Uploaded file to S3 bucket {Bucket} with key {Key}", _bucketName, key);

            return BuildObjectUrl(key);
        }

        private string BuildObjectKey(string objectName)
        {
            var trimmedName = objectName.Trim().TrimStart('/');
            if (string.IsNullOrEmpty(_baseFolder))
            {
                return trimmedName.Replace('\\', '/');
            }

            var normalizedFolder = _baseFolder.Trim().Trim('/').Replace('\\', '/');
            return $"{normalizedFolder}/{trimmedName.Replace('\\', '/')}";
        }

        private string BuildObjectUrl(string key)
        {
            var escapedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
            return $"{_endpoint}/{_bucketName}/{escapedKey}";
        }

        public async Task<Stream> DownloadFileAsync(string objectUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectUrl))
            {
                throw new ArgumentException("Object URL must not be empty.", nameof(objectUrl));
            }

            var key = ExtractKeyFromUrl(objectUrl);
            
            _logger.LogInformation("Downloading file from S3 bucket {Bucket} with key {Key}", _bucketName, key);

            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var memoryStream = new MemoryStream();
            
            using (var response = await _s3Client.GetObjectAsync(request, cancellationToken))
            {
                _logger.LogInformation("Downloaded file from S3 bucket {Bucket} with key {Key}", _bucketName, key);
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            }
            
            memoryStream.Position = 0;
            return memoryStream;
        }

        private string ExtractKeyFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath.TrimStart('/');
                
                if (path.StartsWith(_bucketName + "/", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(_bucketName.Length + 1);
                }
                
                var key = Uri.UnescapeDataString(path);
                return key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract key from URL: {Url}", url);
                throw new ArgumentException($"Invalid object URL format: {url}", nameof(url), ex);
            }
        }

        public void Dispose()
        {
            _s3Client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
