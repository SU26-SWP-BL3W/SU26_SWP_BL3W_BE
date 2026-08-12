using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.UpdateTeam.Models
{
    public class UpdateTeamResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; } // Khớp trường LastUpdatedTime dạng DateTimeOffset
    }
}
