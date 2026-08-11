using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Templates.Models;
using SEAL_Domain.Base;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Queries.GetTemplateById
{
    public class GetTemplateByIdQueryHandler : IRequestHandler<GetTemplateByIdQuery, Result<TemplateModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTemplateByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TemplateModel>> Handle(GetTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            var template = await _unitOfWork.GetRepository<Template>().Entities
                .Include(t => t.TemplateCriterias)
                .ThenInclude(tc => tc.Criteria)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (template == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Mẫu tiêu chí có ID '{request.Id}' không tồn tại.");
            }

            var model = new TemplateModel
            {
                Id = template.Id,
                TemplateName = template.TemplateName,
                Description = template.Description,
                CreatedTime = template.CreatedTime,
                LastUpdatedTime = template.LastUpdatedTime,
                Criterias = template.TemplateCriterias.Select(tc => new TemplateCriteriaModel
                {
                    CriteriaId = tc.CriteriaId,
                    CriteriaName = tc.Criteria.CriteriaName,
                    Description = tc.Criteria.Description,
                    Weight = tc.Weight,
                    MaxScore = tc.MaxScore
                }).ToList()
            };

            return model;
        }
    }
}

