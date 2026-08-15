using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.EventRoles.Commands.UpdateEventRole.Models;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SEAL_Application.Features.EventRoles.Commands.UpdateEventRole
{
    public class UpdateEventRoleCommandHandler : IRequestHandler<UpdateEventRoleCommand, Result<UpdateEventRoleResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateEventRoleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateEventRoleResponseModel>> Handle(UpdateEventRoleCommand request, CancellationToken cancellationToken)
        {
            var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(request.Id);
            if (eventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vai trò (EventRole) có ID '{request.Id}' không tồn tại.");
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
                if (track.EventId != eventRole.EventId)
                {
                    return BaseException.BadRequestResponse($"Hạng mục có ID '{request.Model.TrackId}' không thuộc sự kiện này.");
                }
            }

            // Validate TeamId
            if (request.Model.RoleName == EventRoleType.TeamLeader || request.Model.RoleName == EventRoleType.TeamMember)
            {
                if (string.IsNullOrEmpty(request.Model.TeamId))
                {
                    return BaseException.BadRequestResponse($"TeamId là bắt buộc đối với vai trò '{request.Model.RoleName}'.");
                }
            }

            // KHÔNG đổi sang/khỏi vai trò thành viên đội qua endpoint này — dùng luồng đội
            // (chuyển quyền trưởng nhóm, thêm/xóa thành viên) nơi có đủ ràng buộc.
            bool oldIsTeamRole = eventRole.RoleName == EventRoleType.TeamLeader || eventRole.RoleName == EventRoleType.TeamMember;
            bool newIsTeamRole = request.Model.RoleName == EventRoleType.TeamLeader || request.Model.RoleName == EventRoleType.TeamMember;
            if (eventRole.RoleName != request.Model.RoleName && (oldIsTeamRole || newIsTeamRole))
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Không thể đổi sang/khỏi vai trò thành viên đội qua chức năng này. Vui lòng dùng luồng quản lý đội.");
            }

            // Vai trò ĐÃ CHẤM ĐIỂM: không cho đổi vai trò/hạng mục — phiếu điểm sẽ gắn sai ngữ cảnh
            // (điểm chấm hạng mục A bỗng thuộc giám khảo hạng mục B).
            var hasScores = await _unitOfWork.GetRepository<Score>().AnyAsync(
                s => s.EventRoleId == eventRole.Id, cancellationToken);
            if (hasScores && (eventRole.RoleName != request.Model.RoleName || eventRole.TrackId != request.Model.TrackId))
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Vai trò này đã có phiếu chấm điểm nên không thể đổi vai trò/hạng mục.");
            }

            // Đổi vai giữa các vai trò tổ chức phải qua ma trận xung đột chung (EC/Giám khảo/Mentor
            // loại trừ nhau, không kiêm thí sinh...) — đồng bộ với luồng mời/gán.
            if (eventRole.RoleName != request.Model.RoleName || eventRole.TrackId != request.Model.TrackId)
            {
                var roleConflict = await Features.EventRoles.EventRoleValidationHelper.CheckRoleConflictAsync(
                    _unitOfWork, eventRole.UserId, eventRole.EventId, request.Model.RoleName,
                    request.Model.TrackId, eventRole.Id, cancellationToken);
                if (roleConflict != null)
                {
                    return BaseException.BadRequestDupplicationResponse(roleConflict);
                }
            }

            eventRole.TrackId = request.Model.TrackId;
            eventRole.TeamId = request.Model.TeamId;
            eventRole.RoleName = request.Model.RoleName;
            eventRole.ExpiredAt = request.Model.ExpiredAt;
            eventRole.Notes = request.Model.Notes;
            eventRole.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<EventRole>().UpdateAsync(eventRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateEventRoleResponseModel
            {
                Id = eventRole.Id,
                UserId = eventRole.UserId,
                EventId = eventRole.EventId,
                TrackId = eventRole.TrackId,
                RoleName = eventRole.RoleName.ToString(),
                ExpiredAt = eventRole.ExpiredAt
            };
        }
    }
}

