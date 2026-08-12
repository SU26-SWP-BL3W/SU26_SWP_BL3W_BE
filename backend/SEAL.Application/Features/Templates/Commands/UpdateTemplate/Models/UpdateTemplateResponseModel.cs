using System;

namespace SEAL_Application.Features.Templates.Commands.UpdateTemplate.Models
{
    public class UpdateTemplateResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
