using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.Templates.Models
{
    public class TemplateModel
    {
        public string Id { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        public List<TemplateCriteriaModel> Criterias { get; set; } = new List<TemplateCriteriaModel>();
    }
}
