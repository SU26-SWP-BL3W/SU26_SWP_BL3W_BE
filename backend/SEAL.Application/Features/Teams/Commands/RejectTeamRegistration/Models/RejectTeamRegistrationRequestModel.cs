namespace SEAL_Application.Features.Teams.Commands.RejectTeamRegistration.Models
{
    /// <summary>Body của API từ chối duyệt đội — lý do bắt buộc để đội biết cần sửa gì.</summary>
    public class RejectTeamRegistrationRequestModel
    {
        public string Reason { get; set; } = string.Empty;
    }
}
