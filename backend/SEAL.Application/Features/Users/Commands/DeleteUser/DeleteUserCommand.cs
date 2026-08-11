using SEAL_Domain.Base;
using MediatR;
using System;

namespace SEAL_Application.Features.Users.Commands.DeleteUser
{
    public class DeleteUserCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; } = string.Empty;
        public DeleteUserCommand(string id)
        {
            Id = id;
        }
    }
}

