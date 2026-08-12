using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Schools.Models;

namespace SEAL_Application.Features.Schools.Queries.GetSchoolsWithUserCount
{
    public class GetSchoolsWithUserCountQueryHandler : IRequestHandler<GetSchoolsWithUserCountQuery, Result<IEnumerable<SchoolWithUserCountModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSchoolsWithUserCountQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IEnumerable<SchoolWithUserCountModel>>> Handle(GetSchoolsWithUserCountQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<School>().GetQueryable()
                .Where(x => !x.DeletedTime.HasValue)
                .Select(school => new SchoolWithUserCountModel
                {
                    Id = school.Id,
                    SchoolName = school.SchoolName,
                    Address = school.Address,
                    UserCount = school.Users.Count(u => !u.DeletedTime.HasValue)
                });

            return await query.ToListAsync(cancellationToken);
        }
    }
}



