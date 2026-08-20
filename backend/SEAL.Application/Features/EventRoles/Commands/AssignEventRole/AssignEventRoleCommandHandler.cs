using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.EventRoles.Commands.AssignEventRole.Models;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SEAL_Application.Features.EventRoles.Commands.AssignEventRole
{
    public class AssignEventRoleCommandHandler : IRequestHandler<AssignEventRoleCommand, Result<AssignEventRoleResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly ICurrentUserService _currentUserService;

        public AssignEventRoleCommandHandler(
            IUnitOfWork unitOfWork,
            IEventRoleChecker eventRoleChecker,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _eventRoleChecker = eventRoleChecker;
            _currentUserService = currentUserService;
        }

        public async Task<Result<AssignEventRoleResponseModel>> Handle(AssignEventRoleCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra người dùng có tồn tại trong DB không
            var userExists = await _unitOfWork.GetRepository<User>().AnyAsync(u => u.Id == request.Model.UserId, cancellationToken);
            if (!userExists)
            {
                return BaseException.BadRequestNotFoundResponse($"Người dùng có ID '{request.Model.UserId}' không tồn tại.");
            }

            // 2. Kiểm tra sự kiện có tồn tại trong DB không
            var targetEvent = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.Model.EventId);
            if (targetEvent == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{request.Model.EventId}' không tồn tại.");
            }

            if (request.Model.RoleName == EventRoleType.EventCoordinator)
            {
                var currentUserId = _currentUserService.UserId;
                if (!string.IsNullOrEmpty(currentUserId) && request.Model.UserId == currentUserId)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        "Bạn không thể tự gán chính mình làm Event Coordinator. Vui lòng nhờ Admin khác phân công.");
                }
            }

            // Kiểm tra TrackId (nếu có)
            if (!string.IsNullOrEmpty(request.Model.TrackId))
            {
                var track = await _unitOfWork.GetRepository<Track>()
                    .GetByIdAsync(request.Model.TrackId);
                if (track == null)
                {
                    return BaseException.BadRequestNotFoundResponse($"Hạng mục (Track) có ID '{request.Model.TrackId}' không tồn tại.");
                }
                if (track.EventId != request.Model.EventId)
                {
                    return BaseException.BadRequestResponse($"Hạng mục có ID '{request.Model.TrackId}' không thuộc sự kiện này.");
                }
            }

            // 4. KIỂM TRA NGHIỆP VỤ NHÓM (Dành riêng cho vai trò TeamLeader và TeamMember)
            if (request.Model.RoleName == EventRoleType.TeamLeader || request.Model.RoleName == EventRoleType.TeamMember)
            {
                if (string.IsNullOrEmpty(request.Model.TeamId))
                {
                    return BaseException.BadRequestResponse($"TeamId là bắt buộc đối với vai trò '{request.Model.RoleName}'.");
                }

                // Giải quyết TODO: Xác thực xem TeamId có thực sự tồn tại trong DB không (Fix 8)
                var teamExists = await _unitOfWork.GetRepository<Team>().AnyAsync(t => t.Id == request.Model.TeamId, cancellationToken);
                if (!teamExists)
                {
                    return BaseException.BadRequestNotFoundResponse($"Nhóm có ID '{request.Model.TeamId}' không tồn tại.");
                }

                // Áp dụng quy tắc "1 user / 1 team / 1 event" (Fix 3)
                // Kiểm tra xem người dùng đã tham gia bất kỳ nhóm nào khác trong sự kiện này chưa
                var alreadyInTeam = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                    er => er.UserId == request.Model.UserId
                       && er.EventId == request.Model.EventId
                       && !string.IsNullOrEmpty(er.TeamId),
                    cancellationToken);

                if (alreadyInTeam)
                {
                    return BaseException.BadRequestResponse("Người dùng đã tham gia một nhóm khác trong sự kiện này.");
                }
            }

            // 4b. Kiểm tra ma trận xung đột vai trò (EC / Giám khảo / Mentor / Thí sinh)
            var roleConflict = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _unitOfWork, request.Model.UserId, request.Model.EventId, request.Model.RoleName, request.Model.TrackId, null, cancellationToken);
            if (roleConflict != null)
            {
                return BaseException.BadRequestDupplicationResponse(roleConflict);
            }

            // 5. Khởi tạo đối tượng EventRole mới
            var eventRole = new EventRole
            {
                UserId = request.Model.UserId,
                EventId = request.Model.EventId,
                TrackId = request.Model.TrackId,
                TeamId = request.Model.TeamId,
                RoleName = request.Model.RoleName,
                AssignedAt = DateTime.UtcNow,
                ExpiredAt = request.Model.ExpiredAt ?? targetEvent.EndDate,
                Notes = request.Model.Notes
            };

            await _unitOfWork.GetRepository<EventRole>().AddAsync(eventRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _eventRoleChecker.InvalidateCache(eventRole.UserId, eventRole.EventId);

            return new AssignEventRoleResponseModel
            {
                Id = eventRole.Id,
                UserId = eventRole.UserId,
                EventId = eventRole.EventId,
                TrackId = eventRole.TrackId,
                RoleName = eventRole.RoleName.ToString(),
                AssignedAt = eventRole.AssignedAt
            };
        }
    }
}
