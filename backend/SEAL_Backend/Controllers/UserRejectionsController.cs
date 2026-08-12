using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.UserRejections.Commands.CreateUserRejection;
using SEAL_Application.Features.UserRejections.Commands.CreateUserRejection.Models;
using SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId;
using SEAL_Application.Features.UserRejections.Queries.GetUserRejectionsByUserId.Models;
using SEAL_Application.Features.UserRejections.Queries.GetAllUserRejections;
using SEAL_Application.Features.UserRejections.Commands.DeleteUserRejection;
using SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection;
using SEAL_Application.Features.UserRejections.Commands.UpdateUserRejection.Models;
using SEAL_Application.Commons;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý lịch sử từ chối người dùng (User Rejection).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserRejectionsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public UserRejectionsController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// Tạo mới một bản ghi từ chối người dùng.
        /// </summary>
        /// <param name="requestModel">Thông tin lý do từ chối.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateUserRejectionRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateUserRejectionCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy tất cả lịch sử từ chối (có phân trang).
        /// </summary>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách lịch sử từ chối.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<UserRejectionModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllUserRejectionsQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy lịch sử từ chối của một người dùng cụ thể.
        /// </summary>
        /// <param name="userId">ID của người dùng.</param>
        /// <returns>Danh sách các bản ghi từ chối của người dùng đó.</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<UserRejectionModel>>), 200)]
        public async Task<IActionResult> GetByUserId(string userId)
        {
            var result = await _mediator.Send(new GetUserRejectionsByUserIdQuery(userId));
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin bản ghi từ chối.
        /// </summary>
        /// <param name="id">ID của bản ghi từ chối.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BaseResponse<UpdateUserRejectionResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRejectionRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateUserRejectionCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa bản ghi từ chối (Soft Delete).
        /// </summary>
        /// <param name="id">ID của bản ghi cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteUserRejectionCommand(id));
            return OkResponse(result);
        }
    }
}
