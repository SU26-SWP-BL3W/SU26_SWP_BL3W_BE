using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Events.Commands.CreateEvent.Models
{
    public class CreateEventResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string? Season { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; }
        public string? PhotoEventUrl { get; set; }
        public DateTimeOffset CreatedTime { get; set; }

        public List<RoundResponseDto> Rounds { get; set; } = new List<RoundResponseDto>();
    }

    public class RoundResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string RoundName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? AdvancementRule { get; set; }
        public DateTime? ScoringStartDate { get; set; }
        public DateTime? ScoringEndDate { get; set; }

        public List<TrackResponseDto> Tracks { get; set; } = new List<TrackResponseDto>();
    }

    public class TrackResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TemplateId { get; set; }
        public string? SubmissionRuleDescription { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ScoringStartDate { get; set; }
        public DateTime? ScoringEndDate { get; set; }
    }
}
