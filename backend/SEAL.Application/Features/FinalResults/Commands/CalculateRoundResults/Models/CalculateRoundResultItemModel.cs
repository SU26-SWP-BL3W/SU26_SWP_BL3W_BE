namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models
{
    public class CalculateRoundResultItemModel
    {
        public string FinalResultId { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }
    }
}
