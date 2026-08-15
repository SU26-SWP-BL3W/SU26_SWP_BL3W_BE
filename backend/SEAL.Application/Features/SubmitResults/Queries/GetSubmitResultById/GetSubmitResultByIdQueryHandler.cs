// [FLOW3-NOPBAI][GetSubmitResultById] Xem chi tiet 1 bai nop gom cac duong link va thoi gian nop.

using MediatR;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById
{
    public class GetSubmitResultByIdQueryHandler : IRequestHandler<GetSubmitResultByIdQuery, Result<SubmitResultDetailResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetSubmitResultByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<SubmitResultDetailResponseModel>> Handle(GetSubmitResultByIdQuery request, CancellationToken cancellationToken)
        {
            var sr = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(request.Id);
            if (sr == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Bài n?p không t?n t?i.");
            }

            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(sr.TeamId);
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(sr.TrackId);

            string teamName = team?.Name ?? "Không xác ??nh";
            string teamId = sr.TeamId;
            var userId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(userId) && team != null)
            {
                var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
                bool isAdmin = user != null && user.IsAdmin;
                bool isEc = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                    er => er.UserId == userId && er.EventId == team.EventId && er.RoleName == EventRoleType.EventCoordinator,
                    cancellationToken);
                bool isJudge = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                    er => er.UserId == userId && er.EventId == team.EventId && er.RoleName == EventRoleType.Judge,
                    cancellationToken);
                if (!isAdmin && !isEc && isJudge)
                {
                    teamName = "Bài n?p ?n danh";
                    teamId = string.Empty;
                }
            }

            return new SubmitResultDetailResponseModel
            {
                Id = sr.Id,
                TeamId = teamId,
                TeamName = teamName,
                TrackId = sr.TrackId,
                TrackName = track?.TrackName ?? "Không xác ??nh",
                SubmissionUrl = sr.SubmissionUrl,
                RepoUrl = sr.RepoUrl,
                DemoUrl = sr.DemoUrl,
                SlideUrl = sr.SlideUrl,
                RepoHost = sr.RepoHost,
                RepoFullName = sr.RepoFullName,
                RepoStars = sr.RepoStars,
                RepoLastPush = sr.RepoLastPush,
                Description = sr.Description ?? string.Empty,
                IsActive = sr.IsActive,
                CreatedTime = sr.CreatedTime
            };
        }
    }
}
