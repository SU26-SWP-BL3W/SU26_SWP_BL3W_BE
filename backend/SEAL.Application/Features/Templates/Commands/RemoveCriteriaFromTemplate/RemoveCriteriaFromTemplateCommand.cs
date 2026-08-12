using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Templates.Commands.RemoveCriteriaFromTemplate
{
    public class RemoveCriteriaFromTemplateCommand : IRequest<Result<bool>>
    {
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
    }
}

