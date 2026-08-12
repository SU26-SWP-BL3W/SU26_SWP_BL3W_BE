using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Criterias.Models;

namespace SEAL_Application.Features.Criterias.Queries.GetCriteriaById
{
    public class GetCriteriaByIdQuery : IRequest<Result<CriteriaModel>>
    {
        public string Id { get; set; }

        public GetCriteriaByIdQuery(string id)
        {
            Id = id;
        }
    }
}

