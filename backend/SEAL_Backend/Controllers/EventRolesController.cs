using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.EventRoles.Commands.AssignEventRole;
using SEAL_Application.Features.EventRoles.Commands.AssignEventRole.Models;
using SEAL_Application.Features.EventRoles.Commands.UpdateEventRole;
using SEAL_Application.Features.EventRoles.Commands.UpdateEventRole.Models;
using SEAL_Application.Features.EventRoles.Commands.RemoveEventRole;
using SEAL_Application.Features.EventRoles.Commands.InviteEventRole;
using SEAL_Application.Features.EventRoles.Commands.InviteEventRole.Models;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation.Models;
using SEAL_Application.Features.EventRoles.Commands.DeclineEventRoleInvitation;
using SEAL_Application.Features.EventRoles.Queries.GetEventRolesByEventId;
using SEAL_Application.Features.EventRoles.Queries.GetEventRolesByUserId;
using SEAL_Application.Features.EventRoles.Queries.GetUsersByRoleInEvent;
using SEAL_Application.Features.EventRoles.Queries.CheckUserHasRoleInEvent;
using SEAL_Application.Features.EventRoles.Queries.GetUserRoleInEvent;
using SEAL_Application.Features.EventRoles.Models;
using SEAL_Application.Features.Users.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;


namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Vai trò của Người dùng trong Sự kiện (Event Role).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Http.Tags("3. Personnel & Roles")]
    public class EventRolesController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public EventRolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gán vai trò cho người dùng trong một sự kiện.
        /// </summary>
        /// <param name="requestModel">Thông tin vai trò cần gán (UserId, EventId, RoleType...).</param>
        /// <returns>Thông tin vai trò vừa gán.</returns>
        [HttpPost("assign")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<AssignEventRoleResponseModel>), 200)]
        public async Task<IActionResult> Assign([FromBody] AssignEventRoleRequestModel requestModel)
        {
            var result = await _mediator.Send(new AssignEventRoleCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Gửi lời mời một người dùng đảm nhận vai trò sự kiện (Judge, Mentor, EventCoordinator).
        /// Chỉ Admin hoặc EventCoordinator của sự kiện được phép. Lời mời ở trạng thái Pending, mặc định hết hạn sau 7 ngày.
        /// </summary>
        /// <param name="requestModel">Thông tin lời mời (EventId, InvitedUserId, RoleName, TrackId tùy chọn...).</param>
        /// <returns>Thông tin lời mời vừa tạo.</returns>
        [HttpPost("invitations")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<InviteEventRoleResponseModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> InviteEventRole([FromBody] InviteEventRoleRequestModel requestModel)
        {
            var result = await _mediator.Send(new InviteEventRoleCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Người được mời phản hồi lời mời vai trò sự kiện (chấp nhận hoặc từ chối).
        /// Nếu chấp nhận sẽ tạo bản ghi EventRole chính thức.
        /// </summary>
        /// <param name="invitationId">ID của lời mời.</param>
        /// <param name="requestModel">isAccepted = true để chấp nhận, false để từ chối.</param>
        /// <returns>Kết quả phản hồi lời mời.</returns>
        [HttpPost("invitations/{invitationId}/respond")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<RespondEventRoleInvitationResponseModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RespondEventRoleInvitation(string invitationId, [FromBody] RespondEventRoleInvitationRequestModel requestModel)
        {
            var result = await _mediator.Send(new RespondEventRoleInvitationCommand(invitationId, requestModel.IsAccepted));
            return OkResponse(result);
        }

        /// <summary>
        /// Từ chối lời mời vai trò qua LINK EMAIL — KHÔNG cần đăng nhập (chỉ cần ID lời mời).
        /// An toàn: chỉ đánh dấu Rejected, không tạo vai trò nào. (Chấp nhận vẫn dùng /respond và phải đăng nhập.)
        /// </summary>
        [HttpPost("invitations/{invitationId}/decline")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<RespondEventRoleInvitationResponseModel>), 200)]
        public async Task<IActionResult> DeclineEventRoleInvitationPublic(string invitationId)
        {
            var result = await _mediator.Send(new DeclineEventRoleInvitationCommand(invitationId));
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin vai trò của người dùng trong sự kiện.
        /// </summary>
        /// <param name="id">ID của bản ghi vai trò.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<UpdateEventRoleResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateEventRoleRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateEventRoleCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Thu hồi/Xóa vai trò của người dùng trong sự kiện.
        /// </summary>
        /// <param name="id">ID của bản ghi vai trò cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Remove(string id)
        {
            var result = await _mediator.Send(new RemoveEventRoleCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách các vai trò trong một sự kiện cụ thể.
        /// </summary>
        /// <param name="query">Tham số phân trang và EventId.</param>
        /// <returns>Danh sách vai trò trong sự kiện.</returns>
        [HttpGet("event")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<EventRoleModel>>), 200)]
        public async Task<IActionResult> GetByEventId([FromQuery] GetEventRolesByEventIdQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách các vai trò mà một người dùng đang đảm nhận.
        /// </summary>
        /// <param name="query">Tham số phân trang và UserId.</param>
        /// <returns>Danh sách các vai trò của người dùng.</returns>
        [HttpGet("user")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<EventRoleModel>>), 200)]
        public async Task<IActionResult> GetByUserId([FromQuery] GetEventRolesByUserIdQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách người dùng theo một vai trò cụ thể trong sự kiện.
        /// </summary>
        /// <param name="query">Tham số phân trang, EventId và RoleName.</param>
        /// <returns>Danh sách người dùng có vai trò đó.</returns>
        [HttpGet("event/role")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<UserModel>>), 200)]
        public async Task<IActionResult> GetUsersByRoleInEvent([FromQuery] GetUsersByRoleInEventQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Kiểm tra xem người dùng có vai trò cụ thể trong sự kiện hay không.
        /// </summary>
        /// <param name="query">Thông tin UserId, EventId và Role cần kiểm tra.</param>
        /// <returns>True nếu có vai trò, ngược lại False.</returns>
        [HttpGet("check")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> CheckUserHasRoleInEvent([FromQuery] CheckUserHasRoleInEventQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy vai trò cụ thể của người dùng trong một sự kiện.
        /// </summary>
        [HttpGet("user-role")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<EventRoleModel>), 200)]
        public async Task<IActionResult> GetUserRoleInEvent([FromQuery] GetUserRoleInEventQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách vai trò (EventRoleType) dưới dạng Key-Value.
        /// </summary>
        /// <returns>Danh sách các vai trò với Key là giá trị số và Value là tên vai trò.</returns>
        [HttpGet("types")]
        [ProducesResponseType(typeof(BaseResponse<object>), 200)]
        public IActionResult GetEventRoleTypes()
        {
            var roles = Enum.GetValues(typeof(EventRoleType))
                            .Cast<EventRoleType>()
                            .Select(r => new
                            {
                                Key = (int)r,
                                Value = r.ToString()
                            })
                            .ToList();

            return OkResponse(roles);
        }
    }
}
