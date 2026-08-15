using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Interfaces
{
    public class GitRepoInspectResult
    {
        public bool ShouldReject { get; set; }
        public string? Error { get; set; }
        public string? Host { get; set; }
        public string? FullName { get; set; }
        public int? Stars { get; set; }
        public DateTimeOffset? LastPush { get; set; }
    }

    public interface IGitHostingService
    {
        Task<GitRepoInspectResult> InspectAsync(string repoUrl, CancellationToken cancellationToken = default);
    }
}
