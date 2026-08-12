using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Appeals.Commands.CreateAppeal
{
    public class CreateAppealCommandHandler : IRequestHandler<CreateAppealCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateAppealCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(CreateAppealCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Bạn cần đăng nhập để gửi đơn phúc khảo.");
            }

            var submitResult = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .Include(sr => sr.Track)
                    .ThenInclude(t => t.Round)
                .FirstOrDefaultAsync(sr => sr.Id == request.SubmitResultId, cancellationToken);

            if (submitResult == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy bài nộp.");
            }

            // 1. Kiểm tra người gửi có phải là TeamLeader của bài nộp này không
            var isTeamLeader = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == userId && er.TeamId == submitResult.TeamId && er.RoleName == EventRoleType.TeamLeader,
                cancellationToken);

            if (!isTeamLeader)
            {
                return new BaseException.ForbiddenException("Chỉ trưởng nhóm (Team Leader) mới được gửi đơn phúc khảo.");
            }

            // 2. Kiểm tra thời gian phúc khảo
            var round = submitResult.Track!.Round;
            var now = CoreHelper.SystemTimeNow.UtcDateTime;

            if (now < round.StartDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi chưa bắt đầu, không thể gửi đơn phúc khảo.");
            }
            if (now > round.EndDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Đã hết thời gian vòng thi, không thể gửi đơn phúc khảo.");
            }

            // 3. Kiểm tra xem vòng thi đã chốt (IsPublished == true) chưa. Nếu đã chốt thì cấm phúc khảo.
            var finalResult = await _unitOfWork.GetRepository<FinalResult>().Entities
                .FirstOrDefaultAsync(fr => fr.RoundId == round.Id && fr.TeamId == submitResult.TeamId, cancellationToken);

            if (finalResult != null && finalResult.IsPublished)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi đã công bố kết quả, không thể gửi đơn phúc khảo.");
            }

            // 4. Đảm bảo chưa có đơn phúc khảo nào cho bài nộp này đang Pending
            var existingPendingAppeal = await _unitOfWork.GetRepository<Appeal>().AnyAsync(
                a => a.SubmitResultId == request.SubmitResultId && a.Status == AppealStatus.Pending,
                cancellationToken);

            if (existingPendingAppeal)
            {
                return BaseException.BadRequestDupplicationResponse("Bạn đang có một đơn phúc khảo chờ xử lý cho bài nộp này.");
            }

            // 5. Tạo đơn phúc khảo
            var appeal = new Appeal
            {
                TeamId = submitResult.TeamId,
                SubmitResultId = request.SubmitResultId,
                Reason = request.Reason,
                Status = AppealStatus.Pending,
                CreatedBy = userId
            };

            await _unitOfWork.GetRepository<Appeal>().AddAsync(appeal);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


