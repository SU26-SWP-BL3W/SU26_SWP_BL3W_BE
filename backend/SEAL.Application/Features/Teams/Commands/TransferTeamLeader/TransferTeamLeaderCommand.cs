using SEAL_Domain.Base;
using MediatR;

namespace SEAL_Application.Features.Teams.Commands.TransferTeamLeader
{
    /// <summary>Body của yêu cầu chuyển quyền Trưởng nhóm.</summary>
    public class TransferTeamLeaderRequestModel
    {
        /// <summary>UserId của thành viên sẽ nhận quyền TeamLeader.</summary>
        public string NewLeaderUserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chuyển quyền TeamLeader cho một thành viên khác trong đội (leader hiện tại
    /// tự chuyển hoặc EventCoordinator can thiệp). Không đổi roster, chỉ hoán vai trò.
    /// </summary>
    public class TransferTeamLeaderCommand : IRequest<Result<bool>>
    {
        public string TeamId { get; set; } = string.Empty;
        public string NewLeaderUserId { get; set; } = string.Empty;
    }
}

