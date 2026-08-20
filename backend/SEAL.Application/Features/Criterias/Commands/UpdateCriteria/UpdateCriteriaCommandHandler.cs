using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using SEAL_Application.Features.Criterias.Commands.UpdateCriteria.Models;
using SEAL_Application.Features.Criterias;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Criterias.Commands.UpdateCriteria
{
    public class UpdateCriteriaCommandHandler : IRequestHandler<UpdateCriteriaCommand, Result<UpdateCriteriaResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCriteriaCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UpdateCriteriaResponseModel>> Handle(UpdateCriteriaCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Criteria cần cập nhật
            var criteria = await _unitOfWork.GetRepository<Criteria>().GetByIdAsync(request.Id);
            if (criteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí có ID '{request.Id}' không tồn tại.");
            }

            // 2. Kiểm tra trùng tên tiêu chí (ngoại trừ chính nó)
            var isDuplicate = await _unitOfWork.GetRepository<Criteria>().AnyAsync(
                c => c.CriteriaName.ToLower() == request.Model.CriteriaName.ToLower() && c.Id != request.Id,
                cancellationToken);

            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Tiêu chí '{request.Model.CriteriaName}' đã tồn tại.");
            }

            if (await CriteriaUsageHelper.IsUsedInScoringAsync(_unitOfWork, request.Id, cancellationToken))
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Tiêu chí này đã được dùng để chấm điểm nên không thể thay đổi tên, mô tả hoặc trạng thái.");
            }

            // 3. Cập nhật thông tin Criteria
            criteria.CriteriaName = request.Model.CriteriaName;
            criteria.Description = request.Model.Description;
            criteria.IsActive = request.Model.IsActive;
            criteria.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<Criteria>().UpdateAsync(criteria);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateCriteriaResponseModel
            {
                Id = criteria.Id,
                CriteriaName = criteria.CriteriaName,
                Description = criteria.Description,
                IsActive = criteria.IsActive,
                LastUpdatedTime = criteria.LastUpdatedTime
            };
        }
    }
}

