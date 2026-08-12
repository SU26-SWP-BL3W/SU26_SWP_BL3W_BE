using SEAL_Domain.Base;
using MediatR;
using System;

namespace SEAL_Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommand : IRequest<Result<bool>>
    {
        public string UserId { get; set; }
        public ChangePasswordRequestModel Model { get; set; }

        public ChangePasswordCommand(string userId, ChangePasswordRequestModel model)
        {
            UserId = userId;
            Model = model;
        }
    }
}

