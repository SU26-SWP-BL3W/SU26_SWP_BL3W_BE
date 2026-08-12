using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Commands.AddTeamMember.Models;

namespace SEAL_Application.Features.Teams.Commands.AddTeamMember
{
    public class AddTeamMemberCommand : IRequest<Result<AddTeamMemberResponseModel>>
    {
        public string TeamId { get; set; } = string.Empty;
        public AddTeamMemberRequestModel Model { get; set; } = new AddTeamMemberRequestModel();
    }
}

