using System;

namespace SEAL_Application.Features.Templates.Commands.CreateTemplate.Models
{
    public class CreateTemplateResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
