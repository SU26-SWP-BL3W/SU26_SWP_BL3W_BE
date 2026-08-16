using System;

namespace SEAL_Application.Features.SubmitResults.Queries.GetMentorFeedbacks.Models
{
    public class MentorFeedbackModel
    {
        public string Id { get; set; } = string.Empty;
        public string SubmitResultId { get; set; } = string.Empty;
        public string EventRoleId { get; set; } = string.Empty;
        public string MentorName { get; set; } = string.Empty;
        public string MentorEmail { get; set; } = string.Empty;
        public string FeedbackText { get; set; } = string.Empty;
        public DateTimeOffset CreatedTime { get; set; }
    }
}
