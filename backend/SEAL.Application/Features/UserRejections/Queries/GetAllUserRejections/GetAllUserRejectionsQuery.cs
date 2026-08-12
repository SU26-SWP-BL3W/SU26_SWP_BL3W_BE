using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId.Models;
using System;

namespace SEAL_Application.Features.UserRejections.Queries.GetAllUserRejections
{
    public class GetAllUserRejectionsQuery : BasePaginationQuery, IRequest<Result<PagedResult<UserRejectionModel>>>
    {
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public string? RejectedBy { get; set; }
        public string? UserId { get; set; }

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "CreatedTime", "UserId", "RejectedBy" };
    }
}

