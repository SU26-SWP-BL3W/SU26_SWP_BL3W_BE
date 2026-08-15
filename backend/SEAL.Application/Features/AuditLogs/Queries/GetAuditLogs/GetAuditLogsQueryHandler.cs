using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Commons;
using SEAL_Application.Features.AuditLogs.Queries.GetAuditLogs.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventRoleChecker _eventRoleChecker;

        public GetAuditLogsQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<PagedResult<AuditLogItemModel>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            if (string.IsNullOrWhiteSpace(request.EventId))
            {
                return BaseException.BadRequestInvaildInputResponse("EventId không được để trống.");
            }

            var eventExists = await _unitOfWork.GetRepository<Event>().AnyAsync(e => e.Id == request.EventId, cancellationToken);
            if (!eventExists)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện '{request.EventId}' không tồn tại.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId, request.EventId, new[] { EventRoleType.EventCoordinator }, cancellationToken);
            if (!isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Chỉ Admin hoặc Điều phối viên được xem nhật ký kiểm tra.");
            }

            var query = _unitOfWork.GetRepository<AuditLog>().Entities
                .AsNoTracking()
                .Where(a => a.EventId == request.EventId);

            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                query = query.Where(a => a.Action == request.Action);
            }
            if (!string.IsNullOrWhiteSpace(request.EntityType))
            {
                query = query.Where(a => a.EntityType == request.EntityType);
            }
            if (!string.IsNullOrWhiteSpace(request.EntityId))
            {
                query = query.Where(a => a.EntityId == request.EntityId);
            }

            if (string.IsNullOrEmpty(request.SortBy))
            {
                request.SortBy = "CreatedTime";
                request.IsAscending = false;
            }

            var paged = await query.ToPagedResultAsync(
                request,
                a => new AuditLogItemModel
                {
                    Id = a.Id,
                    EventId = a.EventId,
                    ActorUserId = a.ActorUserId,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Summary = a.Summary,
                    PayloadJson = a.PayloadJson,
                    CreatedTime = a.CreatedTime
                },
                cancellationToken);

            var actorIds = paged.Data.Select(x => x.ActorUserId).Distinct().ToList();
            if (actorIds.Count > 0)
            {
                var names = await _unitOfWork.GetRepository<User>().Entities
                    .AsNoTracking()
                    .Where(u => actorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync(cancellationToken);
                var nameMap = names.ToDictionary(x => x.Id, x => x.FullName);
                foreach (var item in paged.Data)
                {
                    if (nameMap.TryGetValue(item.ActorUserId, out var name))
                    {
                        item.ActorName = name;
                    }
                }
            }

            return paged;
        }
    }
}
