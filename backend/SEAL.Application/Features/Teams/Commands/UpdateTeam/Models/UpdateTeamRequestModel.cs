using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.UpdateTeam.Models
{
    public class UpdateTeamRequestModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>null = giữ nguyên. Client quên gửi mà mặc định false thì đội bị TẮT NGẦM —
        /// vì vậy phải nullable. Chỉ Event Coordinator/Admin được thay đổi.</summary>
        public bool? IsActive { get; set; }
    }
}
