using System;

namespace SEAL_Application.Features.Tracks.Commands.CreateTrack.Models
{
    public class CreateTrackResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public string? TemplateId { get; set; }
        public string? Description { get; set; }
        public string? SubmissionRuleDescription { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ScoringStartDate { get; set; }
        public DateTime? ScoringEndDate { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
