using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Notifications.Commands.MarkNotificationRead;
using SEAL_Application.Features.Notifications.Queries.GetMyNotifications;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("my-notifications")]
        [ProducesResponseType(typeof(BaseResponse<List<NotificationItemModel>>), 200)]
        public async Task<IActionResult> GetMine()
        {
            var result = await _mediator.Send(new GetMyNotificationsQuery());
            return OkResponse(result);
        }

        [HttpPut("{notificationId}/read")]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> MarkRead(string notificationId)
        {
            var result = await _mediator.Send(new MarkNotificationReadCommand { NotificationId = notificationId });
            return OkResponse(result);
        }
    }
}
