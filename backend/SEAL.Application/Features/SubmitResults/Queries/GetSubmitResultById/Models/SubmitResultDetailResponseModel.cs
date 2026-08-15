using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById.Models
{
    public class SubmitResultDetailResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public string SubmissionUrl { get; set; } = string.Empty;
        public string? RepoUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public string? RepoHost { get; set; }
        public string? RepoFullName { get; set; }
        public int? RepoStars { get; set; }
        public DateTimeOffset? RepoLastPush { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
