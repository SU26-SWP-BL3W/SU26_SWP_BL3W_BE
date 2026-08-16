using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateMentorFeedback
{
    public class CreateMentorFeedbackCommandHandler : IRequestHandler<CreateMentorFeedbackCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly INotificationService _notificationService;

        public CreateMentorFeedbackCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _notificationService = notificationService;
        }

        public async Task<Result<string>> Handle(CreateMentorFeedbackCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var submit = await _unitOfWork.GetRepository<SubmitResult>().Entities
                .Include(s => s.Team)
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.Id == request.SubmitResultId, cancellationToken);

            if (submit == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Bài nộp có ID '{request.SubmitResultId}' không tồn tại.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, submit.Round!.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);

            // Tìm vai trò Mentor hợp lệ của user trong Event / Track này
            var nowUtc = DateTime.UtcNow;
            var mentorRole = await _unitOfWork.GetRepository<EventRole>().Entities
                .FirstOrDefaultAsync(er => er.UserId == currentUserId
                                       && er.EventId == submit.Round.EventId
                                       && er.RoleName == EventRoleType.Mentor
                                       && (er.TrackId == null || er.TrackId == submit.TrackId)
                                       && (er.ExpiredAt == null || er.ExpiredAt > nowUtc),
                                     cancellationToken);

            if (!isAdmin && !isCoordinator && mentorRole == null)
            {
                return new BaseException.ForbiddenException("Chỉ Cố vấn (Mentor) phụ trách hạng mục này hoặc BTC mới có quyền gửi nhận xét.");
            }

            // Nếu là Admin/EC không có Mentor role, gán EventRoleId của chính họ hoặc tạo EventRole
            var roleId = mentorRole?.Id;
            if (string.IsNullOrEmpty(roleId))
            {
                var ecRole = await _unitOfWork.GetRepository<EventRole>().Entities
                    .FirstOrDefaultAsync(er => er.UserId == currentUserId && er.EventId == submit.Round.EventId, cancellationToken);
                roleId = ecRole?.Id;
            }

            if (string.IsNullOrEmpty(roleId))
            {
                return BaseException.BadRequestResponse("Không tìm thấy vai trò sự kiện tương ứng của người dùng.");
            }

            var feedback = new MentorFeedback
            {
                SubmitResultId = submit.Id,
                EventRoleId = roleId,
                FeedbackText = request.Model.FeedbackText.Trim()
            };

            await _unitOfWork.GetRepository<MentorFeedback>().AddAsync(feedback);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Bắn thông báo In-App cho toàn bộ thành viên của đội thi
            var memberUserIds = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == submit.TeamId)
                .Select(er => er.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (memberUserIds.Count > 0)
            {
                var mentorName = currentUser?.FullName ?? "Cố vấn";
                await _notificationService.NotifyManyAsync(
                    memberUserIds,
                    "Nhận xét mới từ Cố vấn chuyên môn",
                    $"Cố vấn '{mentorName}' đã gửi nhận xét cho bài nộp của đội bạn: \"{(feedback.FeedbackText.Length > 80 ? feedback.FeedbackText.Substring(0, 80) + "..." : feedback.FeedbackText)}\"",
                    "info",
                    $"/my-team",
                    cancellationToken);
            }

            return feedback.Id;
        }
    }
}
