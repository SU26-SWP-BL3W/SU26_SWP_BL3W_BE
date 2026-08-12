using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Appeals.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Appeals.Queries.GetAssignedAppeals
{
    public class GetAssignedAppealsQueryHandler : IRequestHandler<GetAssignedAppealsQuery, Result<List<AppealModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAssignedAppealsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<AppealModel>>> Handle(GetAssignedAppealsQuery request, CancellationToken cancellationToken)
        {
            var appeals = await _unitOfWork.GetRepository<Appeal>().Entities
                .AsNoTracking()
                .Where(a => a.Status == AppealStatus.Approved && a.AssignedJudgeId == request.EventRoleId)
                .OrderByDescending(a => a.LastUpdatedTime)
                .Select(a => new AppealModel
                {
                    Id = a.Id,
                    TeamId = a.TeamId,
                    SubmitResultId = a.SubmitResultId,
                    Reason = a.Reason,
                    Response = a.Response,
                    Status = a.Status,
                    AssignedJudgeId = a.AssignedJudgeId,
                    CreatedTime = a.CreatedTime,
                    LastUpdatedTime = a.LastUpdatedTime
                })
                .ToListAsync(cancellationToken);

            return appeals;
        }
    }
}



