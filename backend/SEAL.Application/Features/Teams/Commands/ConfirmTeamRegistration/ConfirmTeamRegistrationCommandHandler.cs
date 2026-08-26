// [FLOW3-DOITHI][ConfirmTeamRegistration] Chot danh sach doi (yeu cau 3-5 thanh vien) de gui EC duyet, khoa danh sach thanh vien.

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Application.Commons;
using SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration.Models;
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

namespace SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration
{
    public class ConfirmTeamRegistrationCommandHandler : IRequestHandler<ConfirmTeamRegistrationCommand, Result<ConfirmTeamRegistrationResponseModel>>
    {
        // Đội phải có 3-5 thành viên để đăng ký chính thức
        private const int MIN_TEAM_SIZE = 3;
        private const int MAX_TEAM_SIZE = 5;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly INotificationService _notifications;
        private readonly IEmailService _emailService;
        private readonly ILogger<ConfirmTeamRegistrationCommandHandler> _logger;

        public ConfirmTeamRegistrationCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker,
            INotificationService notifications,
            IEmailService emailService,
            ILogger<ConfirmTeamRegistrationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _notifications = notifications;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result<ConfirmTeamRegistrationResponseModel>> Handle(ConfirmTeamRegistrationCommand request, CancellationToken cancellationToken)
        {
            // 0. CurrentUser bắt buộc
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Team tồn tại
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Nhóm có ID '{request.TeamId}' không tồn tại.");
            }

            // 2. Đội phải đang Forming (chưa đăng ký)
            if (team.Status != TeamStatus.Forming)
            {
                return BaseException.BadRequestInvaildInputResponse($"Đội không ở trạng thái cho phép đăng ký (hiện tại: {team.Status}).");
            }

            if (string.IsNullOrEmpty(team.TrackId))
            {
                return BaseException.BadRequestInvaildInputResponse("Đội chưa đăng ký hạng mục thi nên không thể chốt danh sách.");
            }

            // 3. Quyền: caller là TeamLeader của đội (hoặc Coordinator/Admin)
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isLeaderOfThisTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.TeamId == request.TeamId
                   && er.RoleName == EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);

            if (!isAdmin && !isLeaderOfThisTeam && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ TeamLeader của đội hoặc EventCoordinator được xác nhận đăng ký.");
            }

            // 4. Final validation — lấy danh sách thành viên của đội
            var memberRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == request.TeamId
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .ToListAsync(cancellationToken);

            var memberCount = memberRoles.Count;

            // 4a. Đủ 3-5 thành viên
            if (memberCount < MIN_TEAM_SIZE || memberCount > MAX_TEAM_SIZE)
            {
                return BaseException.BadRequestInvaildInputResponse($"Đội cần {MIN_TEAM_SIZE}-{MAX_TEAM_SIZE} thành viên để đăng ký (hiện tại: {memberCount}).");
            }

            // 4b. (ĐÃ BỎ) Trước đây yêu cầu mọi thành viên phải được duyệt TÀI KHOẢN (IsApproved).
            //     Nay việc xét duyệt chuyển sang cấp ĐỘI THI: đội chốt danh sách -> PendingApproval
            //     -> EC/Admin duyệt cả đội (ApproveTeamRegistration). Không chặn theo IsApproved nữa.
            var memberUserIds = memberRoles.Select(er => er.UserId).ToList();

            // 4b'. Tất cả thành viên đã NỘP HỒ SƠ THÍ SINH — bất biến "đã nộp hồ sơ" của hệ thống:
            //      IsStudent (chỉ UpdateStudentProfile set) + SchoolId (luôn bắt buộc khi nộp hồ sơ).
            //      Hồ sơ được phép bổ sung trong lúc Forming; CHỐT đăng ký thì phải đủ.
            var profileCount = await _unitOfWork.GetRepository<User>().Entities
                .CountAsync(u => memberUserIds.Contains(u.Id) && u.IsStudent && u.SchoolId != null, cancellationToken);
            if (profileCount != memberCount)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Có thành viên chưa nộp hồ sơ thí sinh. Vui lòng hoàn tất hồ sơ trước khi chốt đăng ký đội.");
            }

            // 4c. Còn trong HẠN ĐĂNG KÝ của sự kiện (nhất quán với CreateTeam/InviteTeamMember).
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(team.EventId);
            if (eventEntity != null)
            {
                var nowUtc = System.DateTime.UtcNow;
                if (eventEntity.RegistrationStartDate.HasValue && nowUtc < eventEntity.RegistrationStartDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse("Chưa đến thời gian đăng ký thi, không thể chốt đăng ký đội.");
                }
                if (eventEntity.RegistrationEndDate.HasValue && nowUtc > eventEntity.RegistrationEndDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse("Đã hết thời gian đăng ký thi, không thể chốt đăng ký đội.");
                }
            }

            // 5. Khóa danh sách đội — chuyển sang CHỜ EC/Admin DUYỆT (không còn tự động Registered).
            //    Đội chỉ chính thức được thi sau khi EC duyệt (ApproveTeamRegistration).
            team.Status = TeamStatus.PendingApproval;
            // Dọn lý do từ chối cũ — đội đã sửa và nộp lại, giữ lại sẽ hiện mãi một lý do đã xử lý xong.
            team.LastRejectReason = null;
            team.LastUpdatedTime = CoreHelper.SystemTimeNow;
            await _unitOfWork.GetRepository<Team>().UpdateAsync(team);

            await _notifications.NotifyManyAsync(
                memberUserIds,
                "Đội đã chốt đăng ký",
                $"Đội {team.Name} đã gửi đăng ký, chờ Ban tổ chức duyệt.",
                "info",
                "/my-team",
                cancellationToken);

            var coordinatorIds = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.EventId == team.EventId && er.RoleName == EventRoleType.EventCoordinator)
                .Select(er => er.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);
            await _notifications.NotifyManyAsync(
                coordinatorIds,
                "Đội chờ duyệt",
                $"Đội {team.Name} vừa chốt danh sách, cần duyệt đăng ký.",
                "warning",
                "/coordinator/teams",
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. Gửi email thông báo (lỗi SMTP không chặn nghiệp vụ)
            var eventName = eventEntity?.EventName ?? "sự kiện";

            // 6a. Email cho thành viên đội
            var memberUsers = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == team.Id
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .Select(er => er.User)
                .ToListAsync(cancellationToken);
            foreach (var m in memberUsers)
            {
                if (m == null || string.IsNullOrEmpty(m.Email)) continue;
                var body = EmailTemplate.Render(
                    heading: "Đội đã chốt đăng ký",
                    greetingName: m.FullName,
                    introHtml: $"Đội <b>{team.Name}</b> đã chốt danh sách đăng ký tham gia <b>{eventName}</b>.",
                    calloutLabel: "Chờ Ban tổ chức duyệt",
                    calloutHtml: "Đội của bạn đang chờ Ban tổ chức xét duyệt. Bạn sẽ nhận được thông báo khi có kết quả.",
                    calloutKind: EmailTemplate.Callout.Info,
                    showLoginHint: false);
                try { await _emailService.SendEmailAsync(m.Email, $"[SEAL] Đội {team.Name} đã chốt đăng ký", body); }
                catch (Exception ex) { _logger.LogWarning(ex, "Gửi email chốt đăng ký thất bại cho {Email}", m.Email); }
            }

            // 6b. Email cho EC — báo có đội mới chờ duyệt
            var ecUsers = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.EventId == team.EventId && er.RoleName == EventRoleType.EventCoordinator)
                .Select(er => er.User)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var ec in ecUsers)
            {
                if (ec == null || string.IsNullOrEmpty(ec.Email)) continue;
                var body = EmailTemplate.Render(
                    heading: "Đội chờ duyệt đăng ký",
                    greetingName: ec.FullName,
                    introHtml: $"Đội <b>{team.Name}</b> ({memberCount} thành viên) vừa chốt danh sách đăng ký <b>{eventName}</b>.",
                    calloutLabel: "Cần xét duyệt",
                    calloutHtml: "Vui lòng vào trang Quản lý đội để duyệt hoặc từ chối đội này.",
                    calloutKind: EmailTemplate.Callout.Warning,
                    showLoginHint: true);
                try { await _emailService.SendEmailAsync(ec.Email, $"[SEAL] Đội {team.Name} chờ duyệt", body); }
                catch (Exception ex) { _logger.LogWarning(ex, "Gửi email thông báo EC thất bại cho {Email}", ec.Email); }
            }

            return new ConfirmTeamRegistrationResponseModel
            {
                TeamId = team.Id,
                Status = team.Status.ToString(),
                MemberCount = memberCount
            };
        }
    }
}


