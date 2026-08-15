// [FLOW3-DOITHI][CreateTeam] Truong nhom tao doi thi moi trong 1 su kien, doi o trang thai Forming (dang lap).

using MediatR;
using SEAL_Application.Features.Teams.Commands.CreateTeam.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SEAL_Application.Features.Teams.Commands.CreateTeam
{
    public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Result<CreateTeamResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateTeamCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CreateTeamResponseModel>> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
        {
            // 0. Lấy CurrentUserId (bắt buộc phải đăng nhập mới tạo Team được)
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            // 1. Kiểm tra Event có tồn tại và thời gian đăng ký
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.Model.EventId);
            if (eventEntity == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Sự kiện (Event) không tồn tại.");
            }

            var now = DateTime.UtcNow;
            if (eventEntity.RegistrationStartDate.HasValue && now < eventEntity.RegistrationStartDate.Value)
            {
                return BaseException.BadRequestInvaildInputResponse("Chưa đến thời gian đăng ký thi.");
            }
            if (eventEntity.RegistrationEndDate.HasValue && now > eventEntity.RegistrationEndDate.Value)
            {
                return BaseException.BadRequestInvaildInputResponse("Đã hết thời gian đăng ký thi, không thể tạo đội.");
            }

            // 1b. Người tạo phải là tài khoản đã được ban tổ chức duyệt (đồng bộ với RespondTeamInvitation).
            var creator = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (creator == null || !creator.IsApproved)
            {
                return BaseException.BadRequestInvaildInputResponse("Tài khoản của bạn chưa được phê duyệt hoặc đã bị khóa nên không thể tạo đội.");
            }

            // 1c. Người giữ vai trò tổ chức trong sự kiện (EC/Giám khảo/Cố vấn) KHÔNG được tạo đội thi
            //     (InviteTeamMember đã chặn chiều ngược lại; đây là cửa còn hở).
            var hasOrganizerRole = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.EventId == request.Model.EventId
                   && (er.RoleName == EventRoleType.EventCoordinator
                    || er.RoleName == EventRoleType.Judge
                    || er.RoleName == EventRoleType.Mentor),
                cancellationToken);
            if (hasOrganizerRole)
            {
                return BaseException.BadRequestInvaildInputResponse("Bạn đang giữ vai trò Ban tổ chức/Giám khảo/Cố vấn trong sự kiện này nên không thể tạo đội thi.");
            }

            // 2. Một User chỉ được tham gia 1 Team trong 1 Event (TeamLeader hoặc TeamMember)
            var hasOtherTeamInEvent = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId
                   && er.EventId == request.Model.EventId
                   && er.TeamId != null
                   && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember),
                cancellationToken);
            if (hasOtherTeamInEvent)
            {
                return BaseException.BadRequestInvaildInputResponse("Bạn đã tham gia một nhóm khác trong sự kiện này. Hãy rời nhóm cũ trước khi tạo nhóm mới.");
            }

            // 3. Kiểm tra trùng tên nhóm trong cùng sự kiện
            var isNameExist = await _unitOfWork.GetRepository<Team>().AnyAsync(
                t => t.Name.ToLower() == request.Model.Name.ToLower() && t.EventId == request.Model.EventId,
                cancellationToken);
            if (isNameExist)
            {
                return BaseException.BadRequestDupplicationResponse($"Tên nhóm '{request.Model.Name}' đã tồn tại trong sự kiện này.");
            }

            // 3b. Kiểm tra số lượng nhóm đã đạt tối đa chưa (chỉ tính nhóm IsActive)
            var currentTeamCount = await _unitOfWork.GetRepository<Team>().GetQueryable()
                .CountAsync(t => t.EventId == request.Model.EventId && t.IsActive, cancellationToken);
            if (currentTeamCount >= eventEntity.MaxTeams)
            {
                return BaseException.BadRequestInvaildInputResponse($"Sự kiện đã đạt giới hạn số đội ({eventEntity.MaxTeams} đội). Không thể tạo thêm.");
            }

            // 1 đội / 1 hạng mục; Track phải thuộc đúng Event.
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.Model.TrackId);
            if (track == null || track.EventId != request.Model.EventId)
            {
                return BaseException.BadRequestInvaildInputResponse("Hạng mục thi không tồn tại hoặc không thuộc sự kiện này.");
            }

            // 4. Tạo Team mới
            var team = new Team
            {
                EventId = request.Model.EventId,
                TrackId = request.Model.TrackId,
                Name = request.Model.Name,
                Description = request.Model.Description,
                IsActive = true
            };
            await _unitOfWork.GetRepository<Team>().AddAsync(team);

            // 5. Auto-gán EventRole = TeamLeader cho user vừa tạo Team
            var leaderRole = new EventRole
            {
                UserId = currentUserId,
                EventId = request.Model.EventId,
                TeamId = team.Id,
                RoleName = EventRoleType.TeamLeader,
                AssignedAt = DateTime.UtcNow,
                ExpiredAt = eventEntity.EndDate,
                Notes = "Auto-assigned khi tạo Team"
            };
            await _unitOfWork.GetRepository<EventRole>().AddAsync(leaderRole);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateTeamResponseModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description ?? string.Empty,
                TrackId = team.TrackId,
                IsActive = team.IsActive,
                CreatedTime = team.CreatedTime
            };
        }
    }
}


