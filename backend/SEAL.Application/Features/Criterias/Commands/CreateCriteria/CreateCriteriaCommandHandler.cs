using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Application.Features.Criterias.Commands.CreateCriteria.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Criterias.Commands.CreateCriteria
{
    public class CreateCriteriaCommandHandler : IRequestHandler<CreateCriteriaCommand, Result<CreateCriteriaResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateCriteriaCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateCriteriaResponseModel>> Handle(CreateCriteriaCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra trùng tên tiêu chí hệ thống
            var isDuplicate = await _unitOfWork.GetRepository<Criteria>().AnyAsync(
                c => c.CriteriaName.ToLower() == request.Model.CriteriaName.ToLower(),
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Tiêu chí '{request.Model.CriteriaName}' đã tồn tại.");
            }

            // 2. Tạo tiêu chí mới
            var criteria = new Criteria
            {
                CriteriaName = request.Model.CriteriaName,
                Description = request.Model.Description,
                IsActive = request.Model.IsActive
            };

            await _unitOfWork.GetRepository<Criteria>().AddAsync(criteria);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateCriteriaResponseModel
            {
                Id = criteria.Id,
                CriteriaName = criteria.CriteriaName,
                Description = criteria.Description,
                IsActive = criteria.IsActive,
                CreatedTime = criteria.CreatedTime
            };
        }
    }
}

