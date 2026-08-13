using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Domain.Base;
using SEAL_Application.Features.Demo.Commands.SetupDemoEvents;
using SEAL_Application.Features.Demo.Commands.SetupDemoAppealEvent;
using SEAL_Backend.Filters;
using SEAL_Backend.Helpers;
using System;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller sinh dữ liệu Demo
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AdminAuthorize]
    public class DemoController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public DemoController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo nhanh 2 sự kiện Demo cho ngày cụ thể (vd: 18/07/2026).
        /// Event 1: Nằm trong khoảng thời gian Nộp bài (Submission Phase)
        /// Event 2: Nằm trong khoảng thời gian Chấm điểm (Scoring Phase)
        /// </summary>
        /// <param name="targetDate">Ngày để mốc demo</param>
        [HttpPost("setup-demo-events")]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> SetupDemoEvents([FromQuery] DateTime targetDate)
        {
            var result = await _mediator.Send(new SetupDemoEventsCommand { TargetDate = targetDate });
            return OkResponse(result);
        }

        /// <summary>
        /// Tạo nhanh 1 sự kiện Demo có dữ liệu Phúc khảo (Appeal Phase) cho ngày cụ thể.
        /// Event 3: Nằm trong khoảng thời gian Phúc khảo, đã có bài nộp và có điểm.
        /// </summary>
        /// <param name="targetDate">Ngày để mốc demo</param>
        [HttpPost("setup-demo-appeal-event")]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> SetupDemoAppealEvent([FromQuery] DateTime targetDate)
        {
            var result = await _mediator.Send(new SetupDemoAppealEventCommand { TargetDate = targetDate });
            return OkResponse(result);
        }
    }
}
