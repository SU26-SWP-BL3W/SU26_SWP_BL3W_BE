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
    public class CalculateRoundResultsCommandHandler : IRequestHandler<CalculateRoundResultsCommand, Result<List<CalculateRoundResultItemModel>>>
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

        public async Task<Result<List<CalculateRoundResultItemModel>>> Handle(CalculateRoundResultsCommand request, CancellationToken cancellationToken)
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

            // 4b. Chưa cho tính kết quả nếu chưa ĐỦ giám khảo chấm:
            //     mỗi bài nộp active phải được TẤT CẢ giám khảo được phân của track đó chấm.
            //     (Tránh đội chưa được chấm/chấm thiếu bị tính FinalScore = 0 rồi loại oan.)
            var trackIds = submissions.Select(s => s.TrackId).Distinct().ToList();
            var nowUtc = System.DateTime.UtcNow;
            var judgeRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .AsNoTracking()
                .Where(er => er.RoleName == EventRoleType.Judge && er.TrackId != null && trackIds.Contains(er.TrackId)
                          && (er.ExpiredAt == null || er.ExpiredAt > nowUtc))   // bao gồm giám khảo vô thời hạn (null) hoặc chưa hết hạn
                .ToListAsync(cancellationToken);
            var scoredPairs = scores.Select(sc => sc.EventRoleId + "|" + sc.SubmitResultId).ToHashSet();
            int missingCount = 0;
            foreach (var sub in submissions)
            {
                var trackJudges = judgeRoles.Where(j => j.TrackId == sub.TrackId).ToList();
                if (trackJudges.Count == 0)
                {
                    missingCount++;   // bài nộp chưa được phân giám khảo nào
                    continue;
                }
                missingCount += trackJudges.Count(j => !scoredPairs.Contains(j.Id + "|" + sub.Id));
            }
            if (missingCount > 0)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    $"Chưa thể tính kết quả: còn {missingCount} lượt chấm chưa hoàn tất (giám khảo chưa chấm hết bài được phân). Vui lòng đợi tất cả giám khảo chấm xong.");
            }

            // 5. Tính FinalScore mỗi đội = TRUNG BÌNH ĐIỂM TỪNG HẠNG MỤC của vòng (không phải trung bình
            //    phẳng mọi phiếu chấm gộp lại). Vòng có N hạng mục -> đội phải có N điểm hạng mục để tính
            //    trung bình đúng; hạng mục đã HẾT HẠN NỘP mà đội không nộp thì tính điểm hạng mục đó = 0
            //    (không được bỏ qua/coi như "vô hình" — nếu không đội bỏ 1 hạng mục vẫn được tính y như
            //    thể vòng chỉ có 1 hạng mục). Hạng mục CHƯA hết hạn mà đội chưa nộp thì chưa ép 0 (đội vẫn
            //    còn quyền nộp), tạm bỏ qua khỏi phép tính lần này.
            var allRoundTracks = await _unitOfWork.GetRepository<Track>().GetQueryable()
                .AsNoTracking()
                .Where(t => t.RoundId == request.RoundId)
                .ToListAsync(cancellationToken);

            var teamFinalScores = submissions
                .Select(s => s.TeamId)
                .Distinct()
                .Select(teamId =>
                {
                    var perTrackScores = new List<decimal>();
                    foreach (var trk in allRoundTracks)
                    {
                        var sub = submissions.FirstOrDefault(s => s.TeamId == teamId && s.TrackId == trk.Id);
                        if (sub != null)
                        {
                            // Điểm hạng mục = trung bình các phiếu chấm (mọi giám khảo) cho bài nộp đó.
                            var subScores = scores.Where(sc => sc.SubmitResultId == sub.Id).ToList();
                            if (subScores.Count > 0)
                            {
                                perTrackScores.Add(subScores.Average(sc => sc.TotalScore));
                            }
                        }
                        else
                        {
                            var trackEffectiveEnd = trk.EndDate ?? round.EndDate;
                            if (nowUtc > trackEffectiveEnd)
                            {
                                // Hết hạn nộp mà không nộp -> tính 0 cho hạng mục này (KHÔNG bỏ qua).
                                perTrackScores.Add(0m);
                            }
                            // Chưa hết hạn -> chưa ép 0, tạm bỏ qua hạng mục này khỏi trung bình lần tính này.
                        }
                    }

                    // Làm tròn 2 số NGAY khi tính để xếp hạng khớp với điểm hiển thị/lưu (tránh 7.004 vs 7.001
                    // cùng hiện 7.00 nhưng khác hạng).
                    decimal finalScore = perTrackScores.Count > 0
                        ? System.Math.Round(perTrackScores.Average(), 2, System.MidpointRounding.AwayFromZero)
                        : 0m;
                    return new { TeamId = teamId, FinalScore = finalScore };
                })
                // 6. Xếp hạng theo điểm giảm dần (ThenBy TeamId để thứ tự ổn định khi đồng hạng)
                .OrderByDescending(x => x.FinalScore)
                .ThenBy(x => x.TeamId)
                .ToList();

            // 7. Xóa kết quả cũ của vòng (tính lại từ đầu)
            var oldResults = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(f => f.RoundId == request.RoundId)
                .ToListAsync(cancellationToken);
            if (oldResults.Count > 0)
            {
                await _unitOfWork.GetRepository<FinalResult>().DeleteRangeAsync(oldResults);
            }

            // 7b. Cách thăng vòng: đọc Round.AdvancementRule ("top:N" | "percent:P" | "minScore:X").
            //     Sai định dạng / để trống -> fallback về request.TopN (số nhập tay).
            int totalTeams = teamFinalScores.Count;
            int? cutoffRank = null;     // quy tắc 1 (top N) & 2 (top %) dùng ngưỡng HẠNG
            decimal? minScore = null;   // quy tắc 3 (từ X điểm) dùng ngưỡng ĐIỂM
            if (TryParseAdvancementRule(round.AdvancementRule, out var ruleType, out var ruleValue))
            {
                switch (ruleType)
                {
                    case "top":      cutoffRank = (int)ruleValue; break;
                    case "percent":  cutoffRank = (int)System.Math.Ceiling(totalTeams * ruleValue / 100m); break;
                    case "minscore": minScore = ruleValue; break;
                    default:         cutoffRank = request.TopN; break;   // quy tắc lạ -> fallback
                }
            }
            else
            {
                cutoffRank = request.TopN;   // không có rule -> dùng TopN nhập tay
            }

            // 8. Gán Rank + IsAdvanced và tạo FinalResult mới.
            //    Xếp hạng kiểu thi đấu chuẩn (standard competition ranking "1-1-3"):
            //    các đội bằng điểm cùng hạng; đội kế tiếp nhảy hạng theo số đội đứng trên.
            //    IsAdvanced = Rank <= TopN -> nếu đồng hạng ngay ngưỡng top N thì mọi đội đồng hạng đó đều được thăng.
            var resultModels = new List<CalculateRoundResultItemModel>();
            int rank = 0;
            int position = 0;
            decimal? previousScore = null;
            foreach (var item in teamFinalScores)
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
                    // Gán luôn EventId (suy từ Round) để API đọc kết quả trả về đủ phạm vi, không bắt
                    // client phải tra ngược qua RoundId. TrackId để null vì kết quả này tính theo VÒNG
                    // (gộp mọi hạng mục của vòng), không phải kết quả riêng của một hạng mục.
                    EventId = round.EventId,
                    FinalScore = item.FinalScore,
                    Rank = rank,
                    IsAdvanced = minScore.HasValue
                        ? item.FinalScore >= minScore.Value      // quy tắc 3: đội đạt từ X điểm trở lên
                        : rank <= cutoffRank!.Value,             // quy tắc 1 (top N) & 2 (top %); đồng hạng ở vạch đều thăng
                    // Tính kết quả tạo bản NHÁP (chưa công khai). EC/Admin rà soát rồi gọi Publish mới công bố.
                    IsPublished = false
                };
                await _unitOfWork.GetRepository<FinalResult>().AddAsync(finalResult);

                resultModels.Add(new CalculateRoundResultItemModel
                {
                    FinalResultId = finalResult.Id,
                    TeamId = finalResult.TeamId,
                    FinalScore = finalResult.FinalScore,
                    Rank = finalResult.Rank,
                    IsAdvanced = finalResult.IsAdvanced
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return resultModels;
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




