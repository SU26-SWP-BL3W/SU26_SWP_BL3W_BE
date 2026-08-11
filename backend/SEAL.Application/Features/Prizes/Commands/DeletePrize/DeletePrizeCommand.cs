using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Prizes.Commands.DeletePrize
{
    public class DeletePrizeCommand : IRequest<Result<bool>>
    {
        public string PrizeId { get; set; } = string.Empty;
    }
}
