using System;

namespace SEAL_Application.Features.FinalResults.Commands.CreateFinalResult.Models
{
    public class CreateFinalResultResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string? RoundId { get; set; }
        public string? EventId { get; set; }
        public string? TrackId { get; set; }
        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
