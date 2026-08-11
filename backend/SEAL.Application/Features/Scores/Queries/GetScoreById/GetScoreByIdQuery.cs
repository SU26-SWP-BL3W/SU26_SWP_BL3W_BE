using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Scores.Models;

namespace SEAL_Application.Features.Scores.Queries.GetScoreById
{
    public class GetScoreByIdQuery : IRequest<Result<ScoreModel?>>
    {
        public string Id { get; set; }

        public GetScoreByIdQuery(string id)
        {
            Id = id;
        }
    }
}

