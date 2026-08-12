using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.EventRoles.Commands.RemoveEventRole
{
    public class RemoveEventRoleCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public RemoveEventRoleCommand(string id)
        {
            Id = id;
        }
    }
}

