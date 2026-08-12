using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.EventRoles.Models;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Queries.GetUserRoleInEvent
{
    public class GetUserRoleInEventQueryHandler : IRequestHandler<GetUserRoleInEventQuery, Result<EventRoleModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserRoleInEventQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<EventRoleModel?>> Handle(GetUserRoleInEventQuery request, CancellationToken cancellationToken)
        {
            var er = await _unitOfWork.GetRepository<EventRole>().Entities
                .Include(er => er.User)
                .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.EventId == request.EventId, cancellationToken);

            if (er == null) return Result.Success<EventRoleModel?>(null);

            return Result.Success<EventRoleModel?>(new EventRoleModel
            {
                Id = er.Id,
                UserId = er.UserId,
                EventId = er.EventId,
                TrackId = er.TrackId,
                TeamId = er.TeamId,
                RoleName = er.RoleName.ToString(),
                User = er.User != null ? new UserModel
                {
                    Id = er.User.Id,
                    SchoolId = er.User.SchoolId,
                    StudentCode = er.User.StudentCode,
                    Email = er.User.Email,
                    FullName = er.User.FullName,
                    IsStudent = er.User.IsStudent,
                    IsAdmin = er.User.IsAdmin,
                    IsApproved = er.User.IsApproved,
                    IsFpt = er.User.IsFpt,
                    PhotoStudentCardUrl = er.User.PhotoStudentCardUrl
                } : null,
                AssignedAt = er.AssignedAt,
                ExpiredAt = er.ExpiredAt,
                Notes = er.Notes,
                CreatedTime = er.CreatedTime,
                LastUpdatedTime = er.LastUpdatedTime
            });
        }
    }
}

