using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Appeals.Commands.CreateAppeal;
using SEAL_Application.Features.Appeals.Commands.RespondAppeal;
using SEAL_Application.Features.Appeals.Queries.GetAppealsByRound;
using SEAL_Application.Features.Appeals.Queries.GetAppealsByTeam;
using SEAL_Application.Features.Appeals.Queries.GetAssignedAppeals;
using SEAL_Backend.Helpers;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppealsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public AppealsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppeal([FromBody] CreateAppealCommand command)
        {
            var result = await _mediator.Send(command);
            return OkResponse(result);
        }

        [HttpPut("{id}/respond")]
        public async Task<IActionResult> RespondAppeal(string id, [FromBody] RespondAppealCommand command)
        {
            command.AppealId = id;
            var result = await _mediator.Send(command);
            return OkResponse(result);
        }

        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetAppealsByTeam(string teamId, [FromQuery] GetAppealsByTeamQuery query)
        {
            query.TeamId = teamId;
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        [HttpGet("round/{roundId}")]
        public async Task<IActionResult> GetAppealsByRound(string roundId, [FromQuery] GetAppealsByRoundQuery query)
        {
            query.RoundId = roundId;
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
        [HttpGet("assigned/{eventRoleId}")]
        public async Task<IActionResult> GetAssignedAppeals(string eventRoleId)
        {
            var query = new GetAssignedAppealsQuery { EventRoleId = eventRoleId };
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}
