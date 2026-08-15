using System;
using System.Collections.Generic;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Tracks.Models
{
    public class TrackModel
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
        public List<UserModel>? Judges { get; set; }
        public List<UserModel>? Mentors { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
