using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Rounds.Models;

namespace SEAL_Application.Features.Rounds.Queries.GetRoundById
{
    public class GetRoundByIdQuery : IRequest<Result<RoundModel?>>
    {
        public string Id { get; set; }

        public GetRoundByIdQuery(string id)
        {
            Id = id;
        }
    }
}

