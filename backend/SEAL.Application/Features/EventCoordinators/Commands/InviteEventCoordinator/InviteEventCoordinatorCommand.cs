using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator.Models;

namespace SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator
{
    public class InviteEventCoordinatorCommand : IRequest<Result<InviteEventCoordinatorResponseModel>>
    {
        public InviteEventCoordinatorRequestModel Model { get; set; } = new InviteEventCoordinatorRequestModel();
    }
}

