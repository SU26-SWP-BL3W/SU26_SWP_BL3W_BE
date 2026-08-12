using SEAL_Domain.Base;
using System.Collections.Generic;
using MediatR;
using SEAL_Application.Features.Teams.Queries.GetTeamInvitations.Models;

namespace SEAL_Application.Features.Teams.Queries.GetTeamInvitations
{
    /// <summary>
    /// Lấy danh sách lời mời của một đội (cho TeamLeader/EventCoordinator theo dõi
    /// ai đang được mời, ai đã phản hồi). Bổ trợ trạng thái "đang mời" phía người gửi.
    /// </summary>
    public class GetTeamInvitationsQuery : IRequest<Result<List<TeamInvitationListItemModel>>>
    {
        public string TeamId { get; set; } = string.Empty;
    }
}

