using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Schools.Commands.UpdateSchool.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Schools.Commands.UpdateSchool
{
    public class UpdateSchoolCommandHandler : IRequestHandler<UpdateSchoolCommand, Result<UpdateSchoolResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSchoolCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateSchoolResponseModel>> Handle(UpdateSchoolCommand request, CancellationToken cancellationToken)
        {
            var repository = _unitOfWork.GetRepository<School>();
            var school = await repository.GetByIdAsync(request.Id);
            
            if (school == null)
            {
                return BaseException.BadRequestNotFoundResponse("Không tìm thấy trường học");
            }

            school.SchoolName = request.Model.SchoolName;
            school.Address = request.Model.Address;
            school.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await repository.UpdateAsync(school);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateSchoolResponseModel
            {
                Id = school.Id,
                SchoolName = school.SchoolName,
                Address = school.Address,
                LastUpdatedTime = school.LastUpdatedTime
            };
        }
    }
}

