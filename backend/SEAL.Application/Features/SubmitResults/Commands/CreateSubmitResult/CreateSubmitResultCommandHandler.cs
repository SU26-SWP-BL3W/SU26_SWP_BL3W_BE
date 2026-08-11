using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult
{
    public class CreateSubmitResultCommandHandler : IRequestHandler<CreateSubmitResultCommand, Result<CreateSubmitResultResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public CreateSubmitResultCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<CreateSubmitResultResponseModel>> Handle(CreateSubmitResultCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra tồn tại khóa ngoại TeamId trong DB (Fix 8)
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.Model.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Nhóm nộp bài không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            bool isTeamLeader = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId 
                      && er.TeamId == team.Id 
                      && er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                team.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isTeamLeader && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền nộp bài cho nhóm này.");
            }

            // 1b. Đội phải đã ĐĂNG KÝ CHÍNH THỨC (Registered) mới được nộp bài.
            //     Đội còn Forming (chưa chốt đủ 3-5 TV/duyệt, hoặc vừa bị reject đá về Forming) thì chưa được nộp.
            if (team.Status != SEAL_Domain.Entity.Enums.TeamStatus.Registered)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Đội chưa đăng ký chính thức (chưa chốt đủ thành viên/được duyệt) nên chưa thể nộp bài.");
            }

            // 2. Kiểm tra tồn tại Track
            if (string.IsNullOrEmpty(request.Model.TrackId))
            {
                return BaseException.BadRequestInvaildInputResponse("TrackId không được để trống.");
            }

            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.Model.TrackId);
            if (track == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Track không tồn tại.");
            }

            // 2b. HẠN NỘP: chỉ được nộp trong thời gian hạng mục (ưu tiên Track, fallback Round) đang mở.
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(track.RoundId);
            if (round == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi của hạng mục này không tồn tại.");
            }

            // 2c. Hạng mục phải thuộc ĐÚNG sự kiện mà đội đăng ký (chặn nộp chéo sự kiện).
            if (round.EventId != team.EventId)
            {
                return BaseException.BadRequestInvaildInputResponse("Hạng mục này không thuộc sự kiện mà đội đang tham gia.");
            }

            var now = DateTime.UtcNow;
            var effectiveStartDate = track.StartDate ?? round.StartDate;
            var effectiveEndDate = track.EndDate ?? round.EndDate;

            if (now < effectiveStartDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Hạng mục chưa mở, chưa thể nộp bài.");
            }
            if (now > effectiveEndDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Đã hết hạn nộp bài cho hạng mục này.");
            }

            // 2c'. Kết quả vòng đã được TÍNH/CÔNG BỐ thì khóa nộp bài mới (đối xứng với khóa sửa/xóa):
            //      tránh bài nộp "sinh sau" nằm ngoài kết quả đã công bố.
            var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                fr => fr.RoundId == round.Id, cancellationToken);
            if (roundPublished)
            {
                return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể nộp bài.");
            }

            // 2d. VÒNG SAU: đội phải ĐƯỢC ĐI TIẾP (IsAdvanced) từ vòng liền trước mới được nộp bài.
            //     Vòng đầu tiên (không có vòng nào nhỏ hơn) thì bỏ qua kiểm tra này.
            var prevRound = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .AsNoTracking()
                .Where(r => r.EventId == round.EventId && r.RoundNumber < round.RoundNumber)
                .OrderByDescending(r => r.RoundNumber)
                .FirstOrDefaultAsync(cancellationToken);
            if (prevRound != null)
            {
                var advanced = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.TeamId == team.Id && fr.RoundId == prevRound.Id && fr.IsAdvanced,
                    cancellationToken);
                if (!advanced)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        $"Đội chưa vượt qua vòng trước ('{prevRound.RoundName}') nên không thể nộp bài cho vòng này.");
                }
            }

            // 3. Chống nộp trùng: mỗi đội chỉ nộp 1 bài cho mỗi HẠNG MỤC (Track), không phải mỗi Vòng.
            //    Một vòng có thể có nhiều hạng mục diễn ra song song (vd AI + Web) -> đội phải nộp
            //    ĐỦ từng hạng mục riêng, mỗi hạng mục 1 bài (trước đây chặn nhầm theo Round khiến đội
            //    không thể nộp hạng mục thứ 2 trong cùng vòng).
            var isAlreadySubmitted = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                sr => sr.TeamId == request.Model.TeamId && sr.TrackId == request.Model.TrackId,
                cancellationToken);

            if (isAlreadySubmitted)
            {
                return BaseException.BadRequestDupplicationResponse("Nhóm đã nộp bài giải cho Hạng mục này trước đó.");
            }

            var submitResult = new SubmitResult
            {
                TeamId = request.Model.TeamId,
                TrackId = request.Model.TrackId,
                SubmissionUrl = request.Model.SubmissionUrl,
                Description = request.Model.Description,
                IsActive = true,
                CreatedBy = currentUserId // ghi lại AI nộp (leader hay EC nộp hộ) để đối chiếu khi có tranh chấp
            };

            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submitResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. CHỐNG RACE nộp trùng: 2 request song song có thể cùng qua check AnyAsync ở bước 3
            //    (check-rồi-mới-ghi có khoảng hở). Sau khi lưu, kiểm lại số bài của đội trong CÙNG
            //    HẠNG MỤC (không phải cùng vòng — 1 vòng nhiều hạng mục vẫn phải cho tồn tại song song):
            //    nếu >1 thì bài NỘP SỚM NHẤT thắng (CreatedTime, hòa thì so Id) — quy tắc thắng/thua
            //    xác định để mọi request race đều hội tụ về cùng kết luận -> cuối cùng còn đúng 1 bài.
            //    Ai phát hiện xung đột thì dọn TẤT CẢ bài thua (kể cả của request khác — cùng 1 đội,
            //    bài vừa tạo trong khoảnh khắc race, chưa thể có điểm).
            var rivals = await _unitOfWork.GetRepository<SubmitResult>().GetQueryable()
                .AsNoTracking()
                .Where(sr => sr.TeamId == request.Model.TeamId && sr.TrackId == request.Model.TrackId)
                .Select(sr => new { sr.Id, sr.CreatedTime })
                .ToListAsync(cancellationToken);
            if (rivals.Count > 1)
            {
                var winnerId = rivals.OrderBy(r => r.CreatedTime).ThenBy(r => r.Id, StringComparer.Ordinal).First().Id;
                var loserIds = rivals.Where(r => r.Id != winnerId).Select(r => r.Id).ToList();
                try
                {
                    var repo = _unitOfWork.GetRepository<SubmitResult>();
                    foreach (var loserId in loserIds)
                    {
                        var loser = await repo.GetByIdAsync(loserId);
                        if (loser != null) repo.Delete(loser);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    // Request race còn lại có thể đã dọn cùng lúc — bỏ qua lỗi dọn dẹp,
                    // trạng thái cuối vẫn hội tụ về 1 bài nhờ quy tắc thắng/thua xác định.
                }
                if (loserIds.Contains(submitResult.Id))
                {
                    return BaseException.BadRequestDupplicationResponse("Nhóm đã nộp bài giải cho Hạng mục này trước đó.");
                }
            }

            return new CreateSubmitResultResponseModel
            {
                Id = submitResult.Id,
                TeamId = submitResult.TeamId,
                TrackId = submitResult.TrackId,
                SubmissionUrl = submitResult.SubmissionUrl,
                Description = submitResult.Description ?? string.Empty,
                IsActive = submitResult.IsActive,
                CreatedTime = submitResult.CreatedTime
            };
        }
    }
}

