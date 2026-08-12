using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.EventRoles.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.EventRoles.Queries.GetEventRolesByUserId
{
    public class GetEventRolesByUserIdQuery : BasePaginationQuery, IRequest<Result<PagedResult<EventRoleModel>>>
    {
        public string UserId { get; set; } = string.Empty;

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "RoleName", "AssignedAt", "CreatedTime" };
    }
}

