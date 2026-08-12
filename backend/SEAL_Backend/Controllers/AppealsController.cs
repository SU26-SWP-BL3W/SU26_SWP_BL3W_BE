using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Appeals.Commands.CreateAppeal;
using SEAL_Application.Features.Appeals.Commands.RespondAppeal;
using SEAL_Application.Features.Appeals.Models;
using SEAL_Application.Features.Appeals.Queries.GetAppealsByRound;
using SEAL_Application.Features.Appeals.Queries.GetAppealsByTeam;
using SEAL_Application.Features.Appeals.Queries.GetAssignedAppeals;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý Phúc khảo (Appeal) của các đội thi.
    /// </summary>
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

        /// <summary>
        /// Tạo một yêu cầu phúc khảo mới cho bài nộp của đội.
        /// </summary>
        /// <param name="command">Thông tin phúc khảo (SubmitResultId và lý do).</param>
        /// <returns>true nếu tạo phúc khảo thành công.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateAppeal([FromBody] CreateAppealCommand command)
        {
            var result = await _mediator.Send(command);
            return OkResponse(result);
        }

        /// <summary>
        /// Phản hồi (chấp thuận / từ chối / chuyển giám khảo) một yêu cầu phúc khảo.
        /// Chỉ EventCoordinator hoặc Giám khảo được chỉ định mới có quyền.
        /// </summary>
        /// <param name="id">ID của yêu cầu phúc khảo.</param>
        /// <param name="command">Phản hồi: trạng thái mới, nội dung phản hồi và ID giám khảo (nếu chuyển).</param>
        /// <returns>true nếu phản hồi thành công.</returns>
        [HttpPut("{id}/respond")]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RespondAppeal(string id, [FromBody] RespondAppealCommand command)
        {
            command.AppealId = id;
            var result = await _mediator.Send(command);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách yêu cầu phúc khảo của một đội (phân trang).
        /// </summary>
        /// <param name="teamId">ID của đội thi.</param>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách phúc khảo của đội (phân trang).</returns>
        [HttpGet("team/{teamId}")]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<AppealModel>>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAppealsByTeam(string teamId, [FromQuery] GetAppealsByTeamQuery query)
        {
            query.TeamId = teamId;
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách yêu cầu phúc khảo trong một vòng thi (phân trang).
        /// </summary>
        /// <param name="roundId">ID của vòng thi.</param>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách phúc khảo trong vòng thi (phân trang).</returns>
        [HttpGet("round/{roundId}")]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<AppealModel>>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAppealsByRound(string roundId, [FromQuery] GetAppealsByRoundQuery query)
        {
            query.RoundId = roundId;
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách yêu cầu phúc khảo được giao cho một giám khảo (theo EventRoleId).
        /// </summary>
        /// <param name="eventRoleId">ID của EventRole (giám khảo được chỉ định).</param>
        /// <returns>Danh sách phúc khảo được giao.</returns>
        [HttpGet("assigned/{eventRoleId}")]
        [ProducesResponseType(typeof(BaseResponse<List<AppealModel>>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAssignedAppeals(string eventRoleId)
        {
            var query = new GetAssignedAppealsQuery { EventRoleId = eventRoleId };
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}

