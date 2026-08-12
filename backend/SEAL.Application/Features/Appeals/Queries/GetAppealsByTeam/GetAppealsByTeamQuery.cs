using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Appeals.Models;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Appeals.Queries.GetAppealsByTeam
{
    public class GetAppealsByTeamQuery : BasePaginationQuery, IRequest<Result<PagedResult<AppealModel>>>
    {
        public string TeamId { get; set; } = string.Empty;
    }
}

