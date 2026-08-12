using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.Prizes.Commands.CreatePrize;
using SEAL_Application.Features.Prizes.Commands.DeletePrize;
using SEAL_Application.Features.Prizes.Commands.UpdatePrize;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Application.Features.Prizes.Queries.GetPrizesByEventId;
using SEAL_Backend.Filters;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý Giải thưởng (Prize) của từng sự kiện.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PrizesController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public PrizesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách giải thưởng của một sự kiện.
        /// </summary>
        /// <param name="eventId">ID của sự kiện.</param>
        /// <returns>Danh sách các giải thưởng của sự kiện.</returns>
        [HttpGet("~/api/Events/{eventId}/Prizes")]
        [ProducesResponseType(typeof(BaseResponse<List<PrizeModel>>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetByEventId(string eventId)
        {
            var result = await _mediator.Send(new GetPrizesByEventIdQuery { EventId = eventId });
            return OkResponse(result);
        }

        /// <summary>
        /// Tạo giải thưởng mới cho một sự kiện. Chỉ EventCoordinator được phép.
        /// </summary>
        /// <param name="eventId">ID của sự kiện.</param>
        /// <param name="payload">Thông tin giải thưởng cần tạo (tên, giá trị, số lượng).</param>
        /// <returns>Giải thưởng vừa được tạo.</returns>
        [HttpPost("~/api/Events/{eventId}/Prizes")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<PrizeModel>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create(string eventId, [FromBody] CreatePrizeRequestModel payload)
        {
            var result = await _mediator.Send(new CreatePrizeCommand { EventId = eventId, Payload = payload });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin giải thưởng. Chỉ EventCoordinator được phép.
        /// </summary>
        /// <param name="id">ID của giải thưởng cần cập nhật.</param>
        /// <param name="payload">Thông tin cần cập nhật (tên, giá trị, số lượng).</param>
        /// <returns>Giải thưởng sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<PrizeModel>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePrizeRequestModel payload)
        {
            var result = await _mediator.Send(new UpdatePrizeCommand { PrizeId = id, Payload = payload });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một giải thưởng. Chỉ EventCoordinator được phép.
        /// </summary>
        /// <param name="id">ID của giải thưởng cần xóa.</param>
        /// <returns>true nếu xóa thành công.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeletePrizeCommand { PrizeId = id });
            return OkResponse(result);
        }
    }
}

