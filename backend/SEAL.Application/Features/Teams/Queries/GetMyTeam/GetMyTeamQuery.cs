using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Queries.GetMyTeam.Models;

namespace SEAL_Application.Features.Teams.Queries.GetMyTeam
{
    public class GetMyTeamQuery : IRequest<Result<MyTeamResponseModel?>>
    {
        public string EventId { get; set; } = string.Empty;
    }
}

