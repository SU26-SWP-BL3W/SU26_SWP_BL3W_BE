using SEAL_Domain.Base;
using MediatR;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Application.Features.EventRoles.Queries.CheckUserHasRoleInEvent
{
    public class CheckUserHasRoleInEventQuery : IRequest<Result<bool>>
    {
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public EventRoleType RoleName { get; set; }
    }
}

