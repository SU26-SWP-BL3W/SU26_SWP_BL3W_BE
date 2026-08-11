using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.ScoreDetails.Models;

namespace SEAL_Application.Features.ScoreDetails.Queries.GetScoreDetailById
{
    public class GetScoreDetailByIdQuery : IRequest<Result<ScoreDetailModel?>>
    {
        public string Id { get; set; }

        public GetScoreDetailByIdQuery(string id)
        {
            Id = id;
        }
    }
}

