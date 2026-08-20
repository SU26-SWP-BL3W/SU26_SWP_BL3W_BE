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
using SEAL_Domain.Entity.Enums;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
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

        /// <summary>Danh sách giải theo sự kiện — EC của đúng event hoặc Admin.</summary>
        [HttpGet("~/api/Events/{eventId}/Prizes")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        public async Task<IActionResult> GetByEventId(string eventId)
        {
            var result = await _mediator.Send(new GetPrizesByEventIdQuery { EventId = eventId });
            return OkResponse(result);
        }

        /// <summary>Tạo giải — EC của đúng event hoặc Admin (filter theo route eventId).</summary>
        [HttpPost("~/api/Events/{eventId}/Prizes")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        public async Task<IActionResult> Create(string eventId, [FromBody] CreatePrizeRequestModel payload)
        {
            var result = await _mediator.Send(new CreatePrizeCommand { EventId = eventId, Payload = payload });
            return OkResponse(result);
        }

        /// <summary>Cập nhật giải — quyền kiểm trong handler theo Prize.EventId.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePrizeRequestModel payload)
        {
            var result = await _mediator.Send(new UpdatePrizeCommand { PrizeId = id, Payload = payload });
            return OkResponse(result);
        }

        /// <summary>Xóa giải — quyền kiểm trong handler theo Prize.EventId.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeletePrizeCommand { PrizeId = id });
            return OkResponse(result);
        }
    }
}
