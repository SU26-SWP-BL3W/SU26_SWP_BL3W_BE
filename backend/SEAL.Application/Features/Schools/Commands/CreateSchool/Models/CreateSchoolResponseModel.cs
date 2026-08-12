using System;

namespace SEAL_Application.Features.Schools.Commands.CreateSchool.Models
{
    public class CreateSchoolResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
