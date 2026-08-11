using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Templates.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Templates.Queries.GetAllTemplates
{
    public class GetAllTemplatesQuery : BasePaginationQuery, IRequest<Result<PagedResult<TemplateModel>>>
    {
        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "TemplateName", "CreatedTime" };
    }
}

