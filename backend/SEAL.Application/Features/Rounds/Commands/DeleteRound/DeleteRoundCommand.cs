using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Rounds.Commands.DeleteRound
{
    public class DeleteRoundCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteRoundCommand(string id)
        {
            Id = id;
        }
    }
}

