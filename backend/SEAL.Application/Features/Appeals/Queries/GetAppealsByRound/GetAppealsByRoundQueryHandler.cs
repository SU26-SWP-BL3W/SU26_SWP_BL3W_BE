using MediatR;
using Microsoft.EntityFrameworkCore;
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

namespace SEAL_Application.Features.Appeals.Queries.GetAppealsByRound
{
    public class GetAppealsByRoundQueryHandler : IRequestHandler<GetAppealsByRoundQuery, Result<PagedResult<AppealModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetAppealsByRoundQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<AppealModel>>> Handle(GetAppealsByRoundQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Vui lòng đăng nhập.");
            }

            var query = _unitOfWork.GetRepository<Appeal>().Entities
                .Where(a => a.SubmitResult.RoundId == request.RoundId);

            // Giới hạn phạm vi xem: chỉ Admin, EC của sự kiện chứa vòng này, hoặc Judge/Mentor
            // được phân công (cấp sự kiện hoặc đúng track trong vòng này) mới xem được danh sách phúc khảo.
            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            if (!isAdmin)
            {
                var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.RoundId);
                if (round == null)
                {
                    return new BaseException.ForbiddenException("Bạn không có quyền xem phúc khảo của vòng thi này.");
                }

                var roles = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                    .AsNoTracking()
                    .Where(er => er.UserId == userId && er.EventId == round.EventId)
                    .Select(er => new { er.RoleName, er.TrackId })
                    .ToListAsync(cancellationToken);

                bool isEventCoordinator = roles.Any(r => r.RoleName == EventRoleType.EventCoordinator);
                var judgeMentorRoles = roles
                    .Where(r => r.RoleName == EventRoleType.Judge || r.RoleName == EventRoleType.Mentor)
                    .ToList();
                bool hasEventWideJudgeRole = judgeMentorRoles.Any(r => string.IsNullOrEmpty(r.TrackId));

                if (isEventCoordinator || hasEventWideJudgeRole)
                {
                    // Xem toàn bộ phúc khảo trong vòng — không lọc thêm.
                }
                else if (judgeMentorRoles.Count > 0)
                {
                    var assignedTrackIds = judgeMentorRoles
                        .Where(r => !string.IsNullOrEmpty(r.TrackId))
                        .Select(r => r.TrackId!)
                        .Distinct()
                        .ToList();
                    query = query.Where(a => assignedTrackIds.Contains(a.SubmitResult.TrackId));
                }
                else
                {
                    return new BaseException.ForbiddenException("Bạn không có quyền xem phúc khảo của vòng thi này.");
                }
            }

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




