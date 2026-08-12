using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Templates.Commands.UpdateTemplate.Models;

namespace SEAL_Application.Features.Templates.Commands.UpdateTemplate
{
    public class UpdateTemplateCommand : IRequest<Result<UpdateTemplateResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateTemplateRequestModel Model { get; set; } = null!;
    }
}

