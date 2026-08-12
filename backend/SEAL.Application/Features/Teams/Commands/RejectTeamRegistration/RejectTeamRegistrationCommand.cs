using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Teams.Commands.RejectTeamRegistration
{
    /// <summary>
    /// EC/Admin TỪ CHỐI duyệt đội: PendingApproval -> Forming (mở khóa để đội tự sửa
    /// thành viên rồi chốt lại). Lý do được gửi cho trưởng nhóm.
    /// </summary>
    public class RejectTeamRegistrationCommand : IRequest<Result<bool>>
    {
        public string TeamId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        public RejectTeamRegistrationCommand(string teamId, string reason)
        {
            TeamId = teamId;
            Reason = reason;
        }
    }
}

