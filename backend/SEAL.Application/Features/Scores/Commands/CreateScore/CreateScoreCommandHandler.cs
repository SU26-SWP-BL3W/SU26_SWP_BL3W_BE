using MediatR;
using SEAL_Application.Features.Demo;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Scores.Commands.CreateScore.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Commands.CreateScore
{
    public class CreateScoreCommandHandler : IRequestHandler<CreateScoreCommand, Result<CreateScoreResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public CreateScoreCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<CreateScoreResponseModel>> Handle(CreateScoreCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra EventRole (giám khảo) có tồn tại
            var eventRole = await _unitOfWork.GetRepository<EventRole>()
                .GetByIdAsync(request.Model.EventRoleId);
            if (eventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vai trò chấm điểm có ID '{request.Model.EventRoleId}' không tồn tại.");
            }

            // 1a. Phiếu chấm CHỈ hợp lệ khi thuộc vai trò GIÁM KHẢO (Judge). Mentor/EC... không được
            //     chấm điểm (điểm được gộp vào trung bình khi công bố -> tránh sai kết quả).
            if (eventRole.RoleName != SEAL_Domain.Entity.Enums.EventRoleType.Judge)
            {
                return new BaseException.ForbiddenException("Chỉ Giám khảo (Judge) mới được chấm điểm.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            bool isOwnRole = eventRole.UserId == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                eventRole.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isOwnRole && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không thể tạo phiếu chấm dưới vai trò của người khác.");
            }

            // 1b. Bài nộp phải tồn tại (cần cho mọi kiểm tra bên dưới)
            var submit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(request.Model.SubmitResultId);
            if (submit == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bài nộp '{request.Model.SubmitResultId}' không tồn tại.");
            }

            var scoredTeam = await _unitOfWork.GetRepository<Team>().GetByIdAsync(submit.TeamId);
            if (scoredTeam != null && scoredTeam.Status == SEAL_Domain.Entity.Enums.TeamStatus.Disqualified)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã bị loại nên không thể chấm bài.");
            }

            // 1c. Giám khảo (Judge) gắn Track chỉ được chấm bài nộp thuộc đúng hạng mục được phân công
            if (eventRole.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.Judge
                && !string.IsNullOrEmpty(eventRole.TrackId)
                && submit.TrackId != eventRole.TrackId)
            {
                return new BaseException.ForbiddenException("Giám khảo chỉ được chấm bài nộp thuộc hạng mục được phân công.");
            }

            // 1d. Xung đột lợi ích: người chấm KHÔNG được là thành viên của đội có bài nộp (đồng bộ SaveScore).
            var isMemberOfScoredTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == eventRole.UserId && er.TeamId == submit.TeamId
                   && (er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.TeamLeader
                    || er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.TeamMember),
                cancellationToken);
            if (isMemberOfScoredTeam)
            {
                return new BaseException.ForbiddenException("Không thể chấm bài của đội mà bạn là thành viên (xung đột lợi ích).");
            }

            // 1e. Cửa sổ chấm — đồng bộ SaveScore: sau hạn nộp (Track ?? Round), trong ScoringStart/End,
            //     và chưa có FinalResult. (Trước đây chỉ check Round.EndDate — đường vòng lách luật.)
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(submit.TrackId);
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(submit.RoundId);
            var now = System.DateTime.UtcNow;
            var demoEvent = track != null
                ? await _unitOfWork.GetRepository<Event>().GetByIdAsync(track.EventId)
                : null;
            var isDemoLive = DemoEventRules.IsLiveSubmitScoreEvent(demoEvent?.EventName);

            var effectiveEndDate = track?.EndDate ?? round?.EndDate;
            if (!isDemoLive && effectiveEndDate.HasValue && now <= effectiveEndDate.Value)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Hạng mục chưa kết thúc nộp bài nên chưa thể chấm (đội vẫn còn quyền nộp/sửa bài).");
            }

            var effectiveScoringStartDate = track?.ScoringStartDate ?? round?.ScoringStartDate;
            var effectiveScoringEndDate = track?.ScoringEndDate ?? round?.ScoringEndDate;
            if (effectiveScoringStartDate.HasValue && now < effectiveScoringStartDate.Value)
            {
                return BaseException.BadRequestInvaildInputResponse("Chưa tới thời gian chấm điểm của hạng mục này.");
            }
            if (effectiveScoringEndDate.HasValue && now > effectiveScoringEndDate.Value)
            {
                return new BaseException.ForbiddenException("Đã hết hạn chấm điểm của hạng mục này.");
            }

            var roundPublished = isDemoLive
                ? await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == submit.RoundId && fr.IsPublished, cancellationToken)
                : await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == submit.RoundId, cancellationToken);
            if (roundPublished)
            {
                return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể tạo phiếu chấm.");
            }

            // 2. Một giám khảo không chấm trùng cùng một bài nộp
            var isDuplicate = await _unitOfWork.GetRepository<Score>().AnyAsync(
                s => s.EventRoleId == request.Model.EventRoleId && s.SubmitResultId == request.Model.SubmitResultId,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Giám khảo đã chấm phiếu cho bài nộp '{request.Model.SubmitResultId}'.");
            }

            // 3. Tạo Score mới (TotalScore = 0, sẽ tự tính lại khi có ScoreDetail)
            var score = new Score
            {
                EventRoleId = request.Model.EventRoleId,
                SubmitResultId = request.Model.SubmitResultId,
                TotalScore = 0,
                Comment = request.Model.Comment
            };

            await _unitOfWork.GetRepository<Score>().AddAsync(score);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateScoreResponseModel
            {
                Id = score.Id,
                EventRoleId = score.EventRoleId,
                SubmitResultId = score.SubmitResultId,
                TotalScore = score.TotalScore,
                Comment = score.Comment,
                CreatedTime = score.CreatedTime
            };
        }
    }
}


