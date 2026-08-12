using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.EventRoles.Commands.UpdateEventRole.Models;

namespace SEAL_Application.Features.EventRoles.Commands.UpdateEventRole
{
    public class UpdateEventRoleCommand : IRequest<Result<UpdateEventRoleResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateEventRoleRequestModel Model { get; set; } = null!;
    }
}

