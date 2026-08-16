using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateMentorFeedback
{
    public class CreateMentorFeedbackCommand : IRequest<Result<string>>
    {
        public string SubmitResultId { get; set; } = string.Empty;
        public CreateMentorFeedbackRequestModel Model { get; set; } = new();
    }
}
