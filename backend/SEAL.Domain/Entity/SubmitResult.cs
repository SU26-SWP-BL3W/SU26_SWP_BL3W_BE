using SEAL_Domain.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Domain.Entity
{
    public class SubmitResult : BaseEntity
    {
        public string TeamId { get; set; } = string.Empty;

        // Bài nộp gắn trực tiếp với Track; Vòng thi (Round) được suy ra qua Track.RoundId.
        public string TrackId { get; set; } = string.Empty;
        public string SubmissionUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual Team? Team { get; set; }
        public virtual Track? Track { get; set; }
    }
}