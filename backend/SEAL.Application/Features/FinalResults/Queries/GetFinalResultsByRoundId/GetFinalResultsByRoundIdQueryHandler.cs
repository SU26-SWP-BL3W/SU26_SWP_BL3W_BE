using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.FinalResults.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Queries.GetFinalResultsByRoundId
{
    public class GetFinalResultsByRoundIdQueryHandler : IRequestHandler<GetFinalResultsByRoundIdQuery, Result<PagedResult<FinalResultModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetFinalResultsByRoundIdQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<PagedResult<FinalResultModel>>> Handle(GetFinalResultsByRoundIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(f => f.RoundId == request.RoundId);

            if (!string.IsNullOrWhiteSpace(request.TrackId))
            {
                query = query.Where(f => f.TrackId == request.TrackId);
            }

            // CHỈ EventCoordinator (của sự kiện chứa vòng này) hoặc Admin được xem kết quả NHÁP
            // (chưa công bố). Mọi người khác chỉ thấy kết quả ĐÃ công bố (IsPublished = true).
            if (!await IsPrivilegedViewerAsync(request.RoundId, cancellationToken))
            {
                query = query.Where(f => f.IsPublished);
            }

            // Mặc định sắp xếp theo Rank tăng dần (bảng xếp hạng) khi client không chỉ định.
            if (string.IsNullOrEmpty(request.SortBy))
            {
                request.SortBy = "Rank";
                request.IsAscending = true;
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

        /// <summary>EC của sự kiện chứa vòng, hoặc Admin, được xem cả kết quả nháp.</summary>
        private async Task<bool> IsPrivilegedViewerAsync(string roundId, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return false;
            }

            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(roundId);
            if (round == null)
            {
                return false;
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (user != null && user.IsAdmin)
            {
                return true;
            }

            return await _eventRoleChecker.HasRoleAsync(
                currentUserId, round.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
        }
    }
}



