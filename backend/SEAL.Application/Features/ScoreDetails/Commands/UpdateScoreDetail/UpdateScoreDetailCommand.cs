using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.ScoreDetails.Commands.UpdateScoreDetail.Models;

namespace SEAL_Application.Features.ScoreDetails.Commands.UpdateScoreDetail
{
    public class UpdateScoreDetailCommand : IRequest<Result<UpdateScoreDetailResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateScoreDetailRequestModel Model { get; set; } = new UpdateScoreDetailRequestModel();
    }
}

