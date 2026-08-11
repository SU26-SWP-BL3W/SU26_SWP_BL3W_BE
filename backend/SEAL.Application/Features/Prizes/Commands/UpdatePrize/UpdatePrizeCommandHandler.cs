using MediatR;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Prizes.Commands.UpdatePrize
{
    public class UpdatePrizeCommandHandler : IRequestHandler<UpdatePrizeCommand, Result<PrizeModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePrizeCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PrizeModel>> Handle(UpdatePrizeCommand request, CancellationToken cancellationToken)
        {
            var prize = await _unitOfWork.GetRepository<Prize>().GetByIdAsync(request.PrizeId);
            if (prize == null) return BaseException.BadRequestNotFoundResponse("Prize not found");

            prize.PrizeName = request.Payload.PrizeName;
            prize.Value = request.Payload.Value;
            prize.Quantity = request.Payload.Quantity;

            await _unitOfWork.GetRepository<Prize>().UpdateAsync(prize);
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
