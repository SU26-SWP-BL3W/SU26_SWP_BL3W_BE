// [FLOW3-CHAMDIEM][GetTrackCalibration] Ma tran diem GK x tieu chi de do do lech (RBL).

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Scores.Queries.GetTrackCalibration.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Queries.GetTrackCalibration
{
    public class GetTrackCalibrationQueryHandler : IRequestHandler<GetTrackCalibrationQuery, Result<TrackCalibrationModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetTrackCalibrationQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<TrackCalibrationModel>> Handle(GetTrackCalibrationQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.TrackId);
            if (track == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Hạng mục '{request.TrackId}' không tồn tại.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, track.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ Admin hoặc Điều phối viên được xem ma trận hiệu chuẩn.");
            }

            var nowUtc = DateTime.UtcNow;
            var judges = await _unitOfWork.GetRepository<EventRole>().Entities
                .AsNoTracking()
                .Include(er => er.User)
                .Include(er => er.Event)
                .Where(er => er.TrackId == track.Id
                          && er.RoleName == EventRoleType.Judge
                          && (er.ExpiredAt ?? er.Event.EndDate) > nowUtc)
                .ToListAsync(cancellationToken);

            var submissions = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .AsNoTracking()
                .Include(s => s.Team)
                .Where(s => s.TrackId == track.Id && s.IsActive
                          && (s.Team == null || s.Team.Status != TeamStatus.Disqualified))
                .ToListAsync(cancellationToken);

            var submissionIds = submissions.Select(s => s.Id).ToList();
            var judgeIds = judges.Select(j => j.Id).ToList();

            var scores = submissionIds.Count == 0 || judgeIds.Count == 0
                ? new List<Score>()
                : await _unitOfWork.GetRepository<Score>().Entities
                    .AsNoTracking()
                    .Include(sc => sc.ScoreDetails)
                    .Where(sc => submissionIds.Contains(sc.SubmitResultId) && judgeIds.Contains(sc.EventRoleId))
                    .ToListAsync(cancellationToken);

            var scoreMap = scores.ToDictionary(sc => sc.EventRoleId + "|" + sc.SubmitResultId, sc => sc);

            int missing = 0;
            var rows = new List<CalibrationScoreRow>();
            foreach (var judge in judges)
            {
                foreach (var sub in submissions)
                {
                    scoreMap.TryGetValue(judge.Id + "|" + sub.Id, out var score);
                    if (score == null || !score.IsSubmitted) missing++;
                    rows.Add(new CalibrationScoreRow
                    {
                        JudgeId = judge.Id,
                        JudgeName = judge.User?.FullName ?? judge.Id,
                        SubmitResultId = sub.Id,
                        TeamName = sub.Team?.Name ?? sub.TeamId,
                        TotalScore = score?.TotalScore ?? 0m,
                        IsSubmitted = score?.IsSubmitted ?? false,
                        IsAccepted = score?.IsSubmitted ?? false
                    });
                }
            }

            var criteriaIds = scores.SelectMany(s => s.ScoreDetails).Select(d => d.CriteriaId).Distinct().ToList();
            var criteriaNames = criteriaIds.Count == 0
                ? new Dictionary<string, string>()
                : await _unitOfWork.GetRepository<Criteria>().Entities
                    .AsNoTracking()
                    .Where(c => criteriaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.CriteriaName, cancellationToken);

            var stats = scores
                .SelectMany(s => s.ScoreDetails)
                .GroupBy(d => d.CriteriaId)
                .Select(g =>
                {
                    var values = g.Select(x => x.Value).ToList();
                    decimal mean = values.Average();
                    decimal std = values.Count > 1
                        ? (decimal)Math.Sqrt(values.Select(v => (double)((v - mean) * (v - mean))).Average())
                        : 0m;
                    return new CalibrationCriteriaStat
                    {
                        CriteriaId = g.Key,
                        CriteriaName = criteriaNames.TryGetValue(g.Key, out var name) ? name : g.Key,
                        Mean = Math.Round(mean, 2, MidpointRounding.AwayFromZero),
                        StdDev = Math.Round(std, 2, MidpointRounding.AwayFromZero),
                        Min = values.Min(),
                        Max = values.Max(),
                        SampleCount = values.Count
                    };
                })
                .OrderBy(s => s.CriteriaName)
                .ToList();

            var judgeStats = scores
                .Where(s => s.IsSubmitted)
                .GroupBy(s => s.EventRoleId)
                .Select(g =>
                {
                    var values = g.Select(x => x.TotalScore).ToList();
                    decimal mean = values.Average();
                    decimal std = values.Count > 1
                        ? (decimal)Math.Sqrt(values.Select(v => (double)((v - mean) * (v - mean))).Average())
                        : 0m;
                    var judge = judges.FirstOrDefault(j => j.Id == g.Key);
                    return new CalibrationJudgeStat
                    {
                        JudgeId = g.Key,
                        JudgeName = judge?.User?.FullName ?? g.Key,
                        Mean = Math.Round(mean, 2, MidpointRounding.AwayFromZero),
                        StdDev = Math.Round(std, 2, MidpointRounding.AwayFromZero),
                        Min = values.Min(),
                        Max = values.Max(),
                        SampleCount = values.Count
                    };
                })
                .OrderBy(s => s.JudgeName)
                .ToList();

            bool isCompleted = judges.Count > 0 && submissions.Count > 0 && missing == 0;

            return new TrackCalibrationModel
            {
                TrackId = track.Id,
                TrackName = track.TrackName,
                IsCompleted = isCompleted,
                Scores = rows,
                CriteriaStats = stats,
                JudgeStats = judgeStats
            };
        }
    }
}
