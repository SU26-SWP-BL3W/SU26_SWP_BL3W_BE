using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Teams.Queries.GetTeamById.Models
{
    public class TeamDetailResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// Lý do đội bị từ chối duyệt ở lần chốt danh sách gần nhất; null nếu không có.
        /// FE dùng cặp (Status == "Forming" &amp;&amp; LastRejectReason != null) để hiện banner nhắc đội sửa lại.
        /// </summary>
        public string? LastRejectReason { get; set; }

        public List<TeamMemberDetailModel> Members { get; set; } = new List<TeamMemberDetailModel>();
    }

    public class TeamMemberDetailModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }
}