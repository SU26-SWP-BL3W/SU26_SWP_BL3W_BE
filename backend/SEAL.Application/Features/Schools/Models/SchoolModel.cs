namespace SEAL_Application.Features.Schools.Models
{
    public class SchoolModel
    {
        public string Id { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    public class SchoolWithUserCountModel : SchoolModel
    {
        public int UserCount { get; set; }
    }
}
