using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults
{
    public class CalculateRoundResultsCommandHandler : IRequestHandler<CalculateRoundResultsCommand, Result<CalculateRoundResultsResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public CalculateRoundResultsCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<CalculateRoundResultsResponseModel>> Handle(CalculateRoundResultsCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Round tồn tại
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.RoundId);
            if (round == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.RoundId}' không tồn tại.");
            }

            // 2. Quyền: chỉ EventCoordinator (hoặc Admin) được tính kết quả chính thức
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, round.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ EventCoordinator được tính kết quả vòng thi.");
            }

            // 2c. KHÔNG tính lại khi VÒNG SAU đã vận hành dựa trên kết quả hiện tại:
            //     đội đã nộp bài/đã có kết quả ở vòng sau mà kết quả vòng này đổi (IsAdvanced đổi)
            //     thì bài vòng sau thành mồ côi logic.
            var laterRoundIds = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .AsNoTracking()
                .Where(r => r.EventId == round.EventId && r.RoundNumber > round.RoundNumber)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);
            if (laterRoundIds.Count > 0)
            {
                var laterHasSubmissions = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                    sr => laterRoundIds.Contains(sr.Track!.RoundId), cancellationToken);
                var laterHasResults = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId != null && laterRoundIds.Contains(fr.RoundId), cancellationToken);
                if (laterHasSubmissions || laterHasResults)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Vòng sau đã có bài nộp/kết quả dựa trên kết quả vòng này nên không thể tính lại. Vui lòng xử lý dữ liệu vòng sau trước.");
                }
            }

            // 3. Lấy tất cả bài nộp trong vòng thi này
            var submissions = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .Where(s => s.Track.RoundId == request.RoundId && s.IsActive)
                .ToListAsync(cancellationToken);

            if (submissions.Count == 0)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi chưa có bài nộp nào để tính kết quả.");
            }

            // 4. Lấy các phiếu chấm của các bài nộp trong vòng — CHỈ tính phiếu thuộc vai trò GIÁM KHẢO.
            //    (Phòng thủ nhiều lớp: dù có phiếu do vai trò khác tạo lọt vào, cũng không được gộp vào
            //     điểm trung bình -> tránh sai kết quả.)
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var judgeRoleIds = await _unitOfWork.GetRepository<EventRole>().Entities
                .AsNoTracking()
                .Where(er => er.EventId == round.EventId && er.RoleName == EventRoleType.Judge)
                .Select(er => er.Id)
                .ToListAsync(cancellationToken);
            var scores = await _unitOfWork.GetRepository<Score>().Entities
                .Where(sc => submissionIds.Contains(sc.SubmitResultId) && judgeRoleIds.Contains(sc.EventRoleId))
                .ToListAsync(cancellationToken);

            var trackIds = submissions.Select(s => s.TrackId).Distinct().ToList();
            var nowUtc = System.DateTime.UtcNow;
            var judgeRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .AsNoTracking()
                .Where(er => er.RoleName == EventRoleType.Judge && er.TrackId != null && trackIds.Contains(er.TrackId)
                          && (er.ExpiredAt == null || er.ExpiredAt > nowUtc))   // bao gồm giám khảo vô thời hạn (null) hoặc chưa hết hạn
                .ToListAsync(cancellationToken);
            var scoredPairs = scores.Select(sc => sc.EventRoleId + "|" + sc.SubmitResultId).ToHashSet();

            var allRoundTracks = await _unitOfWork.GetRepository<Track>().GetQueryable()
                .AsNoTracking()
                .Where(t => t.RoundId == request.RoundId)
                .ToListAsync(cancellationToken);

            // 5. FIX: mỗi Team chỉ đăng ký và nộp bài ở ĐÚNG 1 Track cụ thể (xem Flow 3 — Team.EventId,
            //    không có TrackId ở cấp đăng ký đội). Vì vậy điểm/xếp hạng/thăng vòng phải tính RIÊNG
            //    theo TỪNG Track — KHÔNG được gộp trung bình mọi Track của cả vòng vào 1 điểm chung
            //    (bản cũ làm vậy khiến đội chỉ thi 1 hạng mục bị tính 0 điểm oan cho hạng mục mình
            //    không hề đăng ký). Round.AdvancementRule ("top:N"/"percent:P"/"minScore:X") áp dụng
            //    RIÊNG cho bảng xếp hạng của từng Track, không áp cho 1 bảng gộp toàn vòng.
            //
            //    Đồng thời: 1 Track chưa đủ giám khảo chấm xong sẽ bị BỎ QUA (không chặn cả vòng như
            //    bản cũ) — EC vẫn công bố được kết quả các Track đã sẵn sàng, không phải chờ Track
            //    chậm nhất.
            var resultModels = new List<CalculateRoundResultItemModel>();
            var skippedTracks = new List<SkippedTrackModel>();
            var tracksReady = new List<Track>();

            foreach (var track in allRoundTracks)
            {
                var trackSubmissions = submissions.Where(s => s.TrackId == track.Id).ToList();
                if (trackSubmissions.Count == 0)
                {
                    continue; // hạng mục này chưa có đội nào nộp bài — không có gì để tính
                }

                var trackJudges = judgeRoles.Where(j => j.TrackId == track.Id).ToList();
                int missingCount = trackJudges.Count == 0
                    ? trackSubmissions.Count // chưa được phân giám khảo nào
                    : trackSubmissions.Sum(sub => trackJudges.Count(j => !scoredPairs.Contains(j.Id + "|" + sub.Id)));

                if (missingCount > 0)
                {
                    skippedTracks.Add(new SkippedTrackModel
                    {
                        TrackId = track.Id,
                        TrackName = track.TrackName,
                        MissingScoreCount = missingCount
                    });
                    continue;
                }

                tracksReady.Add(track);
            }

            if (tracksReady.Count == 0)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Chưa hạng mục nào trong vòng đủ điều kiện tính kết quả (còn thiếu lượt chấm). Vui lòng đợi giám khảo chấm xong.");
            }

            // 6. Xoá kết quả cũ CỦA CÁC TRACK SẮP TÍNH LẠI (giữ nguyên kết quả track khác chưa sẵn sàng
            //    từ lần tính trước, nếu có — không xoá sạch cả vòng như bản cũ).
            var readyTrackIds = tracksReady.Select(t => t.Id).ToList();
            var oldResults = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(f => f.RoundId == request.RoundId && f.TrackId != null && readyTrackIds.Contains(f.TrackId))
                .ToListAsync(cancellationToken);
            if (oldResults.Count > 0)
            {
                await _unitOfWork.GetRepository<FinalResult>().DeleteRangeAsync(oldResults);
            }

            // 7. Với MỖI Track đã sẵn sàng: xếp hạng riêng các đội đã nộp bài ở Track đó, áp
            //    Round.AdvancementRule cho đúng bảng xếp hạng của Track đó.
            foreach (var track in tracksReady)
            {
                var trackSubmissions = submissions.Where(s => s.TrackId == track.Id).ToList();

                var teamScores = trackSubmissions
                    .Select(sub =>
                    {
                        var subScores = scores.Where(sc => sc.SubmitResultId == sub.Id).ToList();
                        // Làm tròn 2 số NGAY khi tính để xếp hạng khớp với điểm hiển thị/lưu.
                        decimal finalScore = subScores.Count > 0
                            ? System.Math.Round(subScores.Average(sc => sc.TotalScore), 2, System.MidpointRounding.AwayFromZero)
                            : 0m;
                        return new { sub.TeamId, FinalScore = finalScore };
                    })
                    .OrderByDescending(x => x.FinalScore)
                    .ThenBy(x => x.TeamId) // đồng hạng -> thứ tự ổn định
                    .ToList();

                int totalTeamsInTrack = teamScores.Count;
                int? cutoffRank = null;
                decimal? minScore = null;
                if (TryParseAdvancementRule(round.AdvancementRule, out var ruleType, out var ruleValue))
                {
                    switch (ruleType)
                    {
                        case "top": cutoffRank = (int)ruleValue; break;
                        case "percent": cutoffRank = (int)System.Math.Ceiling(totalTeamsInTrack * ruleValue / 100m); break;
                        case "minscore": minScore = ruleValue; break;
                        default: cutoffRank = request.TopN; break;
                    }
                }
                else
                {
                    cutoffRank = request.TopN;
                }

                // Xếp hạng kiểu thi đấu chuẩn (1-1-3): đội bằng điểm cùng hạng, đội kế tiếp nhảy hạng
                // theo số đội đứng trên. IsAdvanced = Rank <= ngưỡng -> đồng hạng ngay ngưỡng đều được thăng.
                int rank = 0;
                int position = 0;
                decimal? previousScore = null;
                foreach (var item in teamScores)
                {
                    position++;
                    if (previousScore == null || item.FinalScore != previousScore.Value)
                    {
                        rank = position;
                        previousScore = item.FinalScore;
                    }

                    var finalResult = new FinalResult
                    {
                        TeamId = item.TeamId,
                        RoundId = request.RoundId,
                        TrackId = track.Id, // kết quả RIÊNG theo từng hạng mục, không gộp cả vòng
                        EventId = round.EventId,
                        FinalScore = item.FinalScore,
                        Rank = rank,
                        IsAdvanced = minScore.HasValue
                            ? item.FinalScore >= minScore.Value
                            : rank <= cutoffRank!.Value,
                        // Tính kết quả tạo bản NHÁP (chưa công khai). EC/Admin rà soát rồi gọi Publish mới công bố.
                        IsPublished = false
                    };
                    await _unitOfWork.GetRepository<FinalResult>().AddAsync(finalResult);

                    resultModels.Add(new CalculateRoundResultItemModel
                    {
                        FinalResultId = finalResult.Id,
                        TeamId = finalResult.TeamId,
                        TrackId = track.Id,
                        FinalScore = finalResult.FinalScore,
                        Rank = finalResult.Rank,
                        IsAdvanced = finalResult.IsAdvanced
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CalculateRoundResultsResponseModel
            {
                Results = resultModels,
                SkippedTracks = skippedTracks
            };
        }

        /// <summary>
        /// Tách chuỗi quy tắc thăng vòng dạng "kiểu:giá_trị" (vd "top:5", "percent:50", "minScore:70").
        /// Trả về false nếu chuỗi trống / sai định dạng -> handler dùng fallback request.TopN.
        /// </summary>
        private static bool TryParseAdvancementRule(string? rule, out string type, out decimal value)
        {
            type = string.Empty;
            value = 0m;
            if (string.IsNullOrWhiteSpace(rule)) return false;

            var parts = rule.Split(':', 2);
            if (parts.Length != 2) return false;

            type = parts[0].Trim().ToLowerInvariant();
            return decimal.TryParse(
                parts[1].Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }
    }
}
