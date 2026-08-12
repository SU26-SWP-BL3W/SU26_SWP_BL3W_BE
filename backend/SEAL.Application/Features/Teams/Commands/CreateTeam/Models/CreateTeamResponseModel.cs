using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.CreateTeam.Models
{
    public class CreateTeamResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; } // Đồng bộ kiểu DateTimeOffset
    }
}