using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Appeals.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Appeals.Queries.GetAppealsByTeam
{
    public class GetAppealsByTeamQueryHandler : IRequestHandler<GetAppealsByTeamQuery, Result<PagedResult<AppealModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetAppealsByTeamQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<AppealModel>>> Handle(GetAppealsByTeamQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Vui lòng đăng nhập.");
            }

            var query = _unitOfWork.GetRepository<Appeal>().Entities
                .Where(a => a.TeamId == request.TeamId);

            var pagedAppeals = await query.ToPagedResultAsync(
                request,
                a => new AppealModel
                {
                    Id = a.Id,
                    TeamId = a.TeamId,
                    SubmitResultId = a.SubmitResultId,
                    Reason = a.Reason,
                    Response = a.Response,
                    Status = a.Status,
                    CreatedTime = a.CreatedTime,
                    LastUpdatedTime = a.LastUpdatedTime
                },
                cancellationToken
            );

            return pagedAppeals;
        }
    }
}




