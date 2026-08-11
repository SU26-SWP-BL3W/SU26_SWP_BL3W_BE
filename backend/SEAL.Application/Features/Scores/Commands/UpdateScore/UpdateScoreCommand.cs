using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Scores.Commands.UpdateScore.Models;

namespace SEAL_Application.Features.Scores.Commands.UpdateScore
{
    public class UpdateScoreCommand : IRequest<Result<UpdateScoreResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateScoreRequestModel Model { get; set; } = new UpdateScoreRequestModel();
    }
}

