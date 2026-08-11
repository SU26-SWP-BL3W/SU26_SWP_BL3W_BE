using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Criterias.Commands.ToggleCriteriaStatus
{
    public class ToggleCriteriaStatusCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public ToggleCriteriaStatusCommand(string id)
        {
            Id = id;
        }
    }
}

