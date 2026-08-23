using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Scores.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Queries.GetScoresByEventRoleId
{
    public class GetScoresByEventRoleIdQueryHandler : IRequestHandler<GetScoresByEventRoleIdQuery, Result<PagedResult<ScoreModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetScoresByEventRoleIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<ScoreModel>>> Handle(GetScoresByEventRoleIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<Score>().Entities
                .Where(s => s.EventRoleId == request.EventRoleId);

            var pagedScores = await query.ToPagedResultAsync(
                request,
                s => new ScoreModel
                {
                    Id = s.Id,
                    EventRoleId = s.EventRoleId,
                    SubmitResultId = s.SubmitResultId,
                    TotalScore = s.TotalScore,
                    Comment = s.Comment,
                    IsSubmitted = s.IsSubmitted,
                    CreatedTime = s.CreatedTime,
                    LastUpdatedTime = s.LastUpdatedTime,
                    Details = s.ScoreDetails.Select(d => new ScoreDetailItemModel
                    {
                        Id = d.Id,
                        TemplateId = d.TemplateId,
                        CriteriaId = d.CriteriaId,
                        Value = d.Value
                    }).ToList()
                },
                cancellationToken
            );

            return pagedScores;
        }
    }
}



