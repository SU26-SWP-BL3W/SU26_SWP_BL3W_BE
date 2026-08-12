using MediatR;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation
{
    public class RespondEventRoleInvitationCommandHandler : IRequestHandler<RespondEventRoleInvitationCommand, Result<RespondEventRoleInvitationResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public RespondEventRoleInvitationCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<RespondEventRoleInvitationResponseModel>> Handle(RespondEventRoleInvitationCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Tìm lời mời
            var invitation = await _unitOfWork.GetRepository<EventRoleInvitation>().GetByIdAsync(request.InvitationId);
            if (invitation == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Lời mời '{request.InvitationId}' không tồn tại.");
            }

            // 2. Chỉ chính người được mời mới được phản hồi
            if (invitation.InvitedUserId != currentUserId)
            {
                return new BaseException.ForbiddenException("Bạn không phải người được mời trong lời mời này.");
            }

            // 3. Lời mời phải đang chờ phản hồi
            if (invitation.Status != EventRoleInvitationStatus.Pending)
            {
                return BaseException.BadRequestInvaildInputResponse($"Lời mời đã được xử lý trước đó (trạng thái: {invitation.Status}).");
            }

            // 4. Lazy expire: nếu quá hạn thì đánh dấu Expired và báo lỗi
            var now = DateTime.UtcNow;
            if (invitation.ExpiresAt <= now)
            {
                invitation.Status = EventRoleInvitationStatus.Expired;
                invitation.RespondedAt = now;
                await _unitOfWork.GetRepository<EventRoleInvitation>().UpdateAsync(invitation);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return BaseException.BadRequestInvaildInputResponse("Lời mời đã hết hạn.");
            }

            // 5. REJECT — từ chối lời mời
            if (!request.IsAccepted)
            {
                invitation.Status = EventRoleInvitationStatus.Rejected;
                invitation.RespondedAt = now;
                await _unitOfWork.GetRepository<EventRoleInvitation>().UpdateAsync(invitation);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new RespondEventRoleInvitationResponseModel
                {
                    InvitationId = invitation.Id,
                    EventId = invitation.EventId,
                    Status = invitation.Status.ToString()
                };
            }

            // 6. ACCEPT — re-validate trước khi tạo EventRole chính thức
            var targetEvent = await _unitOfWork.GetRepository<Event>().GetByIdAsync(invitation.EventId);
            if (targetEvent == null)
            {
                return BaseException.BadRequestNotFoundResponse("Sự kiện của lời mời này không còn tồn tại.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (currentUser == null)
            {
                return BaseException.BadRequestNotFoundResponse("Tài khoản của bạn không tồn tại.");
            }

            // 6a. Xung đột vai trò khi CHẤP NHẬN (đây mới là nơi thật sự tạo EventRole):
            //     Event Coordinator, Giám khảo và Mentor LOẠI TRỪ NHAU trong cùng một sự kiện (tránh mọi nghi ngờ).
            bool crossRoleConflict;
            if (invitation.RoleName == EventRoleType.EventCoordinator)
            {
                crossRoleConflict = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                    er => er.UserId == currentUserId && er.EventId == invitation.EventId
                       && (er.RoleName == EventRoleType.Judge || er.RoleName == EventRoleType.Mentor),
                    cancellationToken);
            }
            else if (invitation.RoleName == EventRoleType.Judge)
            {
                crossRoleConflict = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                    er => er.UserId == currentUserId && er.EventId == invitation.EventId
                       && (er.RoleName == EventRoleType.EventCoordinator || er.RoleName == EventRoleType.Mentor),
                    cancellationToken);
            }
            else if (invitation.RoleName == EventRoleType.Mentor)
            {
                crossRoleConflict = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                    er => er.UserId == currentUserId && er.EventId == invitation.EventId
                       && (er.RoleName == EventRoleType.EventCoordinator || er.RoleName == EventRoleType.Judge),
                    cancellationToken);
            }
            else
            {
                crossRoleConflict = false;
            }
            if (crossRoleConflict)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Không thể nhận vai trò này: Event Coordinator, Giám khảo và Mentor không được kiêm nhiệm lẫn nhau trong cùng một sự kiện.");
            }

            // 6b. Tránh tạo trùng vai trò nếu đã có (idempotent) — so cả TrackId:
            //     cùng vai trò nhưng KHÁC hạng mục vẫn phải tạo EventRole mới (1 người được chấm/hướng dẫn nhiều hạng mục).
            var alreadyHasRole = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.EventId == invitation.EventId
                   && er.RoleName == invitation.RoleName
                   && er.TrackId == invitation.TrackId,
                cancellationToken);

            string? eventRoleId = null;
            if (!alreadyHasRole)
            {
                // 6c. Tạo EventRole chính thức từ thông tin lời mời (EventId, RoleName, TrackId, UserId)
                var newRole = new EventRole
                {
                    UserId = currentUserId,
                    EventId = invitation.EventId,
                    TrackId = invitation.TrackId,
                    RoleName = invitation.RoleName,
                    AssignedAt = now,
                    ExpiredAt = targetEvent.EndDate,
                    Notes = "Được tạo từ lời mời vai trò sự kiện"
                };
                await _unitOfWork.GetRepository<EventRole>().AddAsync(newRole);
                eventRoleId = newRole.Id;
            }

            invitation.Status = EventRoleInvitationStatus.Accepted;
            invitation.RespondedAt = now;
            await _unitOfWork.GetRepository<EventRoleInvitation>().UpdateAsync(invitation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Xóa cache phân quyền để vai trò mới có hiệu lực ngay lập tức
            _eventRoleChecker.InvalidateCache(currentUserId, invitation.EventId);

            return new RespondEventRoleInvitationResponseModel
            {
                InvitationId = invitation.Id,
                EventId = invitation.EventId,
                Status = invitation.Status.ToString(),
                EventRoleId = eventRoleId
            };
        }
    }
}


