using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList.Models
{
    public class SubmitResultListItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string? TeamName { get; set; }
        public bool IsTeamDisqualified { get; set; }
        public string? DisplayCode { get; set; }
        public string SubmissionUrl { get; set; } = string.Empty;
        public string? RepoUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
