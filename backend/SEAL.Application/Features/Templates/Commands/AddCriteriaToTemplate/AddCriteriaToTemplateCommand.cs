using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Templates.Commands.AddCriteriaToTemplate.Models;

namespace SEAL_Application.Features.Templates.Commands.AddCriteriaToTemplate
{
    public class AddCriteriaToTemplateCommand : IRequest<Result<bool>>
    {
        public string TemplateId { get; set; } = string.Empty;
        public AddCriteriaToTemplateRequestModel Model { get; set; } = null!;
    }
}

