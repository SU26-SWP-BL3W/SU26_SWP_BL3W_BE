using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Appeals.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Appeals.Queries.GetAppealsByTeam
{
    public class GetAppealsByTeamQueryHandler : IRequestHandler<GetAppealsByTeamQuery, Result<PagedResult<AppealModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetAppealsByTeamQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<PagedResult<AppealModel>>> Handle(GetAppealsByTeamQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Vui lòng đăng nhập.");
            }

            // Chỉ Admin, EC của sự kiện chứa đội này, hoặc chính thành viên đội mới được xem.
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.TeamId);
            if (team == null)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xem phúc khảo của đội này.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = !isAdmin && await _eventRoleChecker.HasRoleAsync(
                userId, team.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            bool isTeamMember = !isAdmin && !isCoordinator && await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == userId && er.TeamId == request.TeamId
                   && (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember),
                cancellationToken);

            if (!isAdmin && !isCoordinator && !isTeamMember)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xem phúc khảo của đội này.");
            }

            var query = _unitOfWork.GetRepository<Appeal>().Entities
                .Where(a => a.TeamId == request.TeamId);

            var pagedAppeals = await query.ToPagedResultAsync(
                request,
                a => new AppealModel
                {
                    Id = a.Id,
                    TeamId = a.TeamId,
                    SubmitResultId = a.SubmitResultId,
                    Reason = a.Reason,
                    Response = a.Response,
                    Status = a.Status,
                    CreatedTime = a.CreatedTime,
                    LastUpdatedTime = a.LastUpdatedTime
                },
                cancellationToken
            );

            return pagedAppeals;
        }
    }
}




