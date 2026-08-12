using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Events.Models;

namespace SEAL_Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQuery : IRequest<Result<EventModel?>>
    {
        public string Id { get; set; }

        public GetEventByIdQuery(string id)
        {
            Id = id;
        }
    }
}

