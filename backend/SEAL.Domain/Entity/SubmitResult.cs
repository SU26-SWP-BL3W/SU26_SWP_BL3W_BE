// [FLOW3-NOPBAI][Entity] SubmitResult: bai nop cua 1 doi cho 1 Hang muc (Track) trong 1 vong thi (Round).

using SEAL_Domain.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Domain.Entity
{
    public class SubmitResult : BaseEntity
    {
        public string TeamId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string RoundId { get; set; } = string.Empty;
        public string SubmissionUrl { get; set; } = string.Empty;
        public string? RepoUrl { get; set; }
        public string? DemoUrl { get; set; }
        public string? SlideUrl { get; set; }
        public string? RepoHost { get; set; }
        public string? RepoFullName { get; set; }
        public int? RepoStars { get; set; }
        public DateTimeOffset? RepoLastPush { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Team? Team { get; set; }
        public virtual Track? Track { get; set; }
        public virtual Round? Round { get; set; }
    }
}