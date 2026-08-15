using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SEAL_Application.Interfaces;

namespace SEAL_Infrastructure.Services
{
    public class GitHostingService : IGitHostingService
    {
        private static readonly Regex GitHub = new(
            @"^https?://(?:www\.)?github\.com/([^/]+)/([^/#?]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex GitLab = new(
            @"^https?://(?:www\.)?gitlab\.com/(.+?)(?:\.git)?/?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly HttpClient _http;

        public GitHostingService(HttpClient http)
        {
            _http = http;
        }

        public async Task<GitRepoInspectResult> InspectAsync(string repoUrl, CancellationToken cancellationToken = default)
        {
            var result = new GitRepoInspectResult { Host = "other" };
            if (string.IsNullOrWhiteSpace(repoUrl)) return result;

            var gh = GitHub.Match(repoUrl.Trim());
            if (gh.Success)
            {
                var owner = gh.Groups[1].Value;
                var repo = gh.Groups[2].Value.TrimEnd('/');
                if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                    repo = repo[..^4];
                return await FetchGithubAsync(owner, repo, cancellationToken);
            }

            var gl = GitLab.Match(repoUrl.Trim());
            if (gl.Success)
            {
                var path = gl.Groups[1].Value.Trim('/');
                if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                    path = path[..^4];
                return await FetchGitlabAsync(path, cancellationToken);
            }

            return result;
        }

        private async Task<GitRepoInspectResult> FetchGithubAsync(string owner, string repo, CancellationToken ct)
        {
            try
            {
                using var resp = await _http.GetAsync($"https://api.github.com/repos/{owner}/{repo}", ct);
                if ((int)resp.StatusCode == 404)
                {
                    return new GitRepoInspectResult
                    {
                        ShouldReject = true,
                        Error = "Repo GitHub không tồn tại hoặc không public."
                    };
                }
                if (!resp.IsSuccessStatusCode) return new GitRepoInspectResult { Host = "github", FullName = $"{owner}/{repo}" };

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                var root = doc.RootElement;
                int? stars = root.TryGetProperty("stargazers_count", out var s) && s.TryGetInt32(out var n) ? n : null;
                DateTimeOffset? pushed = null;
                if (root.TryGetProperty("pushed_at", out var p) && DateTimeOffset.TryParse(p.GetString(), out var dt))
                    pushed = dt;
                var full = root.TryGetProperty("full_name", out var fn) ? fn.GetString() : $"{owner}/{repo}";
                return new GitRepoInspectResult { Host = "github", FullName = full, Stars = stars, LastPush = pushed };
            }
            catch
            {
                // API ngoài lỗi/timeout: không chặn nộp (đề ghi optional).
                return new GitRepoInspectResult { Host = "github", FullName = $"{owner}/{repo}" };
            }
        }

        private async Task<GitRepoInspectResult> FetchGitlabAsync(string path, CancellationToken ct)
        {
            try
            {
                var encoded = Uri.EscapeDataString(path);
                using var resp = await _http.GetAsync($"https://gitlab.com/api/v4/projects/{encoded}", ct);
                if ((int)resp.StatusCode == 404)
                {
                    return new GitRepoInspectResult
                    {
                        ShouldReject = true,
                        Error = "Repo GitLab không tồn tại hoặc không public."
                    };
                }
                if (!resp.IsSuccessStatusCode) return new GitRepoInspectResult { Host = "gitlab", FullName = path };

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                var root = doc.RootElement;
                int? stars = root.TryGetProperty("star_count", out var s) && s.TryGetInt32(out var n) ? n : null;
                DateTimeOffset? pushed = null;
                if (root.TryGetProperty("last_activity_at", out var p) && DateTimeOffset.TryParse(p.GetString(), out var dt))
                    pushed = dt;
                var full = root.TryGetProperty("path_with_namespace", out var fn) ? fn.GetString() : path;
                return new GitRepoInspectResult { Host = "gitlab", FullName = full, Stars = stars, LastPush = pushed };
            }
            catch
            {
                return new GitRepoInspectResult { Host = "gitlab", FullName = path };
            }
        }
    }
}
