using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult.Models
{
    public class CreateSubmitResultRequestModel
    {
        public string TeamId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string RoundId { get; set; } = string.Empty;
        public string SubmissionUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
