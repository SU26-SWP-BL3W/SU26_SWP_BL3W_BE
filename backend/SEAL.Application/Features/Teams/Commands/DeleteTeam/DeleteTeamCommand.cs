using SEAL_Domain.Base;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.DeleteTeam
{
    public class DeleteTeamCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteTeamCommand(string id)
        {
            Id = id;
        }
    }
}

