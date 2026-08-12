using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult.Models;

namespace SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult
{
    public class UpdateFinalResultCommand : IRequest<Result<UpdateFinalResultResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateFinalResultRequestModel Model { get; set; } = new UpdateFinalResultRequestModel();
    }
}

