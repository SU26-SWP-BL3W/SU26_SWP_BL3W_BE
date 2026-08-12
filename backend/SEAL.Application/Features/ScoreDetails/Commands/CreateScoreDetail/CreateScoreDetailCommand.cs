using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail.Models;

namespace SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail
{
    public class CreateScoreDetailCommand : IRequest<Result<CreateScoreDetailResponseModel>>
    {
        public CreateScoreDetailRequestModel Model { get; set; } = new CreateScoreDetailRequestModel();
    }
}

