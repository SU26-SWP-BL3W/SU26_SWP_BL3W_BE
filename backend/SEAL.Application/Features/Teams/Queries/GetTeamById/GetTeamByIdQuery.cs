using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Queries.GetTeamById.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Queries.GetTeamById
{
    public class GetTeamByIdQuery : IRequest<Result<TeamDetailResponseModel>>
    {
        public string Id { get; set; }

        public GetTeamByIdQuery(string id)
        {
            Id = id;
        }
    }
}
