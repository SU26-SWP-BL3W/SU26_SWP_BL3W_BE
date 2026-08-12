using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.EventRoles.Commands.AssignEventRole.Models;

namespace SEAL_Application.Features.EventRoles.Commands.AssignEventRole
{
    public class AssignEventRoleCommand : IRequest<Result<AssignEventRoleResponseModel>>
    {
        public AssignEventRoleRequestModel Model { get; set; } = null!;
    }
}

