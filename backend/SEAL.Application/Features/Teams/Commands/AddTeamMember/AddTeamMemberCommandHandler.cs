// [FLOW3-DOITHI][AddTeamMember] Them truc tiep 1 sinh vien vao doi (khong qua loi moi), dung cho cac truong hop dac biet.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Teams.Commands.AddTeamMember.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Commands.AddTeamMember
{
    public class AddTeamMemberCommandHandler : IRequestHandler<AddTeamMemberCommand, Result<AddTeamMemberResponseModel>>
    {
        // Theo functional requirement: đội thi 3-5 thành viên (tính cả Leader)
        private const int MAX_TEAM_SIZE = 5;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public AddTeamMemberCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<AddTeamMemberResponseModel>> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
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

            // 2. Người dùng được thêm tồn tại
            var newMember = await _unitOfWork.GetRepository<User>().GetByIdAsync(request.Model.UserId);
            if (newMember == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng có ID '{request.Model.UserId}' không tồn tại.");
            }

            // 3. Kiểm tra quyền: caller phải là TeamLeader của team này, hoặc EventCoordinator, hoặc Admin
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isLeaderOfThisTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.TeamId == request.TeamId
                   && er.RoleName == EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                team.EventId,
                new[] { EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isAdmin && !isLeaderOfThisTeam && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền thêm thành viên vào nhóm này. Chỉ TeamLeader hoặc EventCoordinator được phép.");
            }

            // 3b. Đội đã đăng ký chính thức (Registered) thì bị KHÓA — không thêm thành viên được.
            //     (Nhất quán với RespondTeamInvitation và LeaveTeam.)
            if (team.Status != TeamStatus.Forming)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã đăng ký chính thức (đã khóa) nên không thể thêm thành viên.");
            }

            // 3c. Người được thêm phải là tài khoản đã duyệt (đồng bộ InviteTeamMember/Respond).
            if (!newMember.IsApproved)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng này chưa được ban tổ chức duyệt nên không thể thêm vào đội.");
            }

            // 3d. Còn trong hạn đăng ký của sự kiện (Invite có check, đường thêm trực tiếp trước đây thì không).
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(team.EventId);
            if (eventEntity != null)
            {
                var nowUtc = DateTime.UtcNow;
                if (eventEntity.RegistrationStartDate.HasValue && nowUtc < eventEntity.RegistrationStartDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse("Chưa đến thời gian đăng ký thi, không thể thêm thành viên.");
                }
                if (eventEntity.RegistrationEndDate.HasValue && nowUtc > eventEntity.RegistrationEndDate.Value)
                {
                    return BaseException.BadRequestInvaildInputResponse("Đã hết thời gian đăng ký thi, không thể thêm thành viên.");
                }
            }

            // 3e. Người giữ vai trò tổ chức (EC/Giám khảo/Cố vấn) trong sự kiện KHÔNG được vào đội thi
            //     (Invite đã chặn — đường thêm trực tiếp là cửa còn hở).
            var hasOrganizerRole = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == request.Model.UserId
                   && er.EventId == team.EventId
                   && (er.RoleName == EventRoleType.EventCoordinator
                    || er.RoleName == EventRoleType.Judge
                    || er.RoleName == EventRoleType.Mentor),
                cancellationToken);
            if (hasOrganizerRole)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng đang giữ vai trò Ban tổ chức/Giám khảo/Cố vấn trong sự kiện này nên không thể vào đội thi.");
            }

            // 4. Quy tắc 1 user / 1 team / 1 event: user mới chưa thuộc team nào trong Event này
            var alreadyInTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == request.Model.UserId
                   && er.EventId == team.EventId
                   && er.TeamId != null
                   && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember),
                cancellationToken);
            if (alreadyInTeam)
            {
                return BaseException.BadRequestInvaildInputResponse("Người dùng này đã tham gia một nhóm khác trong sự kiện. Yêu cầu họ rời nhóm cũ trước.");
            }

            // 5. Team chưa đầy (tổng Leader + Member < MAX_TEAM_SIZE)
            var currentMemberCount = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == request.TeamId
                          && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember))
                .CountAsync(cancellationToken);
            if (currentMemberCount >= MAX_TEAM_SIZE)
            {
                return BaseException.BadRequestInvaildInputResponse($"Nhóm đã đủ tối đa {MAX_TEAM_SIZE} thành viên.");
            }

            // 6. Thêm EventRole = TeamMember
            var memberRole = new EventRole
            {
                UserId = request.Model.UserId,
                EventId = team.EventId,
                TeamId = team.Id,
                RoleName = EventRoleType.TeamMember,
                AssignedAt = DateTime.UtcNow,
                ExpiredAt = eventEntity?.EndDate,
                Notes = request.Model.Notes
            };

            await _unitOfWork.GetRepository<EventRole>().AddAsync(memberRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AddTeamMemberResponseModel
            {
                EventRoleId = memberRole.Id,
                TeamId = memberRole.TeamId,
                UserId = memberRole.UserId,
                RoleName = memberRole.RoleName.ToString(),
                AssignedAt = memberRole.AssignedAt
            };
        }
    }
}


