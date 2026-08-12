using System;

namespace SEAL_Application.Features.Rounds.Commands.UpdateRound.Models
{
    public class UpdateRoundRequestModel
    {
        public string EventId { get; set; } = string.Empty;
        public string RoundName { get; set; } = string.Empty;
        public int RoundNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? AdvancementRule { get; set; }
        /// <summary>Thời điểm bắt đầu chấm (optional). Null = mặc định = EndDate.</summary>
        public DateTime? ScoringStartDate { get; set; }
        /// <summary>Hạn chót chấm (optional). Null = không giới hạn.</summary>
        public DateTime? ScoringEndDate { get; set; }
    }
}
