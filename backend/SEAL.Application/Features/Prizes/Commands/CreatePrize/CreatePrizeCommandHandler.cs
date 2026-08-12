using MediatR;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Prizes.Commands.CreatePrize
{
    public class CreatePrizeCommandHandler : IRequestHandler<CreatePrizeCommand, Result<PrizeModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreatePrizeCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PrizeModel>> Handle(CreatePrizeCommand request, CancellationToken cancellationToken)
        {
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.EventId);
            if (eventEntity == null) return BaseException.BadRequestNotFoundResponse("Event not found");

            var prize = new Prize
            {
                EventId = request.EventId,
                PrizeName = request.Payload.PrizeName,
                Value = request.Payload.Value,
                Quantity = request.Payload.Quantity
            };

            await _unitOfWork.GetRepository<Prize>().AddAsync(prize);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var model = new PrizeModel
            {
                Id = prize.Id,
                EventId = prize.EventId,
                PrizeName = prize.PrizeName,
                Value = prize.Value,
                Quantity = prize.Quantity
            };
            
            return model;
        }
    }
}
