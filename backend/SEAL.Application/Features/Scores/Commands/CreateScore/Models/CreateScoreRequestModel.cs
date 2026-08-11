namespace SEAL_Application.Features.Scores.Commands.CreateScore.Models
{
    public class CreateScoreRequestModel
    {
        public string EventRoleId { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }
}
