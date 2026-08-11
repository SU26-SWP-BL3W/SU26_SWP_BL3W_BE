using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Scores.Queries.GetScoreDetail.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Queries.GetScoreDetail
{
    /// <summary>
    /// Lấy 1 phiếu chấm kèm toàn bộ điểm chi tiết theo từng tiêu chí trong 1 lần truy vấn.
    /// </summary>
    public class GetScoreDetailQueryHandler : IRequestHandler<GetScoreDetailQuery, Result<ScoreWithDetailsModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetScoreDetailQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ScoreWithDetailsModel?>> Handle(GetScoreDetailQuery request, CancellationToken cancellationToken)
        {
            var score = await _unitOfWork.GetRepository<Score>().Entities
                .AsNoTracking()
                .Include(s => s.ScoreDetails)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (score == null)
            {
                return null;
            }

            return new ScoreWithDetailsModel
            {
                Id = score.Id,
                EventRoleId = score.EventRoleId,
                SubmitResultId = score.SubmitResultId,
                TotalScore = score.TotalScore,
                Comment = score.Comment,
                IsSubmitted = score.IsSubmitted,
                CreatedTime = score.CreatedTime,
                LastUpdatedTime = score.LastUpdatedTime,
                Details = score.ScoreDetails
                    .OrderBy(d => d.TemplateId).ThenBy(d => d.CriteriaId)
                    .Select(d => new ScoreDetailLine
                    {
                        Id = d.Id,
                        TemplateId = d.TemplateId,
                        CriteriaId = d.CriteriaId,
                        Value = d.Value,
                        CreatedTime = d.CreatedTime,
                        LastUpdatedTime = d.LastUpdatedTime
                    })
                    .ToList()
            };
        }
    }
}

