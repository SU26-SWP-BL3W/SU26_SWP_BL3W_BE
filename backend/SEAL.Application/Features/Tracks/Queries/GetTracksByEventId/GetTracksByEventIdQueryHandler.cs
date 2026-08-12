using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Tracks.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Features.Users.Models;

namespace SEAL_Application.Features.Tracks.Queries.GetTracksByEventId
{
    public class GetTracksByEventIdQueryHandler : IRequestHandler<GetTracksByEventIdQuery, Result<PagedResult<TrackModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTracksByEventIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<TrackModel>>> Handle(GetTracksByEventIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<Track>().Entities
                .Include(t => t.EventRoles)
                    .ThenInclude(er => er.User)
                .Where(t => t.Round.EventId == request.EventId);

            var pagedTracks = await query.ToPagedResultAsync(
                request,
                t => t,
                cancellationToken
            );

            var trackModels = pagedTracks.Data.Select(t => new TrackModel
            {
                Id = t.Id,
                RoundId = t.RoundId,
                TemplateId = t.TemplateId,
                TrackName = t.TrackName,
                Description = t.Description,
                SubmissionRuleDescription = t.SubmissionRuleDescription,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                ScoringStartDate = t.ScoringStartDate,
                ScoringEndDate = t.ScoringEndDate,
                Judges = t.EventRoles.Where(er => er.RoleName == EventRoleType.Judge && er.User != null).Select(er => new UserModel
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
                Mentors = t.EventRoles.Where(er => er.RoleName == EventRoleType.Mentor && er.User != null).Select(er => new UserModel
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
                CreatedTime = t.CreatedTime,
                LastUpdatedTime = t.LastUpdatedTime
            }).ToList();

            return new PagedResult<TrackModel>(
                trackModels,
                pagedTracks.CurrentPage,
                pagedTracks.PageSize,
                pagedTracks.TotalItems
            );
        }
    }
}



