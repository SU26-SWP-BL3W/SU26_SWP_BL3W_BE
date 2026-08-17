using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.SubmitResults.Commands.DeleteMentorFeedback
{
    public class DeleteMentorFeedbackCommand : IRequest<Result<bool>>
    {
        public string FeedbackId { get; set; } = string.Empty;
    }
}
