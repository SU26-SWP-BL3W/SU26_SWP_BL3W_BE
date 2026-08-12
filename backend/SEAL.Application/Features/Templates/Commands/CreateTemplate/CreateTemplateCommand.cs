using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Templates.Commands.CreateTemplate.Models;

namespace SEAL_Application.Features.Templates.Commands.CreateTemplate
{
    public class CreateTemplateCommand : IRequest<Result<CreateTemplateResponseModel>>
    {
        public CreateTemplateRequestModel Model { get; set; } = null!;
    }
}

