using System.Collections.Generic;

namespace SEAL_Application.Features.Users.Queries.GetMyInvitations.Models
{
    public class MyInvitationsResponseModel
    {
        /// <summary>Tổng số lời mời đang chờ phản hồi.</summary>
        public int TotalPending { get; set; }

        public List<InvitationItemModel> Invitations { get; set; } = new List<InvitationItemModel>();
    }
}
