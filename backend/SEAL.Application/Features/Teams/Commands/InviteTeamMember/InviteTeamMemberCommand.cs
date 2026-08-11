using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Commands.InviteTeamMember.Models;

namespace SEAL_Application.Features.Teams.Commands.InviteTeamMember
{
    public class InviteTeamMemberCommand : IRequest<Result<InviteTeamMemberResponseModel>>
    {
        public string TeamId { get; set; } = string.Empty;
        public InviteTeamMemberRequestModel Model { get; set; } = new InviteTeamMemberRequestModel();
    }
}

