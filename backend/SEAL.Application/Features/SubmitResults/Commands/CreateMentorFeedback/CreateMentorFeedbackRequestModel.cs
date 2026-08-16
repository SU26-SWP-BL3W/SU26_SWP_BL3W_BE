using System.ComponentModel.DataAnnotations;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateMentorFeedback
{
    public class CreateMentorFeedbackRequestModel
    {
        [Required(ErrorMessage = "Nội dung nhận xét không được để trống.")]
        [MaxLength(2000, ErrorMessage = "Nội dung nhận xét tối đa 2000 ký tự.")]
        public string FeedbackText { get; set; } = string.Empty;
    }
}
