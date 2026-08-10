using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList.Models;
using System;
using System.Collections.Generic;

namespace SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList
{
    public class GetSubmitResultsListQuery : BasePaginationQuery, IRequest<Result<PagedResult<SubmitResultListItemModel>>>
    {
        /// <summary>Lọc theo sự kiện (suy qua Track -> Round). Thiếu trường này list sẽ trộn bài của MỌI sự kiện.</summary>
        public string? EventId { get; set; }
        public string? TeamId { get; set; }
        public string? RoundId { get; set; }
        public string? TrackId { get; set; }

        public GetSubmitResultsListQuery()
        {
        }

        // Sửa hàm tạo nhận 3 tham số để đồng bộ với API Controller
        public GetSubmitResultsListQuery(string? teamId = null, string? roundId = null, string? trackId = null)
        {
            TeamId = teamId;
            RoundId = roundId;
            TrackId = trackId;
        }

        public override HashSet<string> GetAllowedSortFields() => new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedTime"
        };
    }
}
