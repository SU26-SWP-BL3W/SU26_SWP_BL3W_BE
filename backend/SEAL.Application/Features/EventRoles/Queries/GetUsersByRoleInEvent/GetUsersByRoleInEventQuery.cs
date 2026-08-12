using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using SEAL_Application.Features.Users.Models; // Reusing UserModel from Users feature

namespace SEAL_Application.Features.EventRoles.Queries.GetUsersByRoleInEvent
{
    public class GetUsersByRoleInEventQuery : BasePaginationQuery, IRequest<Result<PagedResult<UserModel>>>
    {
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public EventRoleType RoleName { get; set; }

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "FullName", "Email" };
    }
}

