using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Templates.Commands.UpdateTemplateCriteriaConfig.Models;

namespace SEAL_Application.Features.Templates.Commands.UpdateTemplateCriteriaConfig
{
    public class UpdateTemplateCriteriaConfigCommand : IRequest<Result<bool>>
    {
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public UpdateTemplateCriteriaConfigRequestModel Model { get; set; } = null!;
    }
}

