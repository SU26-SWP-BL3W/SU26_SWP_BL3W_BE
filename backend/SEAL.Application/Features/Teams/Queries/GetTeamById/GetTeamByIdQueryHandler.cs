using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Teams.Queries.GetTeamById.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Queries.GetTeamById
{
    public class GetTeamByIdQueryHandler : IRequestHandler<GetTeamByIdQuery, Result<TeamDetailResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public GetTeamByIdQueryHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<TeamDetailResponseModel>> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
        {
            // Tìm kiếm kèm theo EventRoles và User của thành viên
            var team = await _unitOfWork.GetRepository<Team>().Entities
                .Include(t => t.EventRoles)
                    .ThenInclude(er => er.User)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (team == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Nhóm không tồn tại.");
            }

            // BẢO VỆ PII: chỉ Admin / thành viên chính đội / EC của sự kiện thấy Email + MSSV;
            // người ngoài (kể cả đã đăng nhập) chỉ thấy tên đội + tên thành viên.
            bool canSeeSensitive = false;
            var currentUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
                bool isAdmin = currentUser != null && currentUser.IsAdmin;
                bool isMemberOfTeam = team.EventRoles.Any(er => er.UserId == currentUserId
                    && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember));
                bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                    currentUserId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
                canSeeSensitive = isAdmin || isMemberOfTeam || isCoordinator;
            }

            return new TeamDetailResponseModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description ?? string.Empty,
                Status = team.Status.ToString(),
                IsActive = team.IsActive,
                EventId = team.EventId,
                CreatedTime = team.CreatedTime,
                LastRejectReason = team.LastRejectReason,
                Members = team.EventRoles
                    .Where(er => er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember)
                    .Select(er => new TeamMemberDetailModel
                    {
                        UserId = er.UserId,
                        FullName = er.User?.FullName ?? string.Empty,
                        Email = canSeeSensitive ? (er.User?.Email ?? string.Empty) : string.Empty,
                        StudentCode = canSeeSensitive ? er.User?.StudentCode : null,
                        RoleName = er.RoleName.ToString(),
                        IsApproved = er.User?.IsApproved ?? false
                    })
                    .ToList()
            };
        }
    }
}

