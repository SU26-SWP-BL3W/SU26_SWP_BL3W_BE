using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateMentorFeedback
{
    public class CreateMentorFeedbackCommand : IRequest<Result<MentorFeedbackModel>>
    {
        public string SubmitResultId { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }

    public class MentorFeedbackModel
    {
        public string Id { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string MentorId { get; set; } = string.Empty;
        public string? MentorName { get; set; }
        public string Comment { get; set; } = string.Empty;
        public System.DateTimeOffset CreatedTime { get; set; }
    }
}
