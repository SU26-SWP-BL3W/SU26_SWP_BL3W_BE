namespace SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration.Models
{
    public class ConfirmTeamRegistrationResponseModel
    {
        public string TeamId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }
}
