using System;

namespace SEAL_Application.Features.Events.Models
{
    public class EventModel
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
        public int MaxTeams { get; set; }
        public int TeamCount { get; set; }
        /// <summary>Điều phối viên (EC) được gán đầu tiên cho sự kiện, nếu có. Null = chưa phân công.</summary>
        public string? AssignedCoordinatorName { get; set; }
        public string? AssignedCoordinatorEmail { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }

        public List<EventRoundModel> Rounds { get; set; } = new List<EventRoundModel>();
        public List<SEAL_Application.Features.Prizes.Models.PrizeModel> Prizes { get; set; } = new List<SEAL_Application.Features.Prizes.Models.PrizeModel>();
    }

    public class EventRoundModel
    {
        public string Id { get; set; } = string.Empty;
        public string RoundName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? AdvancementRule { get; set; }
        public DateTime? ScoringStartDate { get; set; }
        public DateTime? ScoringEndDate { get; set; }

        public List<EventTrackModel> Tracks { get; set; } = new List<EventTrackModel>();
    }

    public class EventTrackModel
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
