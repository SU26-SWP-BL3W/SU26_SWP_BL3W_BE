using SEAL_Domain.Base;
using System.Collections.Generic;
namespace SEAL_Domain.Entity
{
    public class Criteria : BaseEntity
    {
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual ICollection<TemplateCriteria> TemplateCriterias { get; set; } = new List<TemplateCriteria>();
    }
}
