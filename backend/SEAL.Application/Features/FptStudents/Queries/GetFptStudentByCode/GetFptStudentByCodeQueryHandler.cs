using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.FptStudents.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FptStudents.Queries.GetFptStudentByCode
{
    public class GetFptStudentByCodeQueryHandler : IRequestHandler<GetFptStudentByCodeQuery, Result<FptStudentModel?>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetFptStudentByCodeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<FptStudentModel?>> Handle(GetFptStudentByCodeQuery request, CancellationToken cancellationToken)
        {
            var student = await _unitOfWork.GetRepository<FptStudent>().Entities
                .FirstOrDefaultAsync(s => s.StudentCode.ToLower() == request.StudentCode.ToLower(), cancellationToken);

            // Không tồn tại HOẶC không ở trạng thái ACTIVE (đã tốt nghiệp/bảo lưu...)
            // đều coi là không xác thực được qua hệ thống FPT.
            if (student == null || !string.Equals(student.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return (FptStudentModel?)null;
            }

            return new FptStudentModel
            {
                IsValid = true,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                Email = student.Email,
                Major = student.Major,
                Campus = student.Campus,
                EnrollYear = student.EnrollYear,
                Status = student.Status,
            };
        }
    }
}
