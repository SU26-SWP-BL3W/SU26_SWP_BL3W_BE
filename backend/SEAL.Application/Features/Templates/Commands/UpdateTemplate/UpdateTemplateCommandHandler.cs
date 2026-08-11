using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Templates.Commands.UpdateTemplate.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Commands.UpdateTemplate
{
    public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand, Result<UpdateTemplateResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTemplateCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateTemplateResponseModel>> Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _unitOfWork.GetRepository<Template>().GetByIdAsync(request.Id);
            if (template == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Mẫu tiêu chí có ID '{request.Id}' không tồn tại.");
            }

            var isDuplicate = await _unitOfWork.GetRepository<Template>().AnyAsync(
                t => t.TemplateName.ToLower() == request.Model.TemplateName.ToLower() && t.Id != request.Id,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Mẫu tiêu chí '{request.Model.TemplateName}' đã tồn tại.");
            }

            template.TemplateName = request.Model.TemplateName;
            template.Description = request.Model.Description;
            template.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<Template>().UpdateAsync(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateTemplateResponseModel
            {
                Id = template.Id,
                TemplateName = template.TemplateName,
                Description = template.Description,
                LastUpdatedTime = template.LastUpdatedTime
            };
        }
    }
}

