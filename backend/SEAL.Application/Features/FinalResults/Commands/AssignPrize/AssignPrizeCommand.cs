using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.FinalResults.Commands.AssignPrize
{
    public class AssignPrizeCommand : IRequest<Result<bool>>
    {
        public string FinalResultId { get; set; } = string.Empty;
        public AssignPrizeRequestModel Payload { get; set; } = null!;
    }
}
