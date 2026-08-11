using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.FinalResults.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Queries.GetFinalResultsByTeamId
{
    public class GetFinalResultsByTeamIdQueryHandler : IRequestHandler<GetFinalResultsByTeamIdQuery, Result<PagedResult<FinalResultModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetFinalResultsByTeamIdQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<FinalResultModel>>> Handle(GetFinalResultsByTeamIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(f => f.TeamId == request.TeamId);

            // Chỉ Admin xem được kết quả NHÁP trong lịch sử đội; người khác chỉ thấy đã công bố.
            // (Lịch sử đội trải nhiều sự kiện nên không lọc theo EC từng vòng — giữ đơn giản, an toàn.)
            if (!await IsAdminAsync())
            {
                query = query.Where(f => f.IsPublished);
            }

            var pagedResults = await query.ToPagedResultAsync(
                request,
                f => new FinalResultModel
                {
                    Id = f.Id,
                    TeamId = f.TeamId,
                    RoundId = f.RoundId,
                    EventId = f.EventId,
                    TrackId = f.TrackId,
                    PrizeId = f.PrizeId,
                    FinalScore = f.FinalScore,
                    Rank = f.Rank,
                    IsAdvanced = f.IsAdvanced,
                    IsPublished = f.IsPublished,
                    CreatedTime = f.CreatedTime,
                    LastUpdatedTime = f.LastUpdatedTime
                },
                cancellationToken
            );

            return pagedResults;
        }

        private async Task<bool> IsAdminAsync()
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return false;
            }
            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            return user != null && user.IsAdmin;
        }
    }
}



