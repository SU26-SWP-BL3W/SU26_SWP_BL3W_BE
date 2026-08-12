using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.FinalResults.Commands.CreateFinalResult.Models;

namespace SEAL_Application.Features.FinalResults.Commands.CreateFinalResult
{
    public class CreateFinalResultCommand : IRequest<Result<CreateFinalResultResponseModel>>
    {
        public CreateFinalResultRequestModel Model { get; set; } = new CreateFinalResultRequestModel();
    }
}

