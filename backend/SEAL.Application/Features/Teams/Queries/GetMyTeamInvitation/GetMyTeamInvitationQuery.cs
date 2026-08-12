using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Queries.GetMyTeamInvitation.Models;

namespace SEAL_Application.Features.Teams.Queries.GetMyTeamInvitation
{
    public class GetMyTeamInvitationQuery : IRequest<Result<MyTeamInvitationResponseModel?>>
    {
        public string TeamId { get; set; } = string.Empty;
    }
}

