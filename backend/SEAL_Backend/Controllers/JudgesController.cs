using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack;
using SEAL_Application.Features.Judges.Commands.InviteJudgeToTrack.Models;
using SEAL_Backend.Filters;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý giám khảo (Judge) của sự kiện: mời + cấp tài khoản tạm chấm theo hạng mục.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class JudgesController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public JudgesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Mời 1 giám khảo chấm đúng MỘT hạng mục (Track). Nếu email chưa có tài khoản sẽ tạo tài khoản tạm
        /// chỉ dùng được trong vòng đời sự kiện, và gửi email mời kèm link xác thực.
        /// </summary>
        /// <param name="requestModel">Thông tin mời: EventId, TrackId, email và họ tên giám khảo.</param>
        /// <returns>Thông tin lời mời và vai trò chấm vừa gán.</returns>
        [HttpPost("invite")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<InviteJudgeToTrackResponseModel>), 200)]
        public async Task<IActionResult> Invite([FromBody] InviteJudgeToTrackRequestModel requestModel)
        {
            var result = await _mediator.Send(new InviteJudgeToTrackCommand { Model = requestModel });
            return OkResponse(result);
        }
    }
}
