using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Events.Commands.UpdateEvent.Models;

namespace SEAL_Application.Features.Events.Commands.UpdateEvent
{
    public class UpdateEventCommand : IRequest<Result<UpdateEventResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateEventRequestModel Model { get; set; } = new UpdateEventRequestModel();
    }
}

