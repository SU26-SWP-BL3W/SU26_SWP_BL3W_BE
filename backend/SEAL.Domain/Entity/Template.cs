using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Domain.Entity
{
    public class Template : BaseEntity
    {
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation Properties
        public virtual ICollection<TemplateCriteria> TemplateCriterias { get; set; } = new List<TemplateCriteria>();
        public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
    }
}
