using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Rounds.Commands.CreateRound.Models;

namespace SEAL_Application.Features.Rounds.Commands.CreateRound
{
    public class CreateRoundCommand : IRequest<Result<CreateRoundResponseModel>>
    {
        public CreateRoundRequestModel Model { get; set; } = new CreateRoundRequestModel();
    }
}

