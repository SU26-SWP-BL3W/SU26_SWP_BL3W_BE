using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Templates.Commands.DeleteTemplate
{
    public class DeleteTemplateCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteTemplateCommand(string id)
        {
            Id = id;
        }
    }
}

