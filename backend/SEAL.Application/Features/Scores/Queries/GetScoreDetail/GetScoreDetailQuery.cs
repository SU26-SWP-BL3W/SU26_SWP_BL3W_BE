using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Scores.Queries.GetScoreDetail.Models;

namespace SEAL_Application.Features.Scores.Queries.GetScoreDetail
{
    public class GetScoreDetailQuery : IRequest<Result<ScoreWithDetailsModel?>>
    {
        public string Id { get; set; }

        public GetScoreDetailQuery(string id)
        {
            Id = id;
        }
    }
}

