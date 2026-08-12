using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Users.Commands.RejectUser
{
    public class RejectUserCommand : IRequest<Result<UserModel>>
    {
        public string Id { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}

