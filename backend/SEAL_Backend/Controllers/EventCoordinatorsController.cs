using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator;
using SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator.Models;
using SEAL_Domain.Entity.Enums;
using SEAL_Backend.Filters;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý Event Coordinator (Điều phối viên sự kiện).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EventCoordinatorsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public EventCoordinatorsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Mời một người làm Event Coordinator cho sự kiện.
        /// Quyền: Admin hoặc EventCoordinator của sự kiện.
        /// </summary>
        [HttpPost("invite")]
        [Authorize]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<InviteEventCoordinatorResponseModel>), 200)]
        public async Task<IActionResult> InviteEventCoordinator([FromBody] InviteEventCoordinatorRequestModel requestModel)
        {
            var result = await _mediator.Send(new InviteEventCoordinatorCommand { Model = requestModel });
            return OkResponse(result);
        }
    }
}
