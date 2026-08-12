namespace SEAL_Application.Features.Schools.Commands.CreateSchool.Models
{
    public class CreateSchoolRequestModel
    {
        public string SchoolName { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}
