using SEAL_Domain.Entity.Enums;
using System;

namespace SEAL_Application.Features.Teams.Queries.GetTeamsList.Models
{
    public class TeamListItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TrackId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TeamStatus Status { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
