using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Events.Commands.CreateEvent.Models;

namespace SEAL_Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommand : IRequest<Result<CreateEventResponseModel>>
    {
        public CreateEventRequestModel Model { get; set; } = new CreateEventRequestModel();
    }
}

