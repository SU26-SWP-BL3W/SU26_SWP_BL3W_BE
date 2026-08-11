using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Commands.UpdateUser.Models;

namespace SEAL_Application.Features.Users.Commands.UpdateUser
{
    public class UpdateUserCommand : IRequest<Result<UpdateUserResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateUserRequestModel Model { get; set; } = new UpdateUserRequestModel();
    }
}

