using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.ScoreDetails.Commands.DeleteScoreDetail
{
    public class DeleteScoreDetailCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteScoreDetailCommand(string id)
        {
            Id = id;
        }
    }
}

