using MediatR;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Prizes.Commands.CreatePrize
{
    public class CreatePrizeCommand : IRequest<Result<PrizeModel>>
    {
        public string EventId { get; set; } = string.Empty;
        public CreatePrizeRequestModel Payload { get; set; } = null!;
    }
}
