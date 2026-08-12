using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Criterias.Models
{
    public class CriteriaModel
    {
        public string Id { get; set; } = string.Empty;
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
