namespace SEAL_Application.Features.Scores.Commands.UpdateScore.Models
{
    public class UpdateScoreRequestModel
    {
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }
}
