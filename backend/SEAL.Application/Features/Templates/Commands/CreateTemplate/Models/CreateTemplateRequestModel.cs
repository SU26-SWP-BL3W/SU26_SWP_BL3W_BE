namespace SEAL_Application.Features.Templates.Commands.CreateTemplate.Models
{
    public class CreateTemplateRequestModel
    {
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
