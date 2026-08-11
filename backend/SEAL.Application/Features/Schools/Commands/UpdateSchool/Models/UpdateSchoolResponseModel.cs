using System;

namespace SEAL_Application.Features.Schools.Commands.UpdateSchool.Models
{
    public class UpdateSchoolResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
