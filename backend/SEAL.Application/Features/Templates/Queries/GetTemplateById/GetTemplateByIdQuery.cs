using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Templates.Models;

namespace SEAL_Application.Features.Templates.Queries.GetTemplateById
{
    public class GetTemplateByIdQuery : IRequest<Result<TemplateModel>>
    {
        public string Id { get; set; }

        public GetTemplateByIdQuery(string id)
        {
            Id = id;
        }
    }
}

