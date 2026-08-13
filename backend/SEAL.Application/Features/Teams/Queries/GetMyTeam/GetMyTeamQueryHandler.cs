// [FLOW3-DOITHI][GetMyTeam] Sinh vien xem doi minh dang tham gia trong 1 su kien.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Teams.Queries.GetMyTeam.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Queries.GetMyTeam
{
    public class GetMyTeamQueryHandler : IRequestHandler<GetMyTeamQuery, Result<MyTeamResponseModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMyTeamQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<MyTeamResponseModel?>> Handle(GetMyTeamQuery request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra xác thực người dùng
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            if (string.IsNullOrEmpty(request.EventId))
            {
                return BaseException.BadRequestResponse("ID sự kiện (EventId) là bắt buộc.");
            }

            // 2. Tìm EventRole của người dùng hiện tại thuộc đội trong sự kiện này
            var userRole = await _unitOfWork.GetRepository<EventRole>().Entities
                .FirstOrDefaultAsync(er => er.UserId == currentUserId
                                       && er.EventId == request.EventId
                                       && er.TeamId != null
                                       && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember),
                                     cancellationToken);

            if (userRole == null)
            {
                return null; // Người dùng chưa tham gia đội nào trong sự kiện này
            }

            // 3. Load chi tiết thông tin đội kèm các thành viên và thông tin người dùng của họ
            var team = await _unitOfWork.GetRepository<Team>().Entities
                .Include(t => t.Event)
                .Include(t => t.EventRoles)
                    .ThenInclude(er => er.User)
                .FirstOrDefaultAsync(t => t.Id == userRole.TeamId, cancellationToken);

            if (team == null)
            {
                return null;
            }

            // 4. Map dữ liệu sang Model trả về
            var response = new MyTeamResponseModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                Status = team.Status.ToString(),
                IsActive = team.IsActive,
                EventId = team.EventId,
                EventName = team.Event?.EventName ?? string.Empty,
                CreatedTime = team.CreatedTime,
                LastRejectReason = team.LastRejectReason,
                Members = team.EventRoles
                    .Where(er => er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember)
                    .Select(er => new TeamMemberModel
                    {
                        UserId = er.UserId,
                        FullName = er.User?.FullName ?? string.Empty,
                        Email = er.User?.Email ?? string.Empty,
                        StudentCode = er.User?.StudentCode,
                        RoleName = er.RoleName.ToString(),
                        IsApproved = er.User?.IsApproved ?? false
                    })
                    .ToList()
            };

            return response;
        }
    }
}


