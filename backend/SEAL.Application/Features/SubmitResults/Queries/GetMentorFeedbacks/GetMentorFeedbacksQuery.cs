using MediatR;
using SEAL_Application.Features.SubmitResults.Commands.CreateMentorFeedback;
using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Application.Features.SubmitResults.Queries.GetMentorFeedbacks
{
    public class GetMentorFeedbacksQuery : IRequest<Result<List<MentorFeedbackModel>>>
    {
        public string SubmitResultId { get; set; } = string.Empty;
    }
}
