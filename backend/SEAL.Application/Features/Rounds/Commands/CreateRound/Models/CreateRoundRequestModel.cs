using System;

namespace SEAL_Application.Features.Rounds.Commands.CreateRound.Models
{
    public class CreateRoundRequestModel
    {
        public string EventId { get; set; } = string.Empty;
        public string RoundName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? AdvancementRule { get; set; }
        public DateTime? ScoringStartDate { get; set; }
        public DateTime? ScoringEndDate { get; set; }
        public DateTime? AppealStartDate { get; set; }
        public DateTime? AppealEndDate { get; set; }
    }
}
