using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Commands.RequestUnblock.Models;

namespace SEAL_Application.Features.Users.Commands.RequestUnblock
{
    public class RequestUnblockCommand : IRequest<Result<RequestUnblockResponseModel>>
    {
        public RequestUnblockRequestModel Model { get; set; } = new RequestUnblockRequestModel();
    }
}

