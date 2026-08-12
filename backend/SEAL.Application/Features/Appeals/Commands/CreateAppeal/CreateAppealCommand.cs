using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Appeals.Commands.CreateAppeal
{
    public class CreateAppealCommand : IRequest<Result<bool>>
    {
        public string SubmitResultId { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
    }
}

