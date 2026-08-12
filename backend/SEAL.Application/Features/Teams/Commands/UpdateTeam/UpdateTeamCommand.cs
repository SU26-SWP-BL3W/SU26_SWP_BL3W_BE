using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Commands.UpdateTeam.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.UpdateTeam
{
    public class UpdateTeamCommand : IRequest<Result<UpdateTeamResponseModel>>
    {
        public string Id { get; set; } = string.Empty; 
        public UpdateTeamRequestModel Model { get; set; } = new UpdateTeamRequestModel();
    }
}
