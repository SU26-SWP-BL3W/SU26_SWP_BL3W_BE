using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Criterias.Commands.UpdateCriteria.Models
{
    public class UpdateCriteriaResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
