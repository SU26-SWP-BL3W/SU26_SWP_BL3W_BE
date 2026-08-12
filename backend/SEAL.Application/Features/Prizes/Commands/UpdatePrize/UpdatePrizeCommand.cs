using MediatR;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Prizes.Commands.UpdatePrize
{
    public class UpdatePrizeCommand : IRequest<Result<PrizeModel>>
    {
        public string PrizeId { get; set; } = string.Empty;
        public UpdatePrizeRequestModel Payload { get; set; } = null!;
    }
}
