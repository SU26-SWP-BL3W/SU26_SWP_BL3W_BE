using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Events.Queries.GetMyEvents.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Events.Queries.GetMyEvents
{
    public class GetMyEventsQuery : IRequest<Result<List<MyEventResponseModel>>>
    {
    }
}

