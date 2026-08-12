namespace SEAL_Application.Features.Templates.Commands.AddCriteriaToTemplate.Models
{
    public class AddCriteriaToTemplateRequestModel
    {
        public string CriteriaId { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal MaxScore { get; set; }
    }
}
