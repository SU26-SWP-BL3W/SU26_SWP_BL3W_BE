using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection.Models;

namespace SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection
{
    public class UpdateUserRejectionCommand : IRequest<Result<UpdateUserRejectionResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateUserRejectionRequestModel Model { get; set; } = new UpdateUserRejectionRequestModel();
    }
}

