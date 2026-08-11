using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.SetRoundResultsPublishStatus
{
    /// <summary>
    /// ĐẶT TRẠNG THÁI CÔNG BỐ kết quả vòng thi (chuyển được cả hai chiều nháp ⇄ công bố).
    /// Luồng: Calculate (tạo nháp) -> publish-status:true (công bố) -> publish-status:false (thu hồi) -> ...
    /// Chỉ EventCoordinator (hoặc Admin) — đối xứng với Calculate.
    /// </summary>
    public class SetRoundResultsPublishStatusCommandHandler : IRequestHandler<SetRoundResultsPublishStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public SetRoundResultsPublishStatusCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(SetRoundResultsPublishStatusCommand request, CancellationToken cancellationToken)
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

            // 2. Quyền: chỉ EventCoordinator (hoặc Admin)
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, round.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException(
                    "Chỉ EventCoordinator được thay đổi trạng thái công bố kết quả vòng thi.");
            }

            // 3. Vòng phải ĐÃ được tính kết quả (có FinalResult) mới đổi trạng thái được.
            var results = await _unitOfWork.GetRepository<FinalResult>().Entities
                .Where(fr => fr.RoundId == request.RoundId)
                .ToListAsync(cancellationToken);
            if (results.Count == 0)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vòng thi chưa có kết quả được tính (hãy tính kết quả trước khi công bố).");
            }

            // 4. Đặt trạng thái cho toàn bộ kết quả của vòng.
            //    Idempotent: gọi lại cùng trạng thái vẫn trả true, không lỗi (tránh làm khó FE khi
            //    người dùng bấm trùng hoặc 2 tab cùng thao tác).
            //    KHÔNG kèm guard "vòng sau chưa vận hành" như lệnh XÓA (DELETE round/{id}): lệnh này
            //    không hủy dữ liệu — Rank/IsAdvanced vẫn nguyên nên vòng sau vẫn tra được đội đi tiếp,
            //    và đảo ngược được ngay; chặn thêm sẽ khiến EC không sửa được sai sót.
            foreach (var fr in results)
            {
                fr.IsPublished = request.IsPublished;
            }
            await _unitOfWork.GetRepository<FinalResult>().UpdateRangeAsync(results);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


