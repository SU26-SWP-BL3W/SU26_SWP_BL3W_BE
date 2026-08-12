using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.FinalResults.Commands.DeleteFinalResult
{
    public class DeleteFinalResultCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteFinalResultCommand(string id)
        {
            Id = id;
        }
    }
}

