using MediatR;
using SEAL_Application.Features.FptStudents.Models;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.FptStudents.Queries.GetFptStudentByCode
{
    public class GetFptStudentByCodeQuery : IRequest<Result<FptStudentModel?>>
    {
        public string StudentCode { get; set; } = string.Empty;
    }
}
