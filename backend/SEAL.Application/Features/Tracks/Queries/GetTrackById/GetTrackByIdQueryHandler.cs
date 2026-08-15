using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Tracks.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Tracks.Queries.GetTrackById
{
    public class GetTrackByIdQueryHandler : IRequestHandler<GetTrackByIdQuery, Result<TrackModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTrackByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TrackModel?>> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var track = await _unitOfWork.GetRepository<Track>().GetQueryable()
                .Include(t => t.EventRoles)
                    .ThenInclude(er => er.User)
                .SingleOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
            if (track == null)
            {
                return null;
            }

            return new TrackModel
            {
                Id = track.Id,
                EventId = track.EventId,
                TemplateId = track.TemplateId,
                TrackName = track.TrackName,
                Description = track.Description,
                SubmissionRuleDescription = track.SubmissionRuleDescription,
                StartDate = track.StartDate,
                EndDate = track.EndDate,
                ScoringStartDate = track.ScoringStartDate,
                ScoringEndDate = track.ScoringEndDate,
                Judges = track.EventRoles.Where(er => er.RoleName == EventRoleType.Judge && er.User != null).Select(er => new UserModel
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
                }).ToList(),
                Mentors = track.EventRoles.Where(er => er.RoleName == EventRoleType.Mentor && er.User != null).Select(er => new UserModel
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
                }).ToList(),
                CreatedTime = track.CreatedTime,
                LastUpdatedTime = track.LastUpdatedTime
            };
        }
    }
}
