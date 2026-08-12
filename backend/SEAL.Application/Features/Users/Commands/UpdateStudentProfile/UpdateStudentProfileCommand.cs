using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Users.Commands.UpdateStudentProfile
{
    public class UpdateStudentProfileCommand : IRequest<Result<UserModel>>
    {
        public string SchoolId { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public string? PhotoStudentCardUrl { get; set; }
        public bool IsFpt { get; set; }
        public string? FullName { get; set; }
    }
}

