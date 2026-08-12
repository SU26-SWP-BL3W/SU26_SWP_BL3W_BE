using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.EventRoles.Commands.InviteEventRole.Models;

namespace SEAL_Application.Features.EventRoles.Commands.InviteEventRole
{
    public class InviteEventRoleCommand : IRequest<Result<InviteEventRoleResponseModel>>
    {
        public InviteEventRoleRequestModel Model { get; set; } = new InviteEventRoleRequestModel();
    }
}

