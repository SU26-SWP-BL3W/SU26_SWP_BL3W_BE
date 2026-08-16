using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Rounds.Commands.CreateRound;
using SEAL_Application.Features.Rounds.Commands.CreateRound.Models;
using SEAL_Application.Features.Rounds.Commands.UpdateRound;
using SEAL_Application.Features.Rounds.Commands.UpdateRound.Models;
using SEAL_Application.Features.Rounds.Commands.DeleteRound;
using SEAL_Application.Features.Rounds.Queries.GetRoundById;
using SEAL_Application.Features.Rounds.Queries.GetRoundsByEventId;
using SEAL_Application.Features.Rounds.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Vòng thi (Round) của Sự kiện.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Http.Tags("2. Competitions & Structure")]
    public class RoundsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public RoundsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một vòng thi mới.
        /// </summary>
        /// <param name="requestModel">Thông tin vòng thi cần tạo.</param>
        /// <returns>Thông tin vòng thi vừa tạo.</returns>
        [HttpPost]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<CreateRoundResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateRoundRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateRoundCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin vòng thi.
        /// </summary>
        /// <param name="id">ID của vòng thi.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin vòng thi sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<UpdateRoundResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRoundRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateRoundCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một vòng thi (xóa vĩnh viễn).
        /// </summary>
        /// <param name="id">ID của vòng thi cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteRoundCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết vòng thi theo ID.
        /// </summary>
        /// <param name="id">ID của vòng thi.</param>
        /// <returns>Thông tin vòng thi chi tiết.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<RoundModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetRoundByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách các vòng thi theo ID sự kiện.
        /// </summary>
        /// <param name="query">Tham số phân trang và EventId.</param>
        /// <returns>Danh sách các vòng thi thuộc sự kiện.</returns>
        [HttpGet("event")]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<RoundModel>>), 200)]
        public async Task<IActionResult> GetByEventId([FromQuery] GetRoundsByEventIdQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}
