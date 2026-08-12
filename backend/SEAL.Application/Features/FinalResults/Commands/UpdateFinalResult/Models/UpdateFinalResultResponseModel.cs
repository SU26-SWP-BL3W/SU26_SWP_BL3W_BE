using System;

namespace SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult.Models
{
    public class UpdateFinalResultResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string RoundId { get; set; } = string.Empty;
        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
