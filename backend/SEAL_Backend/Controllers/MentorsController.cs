using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Mentors.Commands.InviteMentorToTrack;
using SEAL_Application.Features.Mentors.Commands.InviteMentorToTrack.Models;
using SEAL_Backend.Filters;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using SEAL_Domain.Entity.Enums;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MentorsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public MentorsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Mời một Mentor tham gia hướng dẫn hạng mục (Track) trong một sự kiện.
        /// </summary>
        [HttpPost("invite")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<InviteMentorToTrackResponseModel>), 200)]
        public async Task<IActionResult> Invite([FromBody] InviteMentorToTrackRequestModel requestModel)
        {
            var result = await _mediator.Send(new InviteMentorToTrackCommand { Model = requestModel });
            return OkResponse(result);
        }
    }
}
