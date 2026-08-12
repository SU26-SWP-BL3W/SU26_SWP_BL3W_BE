using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.EventRoles.Models;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;

namespace SEAL_Application.Features.EventRoles.Queries.GetEventRolesByEventId
{
    public class GetEventRolesByEventIdQuery : BasePaginationQuery, IRequest<Result<PagedResult<EventRoleModel>>>
    {
        public string EventId { get; set; } = string.Empty;

        /// <summary>Lọc theo vai trò (vd Judge, Mentor, TeamLeader...). Bỏ trống = lấy tất cả vai trò.</summary>
        public EventRoleType? RoleName { get; set; }

        /// <summary>Lọc theo hạng mục (Track) được gán. Tùy chọn.</summary>
        public string? TrackId { get; set; }

        /// <summary>Chỉ lấy vai trò còn hiệu lực (chưa hết hạn). Mặc định false = lấy tất cả.</summary>
        public bool ActiveOnly { get; set; }

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "RoleName", "AssignedAt", "CreatedTime" };
    }
}

