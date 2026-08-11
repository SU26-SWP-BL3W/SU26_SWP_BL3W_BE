using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Appeals.Models;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Appeals.Queries.GetAppealsByRound
{
    public class GetAppealsByRoundQuery : BasePaginationQuery, IRequest<Result<PagedResult<AppealModel>>>
    {
        public string RoundId { get; set; } = string.Empty;
    }
}

