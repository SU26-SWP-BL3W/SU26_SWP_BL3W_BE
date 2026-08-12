using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration.Models;

namespace SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration
{
    public class ConfirmTeamRegistrationCommand : IRequest<Result<ConfirmTeamRegistrationResponseModel>>
    {
        public string TeamId { get; set; } = string.Empty;

        public ConfirmTeamRegistrationCommand(string teamId)
        {
            TeamId = teamId;
        }
    }
}

