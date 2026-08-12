using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Rounds.Commands.UpdateRound.Models;

namespace SEAL_Application.Features.Rounds.Commands.UpdateRound
{
    public class UpdateRoundCommand : IRequest<Result<UpdateRoundResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateRoundRequestModel Model { get; set; } = new UpdateRoundRequestModel();
    }
}

