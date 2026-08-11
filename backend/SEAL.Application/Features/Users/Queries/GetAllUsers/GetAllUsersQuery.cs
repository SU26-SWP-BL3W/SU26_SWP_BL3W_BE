using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Users.Models;
using System.Collections.Generic;

namespace SEAL_Application.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersQuery : BasePaginationQuery, IRequest<Result<PagedResult<UserModel>>>
    {
        public bool? IsApproved { get; set; }
        public bool? IsStudent { get; set; }
        public bool? IsFpt { get; set; }
        public string? Search { get; set; }
        public string? EventId { get; set; }

        /// <summary>
        /// true = chỉ lấy user ĐÃ NỘP hồ sơ thí sinh (IsStudent=true + có SchoolId).
        /// Dùng cho màn xét duyệt: loại user chưa từng cập nhật hồ sơ và tài khoản mời (judge/mentor).
        /// (Không lọc theo StudentCode — MSSV chỉ bắt buộc với SV FPT; không lọc theo ảnh thẻ — chỉ bắt buộc với SV ngoài FPT.)
        /// </summary>
        public bool? HasSubmittedProfile { get; set; }

        public override HashSet<string> GetAllowedSortFields() => new(System.StringComparer.OrdinalIgnoreCase) { "FullName", "Email", "CreatedTime" };
    }
}

