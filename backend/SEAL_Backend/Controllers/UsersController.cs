using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.Users.Commands.CreateUser;
using SEAL_Application.Features.Users.Commands.CreateUser.Models;
using SEAL_Application.Features.Users.Commands.DeleteUser;
using SEAL_Application.Features.Users.Commands.UpdateUser;
using SEAL_Application.Features.Users.Commands.UpdateUser.Models;
using SEAL_Application.Features.Users.Models;
using SEAL_Application.Features.Users.Queries.GetAllUsers;
using SEAL_Application.Features.Users.Queries.GetUserById;
using SEAL_Application.Features.Users.Queries.GetUserProfile;
using SEAL_Application.Features.Users.Queries.GetMyInvitations;
using SEAL_Application.Features.Users.Queries.GetMyInvitations.Models;
using SEAL_Application.Features.Users.Commands.ApproveUser;
using SEAL_Application.Features.Users.Commands.RejectUser;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Base;
using SEAL_Application.Commons;

using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý người dùng (User).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Microsoft.AspNetCore.Http.Tags("1. Auth & Users")]
    public class UsersController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// Lấy thông tin cá nhân của người dùng hiện tại đang đăng nhập.
        /// </summary>
        /// <returns>Thông tin người dùng hiện tại.</returns>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(BaseResponse<UserModel>), 200)]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _mediator.Send(new GetUserProfileQuery());
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy tổng hợp tất cả lời mời đang chờ (vào đội và vai trò sự kiện) của người dùng hiện tại.
        /// Dùng cho chuông thông báo lời mời ở Frontend.
        /// </summary>
        /// <returns>Tổng số lời mời đang chờ và danh sách chi tiết.</returns>
        [HttpGet("my-invitations", Order = 1)]
        [ProducesResponseType(typeof(BaseResponse<MyInvitationsResponseModel>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMyInvitations()
        {
            var result = await _mediator.Send(new GetMyInvitationsQuery());
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách tất cả người dùng (có phân trang). Admin và Event Coordinator được phép.
        /// </summary>
        /// <param name="query">Tham số phân trang và lọc.</param>
        /// <returns>Danh sách người dùng được phân trang.</returns>
        [HttpGet(Order = 2)]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<UserModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllUsersQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết người dùng theo ID. Bản thân người dùng, Admin và Event Coordinator được phép.
        /// </summary>
        /// <param name="id">ID của người dùng.</param>
        /// <returns>Thông tin người dùng.</returns>
        [HttpGet("{id}", Order = 3)]
        [ProducesResponseType(typeof(BaseResponse<UserModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetUserByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Tạo một người dùng mới. Chỉ Admin hệ thống mới được phép.
        /// </summary>
        /// <param name="requestModel">Thông tin người dùng cần tạo.</param>
        /// <returns>Thông tin người dùng vừa tạo.</returns>
        [HttpPost]
        [AdminAuthorize]
        [ProducesResponseType(typeof(BaseResponse<CreateUserResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateUserCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin người dùng. Chỉ Admin hệ thống mới được phép.
        /// </summary>
        /// <param name="id">ID người dùng cần cập nhật.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [AdminAuthorize]
        [ProducesResponseType(typeof(BaseResponse<UpdateUserResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateUserCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa người dùng (xóa vĩnh viễn). Chỉ Admin hệ thống mới được phép.
        /// </summary>
        /// <param name="id">ID người dùng cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [AdminAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteUserCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Duyệt hồ sơ sinh viên (Admin / Event Coordinator).
        /// </summary>
        /// <param name="id">ID người dùng cần duyệt.</param>
        /// <returns>Thông tin người dùng sau khi duyệt.</returns>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(typeof(BaseResponse<UserModel>), 200)]
        public async Task<IActionResult> Approve(string id)
        {
            var result = await _mediator.Send(new ApproveUserCommand { Id = id });
            return OkResponse(result);
        }

        /// <summary>
        /// Từ chối hồ sơ sinh viên kèm lý do (Admin / Event Coordinator).
        /// </summary>
        /// <param name="id">ID người dùng bị từ chối.</param>
        /// <param name="model">Lý do từ chối.</param>
        /// <returns>Thông tin người dùng sau khi bị từ chối.</returns>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(typeof(BaseResponse<UserModel>), 200)]
        public async Task<IActionResult> Reject(string id, [FromBody] SEAL_Application.Features.Users.Commands.RejectUser.Models.RejectUserBodyModel model)
        {
            var result = await _mediator.Send(new RejectUserCommand { Id = id, Reason = model.Reason });
            return OkResponse(result);
        }
    }
}
