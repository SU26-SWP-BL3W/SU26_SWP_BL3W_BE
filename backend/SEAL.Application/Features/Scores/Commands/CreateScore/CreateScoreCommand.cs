using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Scores.Commands.CreateScore.Models;

namespace SEAL_Application.Features.Scores.Commands.CreateScore
{
    public class CreateScoreCommand : IRequest<Result<CreateScoreResponseModel>>
    {
        public CreateScoreRequestModel Model { get; set; } = new CreateScoreRequestModel();
    }
}

