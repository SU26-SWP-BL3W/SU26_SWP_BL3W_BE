using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Schools.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Schools.Queries.GetAllSchools
{
    public class GetAllSchoolsQueryHandler : IRequestHandler<GetAllSchoolsQuery, Result<PagedResult<SchoolModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllSchoolsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<SchoolModel>>> Handle(GetAllSchoolsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<School>().GetQueryable();

            return await query.ToPagedResultAsync(
                request: request,
                selector: school => new SchoolModel
                {
                    Id = school.Id,
                    SchoolName = school.SchoolName,
                    Address = school.Address
                },
                cancellationToken: cancellationToken
            );
        }
    }
}



