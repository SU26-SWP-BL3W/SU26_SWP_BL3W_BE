using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Schools.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Schools.Queries.GetSchoolById
{
    public class GetSchoolByIdQueryHandler : IRequestHandler<GetSchoolByIdQuery, Result<SchoolModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSchoolByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        public async Task<Result<SchoolModel?>> Handle(GetSchoolByIdQuery request, CancellationToken cancellationToken)
        {
            var school = await _unitOfWork.GetRepository<School>().GetByIdAsync(request.Id);
            if (school == null) return null;

            return new SchoolModel
            {
                Id = school.Id,
                SchoolName = school.SchoolName,
                Address = school.Address
            };
        }
    }
}

