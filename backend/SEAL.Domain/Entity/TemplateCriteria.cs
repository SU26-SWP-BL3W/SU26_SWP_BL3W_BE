namespace SEAL_Domain.Entity
{
    public class TemplateCriteria
    {
        public string TemplateId { get; set; } = string.Empty;
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal MaxScore { get; set; }

        // Navigation Properties
        public virtual Template Template { get; set; } = null!;
        public virtual Criteria Criteria { get; set; } = null!;
    }
}
