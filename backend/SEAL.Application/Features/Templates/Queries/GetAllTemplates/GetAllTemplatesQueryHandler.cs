using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Templates.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Templates.Queries.GetAllTemplates
{
    public class GetAllTemplatesQueryHandler : IRequestHandler<GetAllTemplatesQuery, Result<PagedResult<TemplateModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllTemplatesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<TemplateModel>>> Handle(GetAllTemplatesQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<Template>().Entities;

            return await query.ToPagedResultAsync(
                request,
                t => new TemplateModel
                {
                    Id = t.Id,
                    TemplateName = t.TemplateName,
                    Description = t.Description,
                    CreatedTime = t.CreatedTime,
                    LastUpdatedTime = t.LastUpdatedTime,
                    // Kèm danh sách tiêu chí để FE lọc trùng khi thêm tiêu chí (getAvailable dựa vào đây).
                    Criterias = t.TemplateCriterias.Select(tc => new TemplateCriteriaModel
                    {
                        CriteriaId = tc.CriteriaId,
                        CriteriaName = tc.Criteria.CriteriaName,
                        Description = tc.Criteria.Description,
                        Weight = tc.Weight,
                        MaxScore = tc.MaxScore
                    }).ToList()
                },
                cancellationToken);
        }
    }
}



