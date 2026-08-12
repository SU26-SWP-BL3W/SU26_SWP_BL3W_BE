using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Criterias.Commands.ToggleCriteriaStatus
{
    public class ToggleCriteriaStatusCommandHandler : IRequestHandler<ToggleCriteriaStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ToggleCriteriaStatusCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(ToggleCriteriaStatusCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Criteria cần toggle
            var criteria = await _unitOfWork.GetRepository<Criteria>().GetByIdAsync(request.Id);
            if (criteria == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Tiêu chí có ID '{request.Id}' không tồn tại.");
            }

            // 2. Toggle trạng thái
            criteria.IsActive = !criteria.IsActive;
            criteria.LastUpdatedTime = CoreHelper.SystemTimeNow;

            // 3. Lưu vào Database
            await _unitOfWork.GetRepository<Criteria>().UpdateAsync(criteria);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return criteria.IsActive;
        }
    }
}

