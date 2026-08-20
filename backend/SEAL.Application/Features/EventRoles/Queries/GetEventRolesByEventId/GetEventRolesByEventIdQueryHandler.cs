using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.EventRoles.Models;
using SEAL_Application.Features.Users.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Queries.GetEventRolesByEventId
{
    public class GetEventRolesByEventIdQueryHandler : IRequestHandler<GetEventRolesByEventIdQuery, Result<PagedResult<EventRoleModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEventRolesByEventIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<EventRoleModel>>> Handle(GetEventRolesByEventIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<EventRole>().Entities
                .Include(er => er.User)
                .Where(er => er.EventId == request.EventId);

            // Lọc theo vai trò (vd chỉ lấy Judge) — bỏ trống thì lấy tất cả vai trò.
            if (request.RoleName.HasValue)
            {
                query = query.Where(er => er.RoleName == request.RoleName.Value);
            }

            // Lọc theo hạng mục (Track) nếu có.
            if (!string.IsNullOrEmpty(request.TrackId))
            {
                query = query.Where(er => er.TrackId == request.TrackId);
            }

            // Chỉ lấy vai trò còn hiệu lực (ExpiredAt hoặc fallback Event.EndDate).
            if (request.ActiveOnly)
            {
                var now = System.DateTime.UtcNow;
                query = query.Where(er => (er.ExpiredAt ?? er.Event.EndDate) > now);
            }

            return await query.ToPagedResultAsync(
                request,
                er => new EventRoleModel
                {
                    Id = er.Id,
                    UserId = er.UserId,
                    EventId = er.EventId,
                    TrackId = er.TrackId,
                    TeamId = er.TeamId,
                    RoleName = er.RoleName.ToString(),
                    EventName = er.Event != null ? er.Event.EventName : null,
                    EventEndDate = er.Event != null ? er.Event.EndDate : null,
                    TrackName = er.Track != null ? er.Track.TrackName : null,
                    TeamName = er.Team != null ? er.Team.Name : null,
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
                },
                cancellationToken);
        }
    }
}



