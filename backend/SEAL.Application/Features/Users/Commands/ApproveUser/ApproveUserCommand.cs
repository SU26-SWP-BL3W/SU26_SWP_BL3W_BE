using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Users.Commands.ApproveUser
{
    public class ApproveUserCommand : IRequest<Result<UserModel>>
    {
        public string Id { get; set; } = string.Empty;
    }
}

