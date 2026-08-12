using System.Collections.Generic;

namespace SEAL_Application.Features.Criterias.Commands.CreateCriteria.Models
{
    public class CreateCriteriaRequestModel
    {
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
