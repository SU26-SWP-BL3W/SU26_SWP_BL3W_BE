using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Appeals.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Appeals.Queries.GetAssignedAppeals
{
    public class GetAssignedAppealsQueryHandler : IRequestHandler<GetAssignedAppealsQuery, Result<List<AppealModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetAssignedAppealsQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<List<AppealModel>>> Handle(GetAssignedAppealsQuery request, CancellationToken cancellationToken)
        {
            // Chỉ chính giám khảo giữ EventRoleId này, hoặc Admin/EC của sự kiện đó, mới được xem.
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Vui lòng đăng nhập.");
            }

            var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(request.EventRoleId);
            if (eventRole == null)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xem danh sách phúc khảo này.");
            }

            bool isOwnRole = eventRole.UserId == currentUserId;
            bool isCoordinator = !isOwnRole && await _eventRoleChecker.HasRoleAsync(
                currentUserId, eventRole.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);

            if (!isOwnRole && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xem danh sách phúc khảo này.");
            }

            var appeals = await _unitOfWork.GetRepository<Appeal>().Entities
                .AsNoTracking()
                .Where(a => a.Status == AppealStatus.Approved && a.AssignedJudgeId == request.EventRoleId)
                .OrderByDescending(a => a.LastUpdatedTime)
                .Select(a => new AppealModel
                {
                    Id = a.Id,
                    TeamId = a.TeamId,
                    SubmitResultId = a.SubmitResultId,
                    Reason = a.Reason,
                    Response = a.Response,
                    Status = a.Status,
                    AssignedJudgeId = a.AssignedJudgeId,
                    CreatedTime = a.CreatedTime,
                    LastUpdatedTime = a.LastUpdatedTime
                })
                .ToListAsync(cancellationToken);

            return appeals;
        }
    }
}



