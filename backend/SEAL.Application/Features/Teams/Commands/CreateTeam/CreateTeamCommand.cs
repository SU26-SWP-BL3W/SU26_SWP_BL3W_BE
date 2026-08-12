using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Commands.CreateTeam.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.CreateTeam
{
    public class CreateTeamCommand : IRequest<Result<CreateTeamResponseModel>>
    {
        public CreateTeamRequestModel Model { get; set; } = new CreateTeamRequestModel();
    }
}


