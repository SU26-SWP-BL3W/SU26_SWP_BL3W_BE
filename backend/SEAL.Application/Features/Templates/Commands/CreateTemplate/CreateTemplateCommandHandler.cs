using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Templates.Commands.CreateTemplate.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Commands.CreateTemplate
{
    public class CreateTemplateCommandHandler : IRequestHandler<CreateTemplateCommand, Result<CreateTemplateResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateTemplateCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateTemplateResponseModel>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
        {
            var isDuplicate = await _unitOfWork.GetRepository<Template>().AnyAsync(
                t => t.TemplateName.ToLower() == request.Model.TemplateName.ToLower(),
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Mẫu tiêu chí '{request.Model.TemplateName}' đã tồn tại.");
            }

            var template = new Template
            {
                TemplateName = request.Model.TemplateName,
                Description = request.Model.Description
            };

            await _unitOfWork.GetRepository<Template>().AddAsync(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateTemplateResponseModel
            {
                Id = template.Id,
                TemplateName = template.TemplateName,
                Description = template.Description,
                CreatedTime = template.CreatedTime
            };
        }
    }
}

