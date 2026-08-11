using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Scores.Commands.DeleteScore
{
    public class DeleteScoreCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteScoreCommand(string id)
        {
            Id = id;
        }
    }
}

