using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.FinalResults.Models;

namespace SEAL_Application.Features.FinalResults.Queries.GetFinalResultById
{
    public class GetFinalResultByIdQuery : IRequest<Result<FinalResultModel?>>
    {
        public string Id { get; set; }

        public GetFinalResultByIdQuery(string id)
        {
            Id = id;
        }
    }
}

