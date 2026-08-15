namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models
{
    public class CalculateRoundResultItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string FinalResultId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string RoundId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }
        public bool IsPublished { get; set; }
    }
}
