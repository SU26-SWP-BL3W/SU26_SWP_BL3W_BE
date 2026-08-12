using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Teams.Commands.ApproveTeamRegistration
{
    /// <summary>
    /// EC/Admin DUYỆT đội thi: PendingApproval -> Registered (đội chính thức được thi).
    /// </summary>
    public class ApproveTeamRegistrationCommand : IRequest<Result<bool>>
    {
        public string TeamId { get; set; } = string.Empty;

        public ApproveTeamRegistrationCommand(string teamId)
        {
            TeamId = teamId;
        }
    }
}

