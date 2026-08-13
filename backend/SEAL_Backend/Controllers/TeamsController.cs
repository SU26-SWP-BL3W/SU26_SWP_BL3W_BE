// [FLOW3-DOITHI][Controller] TeamsController: toan bo endpoint quan ly doi thi.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.Teams.Commands.CreateTeam;
using SEAL_Application.Features.Teams.Commands.CreateTeam.Models;
using SEAL_Application.Features.Teams.Commands.UpdateTeam;
using SEAL_Application.Features.Teams.Commands.UpdateTeam.Models;
using SEAL_Application.Features.Teams.Commands.DeleteTeam;
using SEAL_Application.Features.Teams.Commands.AddTeamMember;
using SEAL_Application.Features.Teams.Commands.AddTeamMember.Models;
using SEAL_Application.Features.Teams.Commands.RemoveTeamMember;
using SEAL_Application.Features.Teams.Commands.LeaveTeam;
using SEAL_Application.Features.Teams.Commands.InviteTeamMember;
using SEAL_Application.Features.Teams.Commands.InviteTeamMember.Models;
using SEAL_Application.Features.Teams.Commands.RespondTeamInvitation;
using SEAL_Application.Features.Teams.Commands.ConfirmTeamRegistration;
using SEAL_Application.Features.Teams.Commands.ApproveTeamRegistration;
using SEAL_Application.Features.Teams.Commands.RejectTeamRegistration;
using SEAL_Application.Features.Teams.Commands.RejectTeamRegistration.Models;
using SEAL_Application.Features.Teams.Commands.TransferTeamLeader;
using SEAL_Application.Features.Teams.Queries.GetTeamById;
using SEAL_Application.Features.Teams.Queries.GetTeamById.Models;
using SEAL_Application.Commons;
using SEAL_Application.Features.Teams.Queries.GetTeamsList;
using SEAL_Application.Features.Teams.Queries.GetTeamsList.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using SEAL_Application.Features.Teams.Queries.GetMySubmissions;
using SEAL_Application.Features.Teams.Queries.GetMyTeam;
using SEAL_Application.Features.Teams.Queries.GetMyTeam.Models;
using SEAL_Application.Features.Teams.Queries.GetTeamInvitations;
using SEAL_Application.Features.Teams.Queries.GetTeamInvitations.Models;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList.Models;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Nhóm (Team) tham gia cuộc thi.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public TeamsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách các bài nộp giải pháp của chính đội (hoặc các đội) mà người dùng đang tham gia.
        /// </summary>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách bài nộp của đội mình.</returns>
        [HttpGet("my-submissions")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<SubmitResultListItemModel>>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMySubmissions([FromQuery] GetMySubmissionsQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết đội của tôi trong một Sự kiện cụ thể.
        /// </summary>
        /// <param name="eventId">ID của sự kiện.</param>
        /// <returns>Thông tin đội thi kèm danh sách thành viên.</returns>
        [HttpGet("my-team")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<MyTeamResponseModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMyTeam([FromQuery] string eventId)
        {
            var result = await _mediator.Send(new GetMyTeamQuery { EventId = eventId });
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy thông tin lời mời tham gia đội đang chờ phản hồi của người dùng hiện tại (nếu có).
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <returns>Thông tin lời mời hoặc 404 nếu không có lời mời nào.</returns>
        [HttpGet("{teamId}/my-invitation")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<SEAL_Application.Features.Teams.Queries.GetMyTeamInvitation.Models.MyTeamInvitationResponseModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMyTeamInvitation(string teamId)
        {
            var result = await _mediator.Send(new SEAL_Application.Features.Teams.Queries.GetMyTeamInvitation.GetMyTeamInvitationQuery { TeamId = teamId });
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Tạo một nhóm (Team) mới trong một Sự kiện. Người gọi tự động được gán làm TeamLeader.
        /// </summary>
        /// <param name="requestModel">Thông tin nhóm cần tạo (bao gồm EventId).</param>
        /// <returns>Thông tin nhóm vừa tạo.</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<CreateTeamResponseModel>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create([FromBody] CreateTeamRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateTeamCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin nhóm.
        /// </summary>
        /// <param name="id">ID của nhóm.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin nhóm sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<UpdateTeamResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamRequestModel requestModel)
        {
            requestModel.Id = id;
            var result = await _mediator.Send(new UpdateTeamCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một nhóm.
        /// </summary>
        /// <param name="id">ID của nhóm cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteTeamCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin nhóm theo ID. Yêu cầu đăng nhập.
        /// </summary>
        /// <param name="id">ID của nhóm.</param>
        /// <returns>Thông tin nhóm chi tiết.</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<TeamDetailResponseModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetTeamByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách các nhóm (có phân trang). Yêu cầu đăng nhập.
        /// </summary>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách nhóm.</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<TeamListItemModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetTeamsListQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Thêm một thành viên vào nhóm. Chỉ TeamLeader của nhóm hoặc EventCoordinator được phép.
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <param name="requestModel">UserId của thành viên cần thêm.</param>
        /// <returns>Thông tin EventRole vừa tạo.</returns>
        [HttpPost("{teamId}/members")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<AddTeamMemberResponseModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> AddMember(string teamId, [FromBody] AddTeamMemberRequestModel requestModel)
        {
            var result = await _mediator.Send(new AddTeamMemberCommand { TeamId = teamId, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một thành viên khỏi nhóm (kick). Chỉ TeamLeader hoặc EventCoordinator được phép.
        /// Không được xóa chính TeamLeader; muốn xóa nhóm thì dùng Delete Team.
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <param name="userId">ID của thành viên cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{teamId}/members/{userId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RemoveMember(string teamId, string userId)
        {
            var result = await _mediator.Send(new RemoveTeamMemberCommand(teamId, userId));
            return OkResponse(result);
        }

        /// <summary>
        /// Người dùng tự rời khỏi nhóm. TeamLeader không thể tự rời, phải xóa nhóm hoặc transfer leader trước.
        /// Nếu đội đã đăng ký chính thức (Registered) thì không thể tự rời.
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpPost("{teamId}/leave")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> LeaveTeam(string teamId)
        {
            var result = await _mediator.Send(new LeaveTeamCommand(teamId));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách lời mời của một đội (để TeamLeader theo dõi ai đang được mời / đã phản hồi).
        /// Chỉ TeamLeader của đội hoặc EventCoordinator được xem.
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <returns>Danh sách lời mời kèm trạng thái.</returns>
        [HttpGet("{teamId}/invitations")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<List<TeamInvitationListItemModel>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTeamInvitations(string teamId)
        {
            var result = await _mediator.Send(new GetTeamInvitationsQuery { TeamId = teamId });
            return OkResponse(result);
        }

        /// <summary>
        /// TeamLeader gửi lời mời một người dùng vào đội (tạo lời mời chờ chấp nhận, hết hạn sau 24h).
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <param name="requestModel">UserId của người được mời.</param>
        /// <returns>Thông tin lời mời vừa tạo.</returns>
        [HttpPost("{teamId}/invitations")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<InviteTeamMemberResponseModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> InviteMember(string teamId, [FromBody] InviteTeamMemberRequestModel requestModel)
        {
            var result = await _mediator.Send(new InviteTeamMemberCommand { TeamId = teamId, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Người được mời phản hồi lời mời vào đội (chấp nhận hoặc từ chối).
        /// </summary>
        /// <param name="invitationId">ID của lời mời.</param>
        /// <param name="isAccepted">true = chấp nhận, false = từ chối.</param>
        /// <returns>Kết quả phản hồi lời mời.</returns>
        [HttpPost("invitations/{invitationId}/respond")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RespondInvitation(string invitationId, [FromQuery] bool isAccepted)
        {
            var result = await _mediator.Send(new RespondTeamInvitationCommand(invitationId, isAccepted));
            return OkResponse(result);
        }

        /// <summary>
        /// Chuyển quyền Trưởng nhóm cho một thành viên khác trong đội.
        /// Chỉ Trưởng nhóm hiện tại của đội hoặc EventCoordinator được phép. Người nhận phải là thành viên của đội.
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <param name="model">UserId của thành viên nhận quyền Trưởng nhóm.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpPost("{teamId}/transfer-leader")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> TransferLeader(string teamId, [FromBody] TransferTeamLeaderRequestModel model)
        {
            var result = await _mediator.Send(new TransferTeamLeaderCommand
            {
                TeamId = teamId,
                NewLeaderUserId = model.NewLeaderUserId
            });
            return OkResponse(result);
        }

        /// <summary>
        /// TeamLeader xác nhận đăng ký tham gia: kiểm tra đủ 3-5 thành viên đã duyệt rồi khóa đội (Registered).
        /// </summary>
        /// <param name="teamId">ID của nhóm.</param>
        /// <returns>Trạng thái đội sau khi đăng ký.</returns>
        [HttpPost("{teamId}/confirm-registration")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ConfirmRegistration(string teamId)
        {
            var result = await _mediator.Send(new ConfirmTeamRegistrationCommand(teamId));
            return OkResponse(result);
        }

        /// <summary>
        /// EC/Admin DUYỆT đội đã chốt danh sách: PendingApproval -> Registered (chính thức được thi).
        /// </summary>
        /// <param name="teamId">ID của đội.</param>
        [HttpPost("{teamId}/approve-registration")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ApproveRegistration(string teamId)
        {
            var result = await _mediator.Send(new ApproveTeamRegistrationCommand(teamId));
            return OkResponse(result);
        }

        /// <summary>
        /// EC/Admin TỪ CHỐI duyệt đội: PendingApproval -> Forming (mở khóa để đội sửa rồi chốt lại).
        /// Lý do được gửi email cho trưởng nhóm.
        /// </summary>
        /// <param name="teamId">ID của đội.</param>
        /// <param name="model">Lý do từ chối (bắt buộc).</param>
        [HttpPost("{teamId}/reject-registration")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RejectRegistration(string teamId, [FromBody] RejectTeamRegistrationRequestModel model)
        {
            var result = await _mediator.Send(new RejectTeamRegistrationCommand(teamId, model?.Reason ?? string.Empty));
            return OkResponse(result);
        }
    }
}