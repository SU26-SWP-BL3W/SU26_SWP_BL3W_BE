using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Users.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Queries.GetUsersByRoleInEvent
{
    public class GetUsersByRoleInEventQueryHandler : IRequestHandler<GetUsersByRoleInEventQuery, Result<PagedResult<UserModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUsersByRoleInEventQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<UserModel>>> Handle(GetUsersByRoleInEventQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.EventId == request.EventId && er.RoleName == request.RoleName);

            if (!string.IsNullOrEmpty(request.TrackId))
            {
                query = query.Where(er => er.TrackId == request.TrackId);
            }

            var userQuery = query.Select(er => er.User); // Lấy User từ EventRole

            return await userQuery.ToPagedResultAsync(
                request,
                u => new UserModel
                {
                    Id = u.Id,
                    SchoolId = u.SchoolId,
                    StudentCode = u.StudentCode,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsStudent = u.IsStudent,
                    IsAdmin = u.IsAdmin,
                    IsApproved = u.IsApproved
                },
                cancellationToken);
        }
    }
}



