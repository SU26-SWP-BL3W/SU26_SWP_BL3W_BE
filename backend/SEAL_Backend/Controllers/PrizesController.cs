using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.Prizes.Commands.CreatePrize;
using SEAL_Application.Features.Prizes.Commands.DeletePrize;
using SEAL_Application.Features.Prizes.Commands.UpdatePrize;
using SEAL_Application.Features.Prizes.Models;
using SEAL_Application.Features.Prizes.Queries.GetPrizesByEventId;
using SEAL_Backend.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrizesController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public PrizesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("~/api/Events/{eventId}/Prizes")]
        public async Task<IActionResult> GetByEventId(string eventId)
        {
            var result = await _mediator.Send(new GetPrizesByEventIdQuery { EventId = eventId });
            return OkResponse(result);
        }

        [HttpPost("~/api/Events/{eventId}/Prizes")]
        public async Task<IActionResult> Create(string eventId, [FromBody] CreatePrizeRequestModel payload)
        {
            var result = await _mediator.Send(new CreatePrizeCommand { EventId = eventId, Payload = payload });
            return OkResponse(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePrizeRequestModel payload)
        {
            var result = await _mediator.Send(new UpdatePrizeCommand { PrizeId = id, Payload = payload });
            return OkResponse(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeletePrizeCommand { PrizeId = id });
            return OkResponse(result);
        }
    }
}
