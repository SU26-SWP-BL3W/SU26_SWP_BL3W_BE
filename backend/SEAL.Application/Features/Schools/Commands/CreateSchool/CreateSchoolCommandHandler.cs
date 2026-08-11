using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Schools.Commands.CreateSchool.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Schools.Commands.CreateSchool
{
    public class CreateSchoolCommandHandler : IRequestHandler<CreateSchoolCommand, Result<CreateSchoolResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateSchoolCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateSchoolResponseModel>> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
        {
            var isDuplicate = await _unitOfWork.GetRepository<School>().AnyAsync(
                s => s.SchoolName.ToLower() == request.Model.SchoolName.ToLower(), 
                cancellationToken);
            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Tên trường '{request.Model.SchoolName}' đã tồn tại trong hệ thống.");
            }

            var school = new School
            {
                SchoolName = request.Model.SchoolName,
                Address = request.Model.Address
            };

            await _unitOfWork.GetRepository<School>().AddAsync(school);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateSchoolResponseModel
            {
                Id = school.Id,
                SchoolName = school.SchoolName,
                Address = school.Address,
                CreatedTime = school.CreatedTime
            };
        }
    }
}

