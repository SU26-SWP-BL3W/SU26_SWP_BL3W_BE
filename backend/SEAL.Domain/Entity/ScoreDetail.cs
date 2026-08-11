using SEAL_Domain.Base;

namespace SEAL_Domain.Entity
{
    public class ScoreDetail : BaseEntity
    {
        public string ScoreId { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Value { get; set; }

        // Navigation Properties
        public virtual Score Score { get; set; } = null!;
        public virtual TemplateCriteria TemplateCriteria { get; set; } = null!;
    }
}
