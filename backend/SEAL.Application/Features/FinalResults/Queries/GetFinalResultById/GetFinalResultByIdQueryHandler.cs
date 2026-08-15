using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Features.FinalResults.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Queries.GetFinalResultById
{
    public class GetFinalResultByIdQueryHandler : IRequestHandler<GetFinalResultByIdQuery, Result<FinalResultModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetFinalResultByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<FinalResultModel?>> Handle(GetFinalResultByIdQuery request, CancellationToken cancellationToken)
        {
            var finalResult = await _unitOfWork.GetRepository<FinalResult>().GetByIdAsync(request.Id);
            if (finalResult == null)
            {
                return null;
            }

            // CHỈ EventCoordinator (của sự kiện chứa kết quả này) hoặc Admin được xem kết quả NHÁP
            // (chưa công bố) — cùng chuẩn với GetFinalResultsByRoundIdQueryHandler.
            if (!finalResult.IsPublished && !await IsPrivilegedViewerAsync(finalResult, cancellationToken))
            {
                return null;
            }

            return new FinalResultModel
            {
                Id = finalResult.Id,
                TeamId = finalResult.TeamId,
                RoundId = finalResult.RoundId,
                EventId = finalResult.EventId,
                TrackId = finalResult.TrackId,
                PrizeId = finalResult.PrizeId,
                FinalScore = finalResult.FinalScore,
                Rank = finalResult.Rank,
                IsAdvanced = finalResult.IsAdvanced,
                IsPublished = finalResult.IsPublished,
                CreatedTime = finalResult.CreatedTime,
                LastUpdatedTime = finalResult.LastUpdatedTime
            };
        }

        /// <summary>EC của sự kiện chứa kết quả, hoặc Admin, được xem cả kết quả nháp.</summary>
        private async Task<bool> IsPrivilegedViewerAsync(FinalResult finalResult, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return false;
            }

            var eventId = await ResolveEventIdAsync(finalResult);
            if (string.IsNullOrEmpty(eventId))
            {
                return false;
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (user != null && user.IsAdmin)
            {
                return true;
            }

            return await _eventRoleChecker.HasRoleAsync(
                currentUserId, eventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
        }

        /// <summary>FinalResult chỉ mang đúng 1 trong 3 phạm vi (RoundId/EventId/TrackId) — resolve về EventId thật.</summary>
        private async Task<string?> ResolveEventIdAsync(FinalResult finalResult)
        {
            if (!string.IsNullOrEmpty(finalResult.EventId))
            {
                return finalResult.EventId;
            }

            if (!string.IsNullOrEmpty(finalResult.RoundId))
            {
                var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(finalResult.RoundId);
                return round?.EventId;
            }

            if (!string.IsNullOrEmpty(finalResult.TrackId))
            {
                var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(finalResult.TrackId);
                return track?.EventId;
            }

            return null;
        }
    }
}

