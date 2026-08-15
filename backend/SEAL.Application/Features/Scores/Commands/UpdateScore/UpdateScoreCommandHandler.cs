using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Scores.Commands.UpdateScore.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Commands.UpdateScore
{
    public class UpdateScoreCommandHandler : IRequestHandler<UpdateScoreCommand, Result<UpdateScoreResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public UpdateScoreCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<UpdateScoreResponseModel>> Handle(UpdateScoreCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Score cần cập nhật
            var score = await _unitOfWork.GetRepository<Score>().GetByIdAsync(request.Id);
            if (score == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Phiếu chấm có ID '{request.Id}' không tồn tại.");
            }

            // 1b. KHÔNG cho đổi bài nộp (SubmitResultId) của phiếu chấm đã tồn tại: đổi sang bài khác sẽ
            //     giữ nguyên TotalScore/ScoreDetail đã tính cho bài CŨ (không tính lại theo template bài MỚI),
            //     đồng thời làm khóa "sau công bố" bên dưới (dựa trên bài đang gắn) kiểm nhầm vòng đích.
            //     Muốn chuyển giám khảo sang chấm bài khác: dùng POST /Scores/save (upsert theo EventRoleId+SubmitResultId).
            if (score.SubmitResultId != request.Model.SubmitResultId)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Không thể đổi bài nộp của phiếu chấm đã tồn tại. Vui lòng dùng chức năng lưu điểm (Save) để chấm bài nộp khác.");
            }

            // Lấy EventRole cũ
            var oldEventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(score.EventRoleId);
            if (oldEventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse("Vai trò chấm điểm của phiếu chấm cũ không hợp lệ.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            bool isOwnOldRole = oldEventRole.UserId == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                oldEventRole.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isOwnOldRole && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền sửa phiếu chấm này.");
            }

            // 2. Kiểm tra EventRole mới có tồn tại và thuộc sở hữu của User (nếu đổi EventRoleId)
            var newEventRole = await _unitOfWork.GetRepository<EventRole>()
                .GetByIdAsync(request.Model.EventRoleId);
            if (newEventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vai trò chấm điểm có ID '{request.Model.EventRoleId}' không tồn tại.");
            }

            if (newEventRole.RoleName != SEAL_Domain.Entity.Enums.EventRoleType.Judge)
            {
                return new BaseException.ForbiddenException("Chỉ Giám khảo (Judge) mới được chấm điểm.");
            }

            if (score.EventRoleId != request.Model.EventRoleId)
            {
                bool isOwnNewRole = newEventRole.UserId == currentUserId;
                if (!isOwnNewRole && !isCoordinator)
                {
                    return new BaseException.ForbiddenException("Bạn không thể thay đổi vai trò chấm điểm thành tài khoản của người khác.");
                }
            }

            // 3. Kiểm tra trùng (loại trừ chính nó)
            var isDuplicate = await _unitOfWork.GetRepository<Score>().AnyAsync(
                s => s.EventRoleId == request.Model.EventRoleId
                  && s.SubmitResultId == request.Model.SubmitResultId
                  && s.Id != request.Id,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Giám khảo đã chấm phiếu cho bài nộp '{request.Model.SubmitResultId}'.");
            }

            // Khóa sửa khi kết quả vòng đã được tính/công bố (tránh lệch kết quả đã công bố).
            var submit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(score.SubmitResultId);
            if (submit != null)
            {
                var published = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == submit.RoundId, cancellationToken);
                if (published)
                    return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể sửa phiếu chấm.");
            }

            // 4. Cập nhật
            score.EventRoleId = request.Model.EventRoleId;
            score.SubmitResultId = request.Model.SubmitResultId;
            score.Comment = request.Model.Comment;
            score.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // Lưu ý: TotalScore được tính tự động qua ScoreDetail, không cho sửa ở đây
            await _unitOfWork.GetRepository<Score>().UpdateAsync(score);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateScoreResponseModel
            {
                Id = score.Id,
                EventRoleId = score.EventRoleId,
                SubmitResultId = score.SubmitResultId,
                TotalScore = score.TotalScore,
                Comment = score.Comment,
                LastUpdatedTime = score.LastUpdatedTime
            };
        }
    }
}


