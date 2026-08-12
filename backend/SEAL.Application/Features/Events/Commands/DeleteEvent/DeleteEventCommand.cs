using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Events.Commands.DeleteEvent
{
    public class DeleteEventCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteEventCommand(string id)
        {
            Id = id;
        }
    }
}

