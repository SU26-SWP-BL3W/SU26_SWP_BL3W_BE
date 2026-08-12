using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.EventRoles.Models;

namespace SEAL_Application.Features.EventRoles.Queries.GetUserRoleInEvent
{
    public class GetUserRoleInEventQuery : IRequest<Result<EventRoleModel?>>
    {
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
    }
}

