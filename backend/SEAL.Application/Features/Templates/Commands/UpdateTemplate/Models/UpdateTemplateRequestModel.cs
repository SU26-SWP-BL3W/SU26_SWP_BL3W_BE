namespace SEAL_Application.Features.Templates.Commands.UpdateTemplate.Models
{
    public class UpdateTemplateRequestModel
    {
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
