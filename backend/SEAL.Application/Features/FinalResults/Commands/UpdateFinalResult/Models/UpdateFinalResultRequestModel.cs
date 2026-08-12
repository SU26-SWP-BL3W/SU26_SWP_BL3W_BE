namespace SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult.Models
{
    public class UpdateFinalResultRequestModel
    {
        public string TeamId { get; set; } = string.Empty;
        public string RoundId { get; set; } = string.Empty;
        public decimal FinalScore { get; set; }
        public int Rank { get; set; }
        public bool IsAdvanced { get; set; }
    }
}
