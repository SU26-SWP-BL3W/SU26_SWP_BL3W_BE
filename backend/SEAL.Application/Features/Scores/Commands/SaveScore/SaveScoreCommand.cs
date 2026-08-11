using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Scores.Commands.SaveScore.Models;

namespace SEAL_Application.Features.Scores.Commands.SaveScore
{
    public class SaveScoreCommand : IRequest<Result<SaveScoreResponseModel>>
    {
        public SaveScoreRequestModel Model { get; set; } = new SaveScoreRequestModel();
    }
}

