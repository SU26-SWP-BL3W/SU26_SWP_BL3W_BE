using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Scores.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Queries.GetScoreById
{
    public class GetScoreByIdQueryHandler : IRequestHandler<GetScoreByIdQuery, Result<ScoreModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetScoreByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ScoreModel?>> Handle(GetScoreByIdQuery request, CancellationToken cancellationToken)
        {
            var score = await _unitOfWork.GetRepository<Score>().GetByIdAsync(request.Id);
            if (score == null)
            {
                return null;
            }

            return new ScoreModel
            {
                Id = score.Id,
                EventRoleId = score.EventRoleId,
                SubmitResultId = score.SubmitResultId,
                TotalScore = score.TotalScore,
                Comment = score.Comment,
                CreatedTime = score.CreatedTime,
                LastUpdatedTime = score.LastUpdatedTime
            };
        }
    }
}

