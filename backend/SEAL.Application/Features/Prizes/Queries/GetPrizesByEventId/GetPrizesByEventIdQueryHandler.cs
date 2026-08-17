using MediatR;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Prizes.Queries.GetPrizesByEventId
{
    public class GetPrizesByEventIdQueryHandler : IRequestHandler<GetPrizesByEventIdQuery, Result<List<PrizeModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPrizesByEventIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<PrizeModel>>> Handle(GetPrizesByEventIdQuery request, CancellationToken cancellationToken)
        {
            var prizes = await _unitOfWork.GetRepository<Prize>()
                .FindAsync(p => p.EventId == request.EventId);

            var list = prizes.Select(p => new PrizeModel
            {
                Id = p.Id,
                EventId = p.EventId,
                TrackId = p.TrackId,
                PrizeName = p.PrizeName,
                Value = p.Value,
                Quantity = p.Quantity
            }).ToList();

            return list;
        }
    }
}
