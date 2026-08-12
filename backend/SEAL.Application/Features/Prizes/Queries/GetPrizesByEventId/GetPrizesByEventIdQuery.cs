using MediatR;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Application.Features.Prizes.Queries.GetPrizesByEventId
{
    public class GetPrizesByEventIdQuery : IRequest<Result<List<PrizeModel>>>
    {
        public string EventId { get; set; } = string.Empty;
    }
}
