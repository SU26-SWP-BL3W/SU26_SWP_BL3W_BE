using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SEAL_Application.Commons;
using SEAL_Application.Features.EventRoles.Commands.InviteEventRole.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Commands.InviteEventRole
{
    public class InviteEventRoleCommandHandler : IRequestHandler<InviteEventRoleCommand, Result<InviteEventRoleResponseModel>>
    {
        // Thời hạn lời mời (giờ) — chỉnh ở đây nếu muốn đổi
        private const int INVITATION_EXPIRY_HOURS = 24;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public InviteEventRoleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<Result<InviteEventRoleResponseModel>> Handle(InviteEventRoleCommand request, CancellationToken cancellationToken)
        {
            var model = request.Model;

            // 0. CurrentUser bắt buộc (người gửi mời). Quyền Admin/EC đã được EventRoleAuthorize kiểm tra ở Controller.
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Sự kiện tồn tại (lấy luôn entity để dùng EventName cho email)
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(model.EventId);
            if (eventEntity == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{model.EventId}' không tồn tại.");
            }

            // 2. Người được mời tồn tại
            var invitedUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(model.InvitedUserId);
            if (invitedUser == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng có ID '{model.InvitedUserId}' không tồn tại.");
            }

            // 3. Track (nếu có) phải thuộc sự kiện — giữ lại tên hạng mục để hiển thị trong email
            string? trackName = null;
            if (!string.IsNullOrEmpty(model.TrackId))
            {
                var track = await _unitOfWork.GetRepository<Track>()
                    .GetByIdAsync(model.TrackId);
                if (track == null)
                {
                    return BaseException.BadRequestNotFoundResponse($"Hạng mục (Track) có ID '{model.TrackId}' không tồn tại.");
                }
                if (track.EventId != model.EventId)
                {
                    return BaseException.BadRequestResponse($"Hạng mục có ID '{model.TrackId}' không thuộc sự kiện này.");
                }
                trackName = track.TrackName;
            }

            // 4. Người được mời chưa giữ vai trò này trong sự kiện
            var alreadyHasRole = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == model.InvitedUserId
                   && er.EventId == model.EventId
                   && er.RoleName == model.RoleName,
                cancellationToken);
            if (alreadyHasRole)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng đã giữ vai trò này trong sự kiện.");
            }

            // 5. Chưa có lời mời cùng vai trò đang chờ phản hồi (còn hiệu lực)
            var now = DateTime.UtcNow;
            var hasPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                inv => inv.InvitedUserId == model.InvitedUserId
                    && inv.EventId == model.EventId
                    && inv.RoleName == model.RoleName
                    && inv.Status == EventRoleInvitationStatus.Pending
                    && inv.ExpiresAt > now,
                cancellationToken);
            if (hasPendingInvite)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng đang có một lời mời cùng vai trò chờ phản hồi trong sự kiện này.");
            }

            // 5b. Chặn NGAY TỪ LÚC MỜI: đang có lời mời vai trò XUNG KHẮC chờ phản hồi trong cùng sự kiện
            //     (EC/Giám khảo/Mentor không kiêm nhiệm lẫn nhau) -> không gửi email mâu thuẫn.
            bool hasConflictPendingInvite;
            if (model.RoleName == EventRoleType.EventCoordinator)
            {
                hasConflictPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                    inv => inv.InvitedUserId == model.InvitedUserId && inv.EventId == model.EventId
                        && (inv.RoleName == EventRoleType.Judge || inv.RoleName == EventRoleType.Mentor)
                        && inv.Status == EventRoleInvitationStatus.Pending && inv.ExpiresAt > now,
                    cancellationToken);
            }
            else if (model.RoleName == EventRoleType.Judge)
            {
                hasConflictPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                    inv => inv.InvitedUserId == model.InvitedUserId && inv.EventId == model.EventId
                        && (inv.RoleName == EventRoleType.EventCoordinator || inv.RoleName == EventRoleType.Mentor)
                        && inv.Status == EventRoleInvitationStatus.Pending && inv.ExpiresAt > now,
                    cancellationToken);
            }
            else if (model.RoleName == EventRoleType.Mentor)
            {
                hasConflictPendingInvite = await _unitOfWork.GetRepository<EventRoleInvitation>().AnyAsync(
                    inv => inv.InvitedUserId == model.InvitedUserId && inv.EventId == model.EventId
                        && (inv.RoleName == EventRoleType.EventCoordinator || inv.RoleName == EventRoleType.Judge)
                        && inv.Status == EventRoleInvitationStatus.Pending && inv.ExpiresAt > now,
                    cancellationToken);
            }
            else
            {
                hasConflictPendingInvite = false;
            }
            if (hasConflictPendingInvite)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Người này đang có lời mời vai trò xung khắc chờ phản hồi trong sự kiện — Event Coordinator, Giám khảo và Cố vấn không được kiêm nhiệm lẫn nhau.");
            }

            // 6. Tạo lời mời PENDING, hết hạn sau INVITATION_EXPIRY_HOURS giờ
            var invitation = new EventRoleInvitation
            {
                EventId = model.EventId,
                RoleName = model.RoleName,
                TrackId = string.IsNullOrEmpty(model.TrackId) ? null : model.TrackId,
                InvitedUserId = model.InvitedUserId,
                InvitedByUserId = currentUserId,
                Status = EventRoleInvitationStatus.Pending,
                ExpiresAt = now.AddHours(INVITATION_EXPIRY_HOURS),
                Notes = model.Notes
            };

            await _unitOfWork.GetRepository<EventRoleInvitation>().AddAsync(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Gửi email thông báo cho người được mời (best-effort: không chặn luồng nếu gửi mail lỗi,
            //    vì lời mời đã được lưu và vẫn hiển thị ở chuông thông báo /my-invitations).
            try
            {
                var frontendUrl = (_configuration["FrontendUrl"] ?? "https://sealswp391.xyz").TrimEnd('/');
                var acceptLink = $"{frontendUrl}/invitations/{invitation.Id}?action=accept";
                var declineLink = $"{frontendUrl}/invitations/{invitation.Id}?action=decline";
                var roleLabel = invitation.RoleName.ToString();

                var calloutHtml = $"Sự kiện: <b>{eventEntity.EventName}</b>";
                if (!string.IsNullOrEmpty(trackName))
                    calloutHtml += $"<br>Hạng mục: <b>{trackName}</b>";
                calloutHtml += $"<br>Vai trò: <b>{roleLabel}</b>";

                var introTrack = string.IsNullOrEmpty(trackName) ? "" : $" (hạng mục <b>{trackName}</b>)";
                var emailBody = EmailTemplate.Render(
                    heading: "Lời mời vai trò sự kiện",
                    greetingName: invitedUser.FullName,
                    introHtml: $"Bạn được mời đảm nhận vai trò <b>{roleLabel}</b>{introTrack} trong sự kiện <b>{eventEntity.EventName}</b>.",
                    calloutLabel: "Thông tin lời mời",
                    calloutHtml: calloutHtml,
                    calloutKind: EmailTemplate.Callout.Success,
                    ctaText: "Đồng ý",
                    ctaUrl: acceptLink,
                    ctaText2: "Từ chối",
                    ctaUrl2: declineLink,
                    ctaFallbackUrl: $"{frontendUrl}/invitations/{invitation.Id}",
                    noteHtml: $"Lời mời sẽ hết hạn sau {INVITATION_EXPIRY_HOURS} giờ. Nếu bạn không chấp nhận, vai trò sẽ không được tạo.");

                await _emailService.SendEmailAsync(
                    invitedUser.Email,
                    $"[SEAL] Lời mời làm {roleLabel} tại {eventEntity.EventName}",
                    emailBody);
            }
            catch
            {
                // Bỏ qua lỗi gửi email — không ảnh hưởng tới việc tạo lời mời.
            }

            return new InviteEventRoleResponseModel
            {
                InvitationId = invitation.Id,
                EventId = invitation.EventId,
                InvitedUserId = invitation.InvitedUserId,
                RoleName = invitation.RoleName.ToString(),
                Status = invitation.Status.ToString(),
                ExpiresAt = invitation.ExpiresAt
            };
        }
    }
}


