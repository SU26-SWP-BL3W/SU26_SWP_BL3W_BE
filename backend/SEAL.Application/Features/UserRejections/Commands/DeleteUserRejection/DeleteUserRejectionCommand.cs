using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.UserRejections.Commands.DeleteUserRejection
{
    public class DeleteUserRejectionCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteUserRejectionCommand(string id)
        {
            Id = id;
        }
    }
}

