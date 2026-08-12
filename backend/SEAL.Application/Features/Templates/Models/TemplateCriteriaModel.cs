namespace SEAL_Application.Features.Templates.Models
{
    public class TemplateCriteriaModel
    {
        public string CriteriaId { get; set; } = string.Empty;
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Weight { get; set; }
        public decimal MaxScore { get; set; }
    }
}
