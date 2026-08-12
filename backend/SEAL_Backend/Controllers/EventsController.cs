using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Events.Commands.CreateEvent;
using SEAL_Application.Features.Events.Commands.CreateEvent.Models;
using SEAL_Application.Features.Events.Commands.UpdateEvent;
using SEAL_Application.Features.Events.Commands.UpdateEvent.Models;
using SEAL_Application.Features.Events.Commands.DeleteEvent;
using SEAL_Application.Features.Events.Queries.GetEventById;
using SEAL_Application.Features.Events.Queries.GetAllEvents;
using SEAL_Application.Features.Events.Queries.GetUpcomingEvents;
using SEAL_Application.Features.Events.Queries.GetMyEvents;
using SEAL_Application.Features.Events.Queries.GetMyEvents.Models;
using SEAL_Application.Features.Events.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các hoạt động liên quan đến Sự kiện (Event).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public EventsController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        /// Lấy danh sách các sự kiện mà người dùng hiện tại đang tham gia (yêu cầu đăng nhập).
        /// </summary>
        /// <returns>Danh sách các sự kiện của người dùng hiện tại.</returns>
        [Authorize]
        [HttpGet("my-events")]
        [ProducesResponseType(typeof(BaseResponse<List<MyEventResponseModel>>), 200)]
        public async Task<IActionResult> GetMyEvents()
        {
            var result = await _mediator.Send(new GetMyEventsQuery());
            return OkResponse(result);
        }

        /// <summary>
        /// Tạo một sự kiện mới (yêu cầu đăng nhập, người tạo sẽ tự động trở thành EventCoordinator của sự kiện này).
        /// </summary>
        /// <param name="requestModel">Thông tin sự kiện cần tạo.</param>
        /// <returns>Thông tin sự kiện vừa được tạo.</returns>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<CreateEventResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateEventRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateEventCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin sự kiện hiện có.
        /// </summary>
        /// <param name="eventId">ID của sự kiện cần cập nhật.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin sự kiện sau khi cập nhật.</returns>
        [HttpPut("{eventId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<UpdateEventResponseModel>), 200)]
        public async Task<IActionResult> Update(string eventId, [FromBody] UpdateEventRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateEventCommand { Id = eventId, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một sự kiện (Soft Delete).
        /// </summary>
        /// <param name="eventId">ID của sự kiện cần xóa.</param>
        /// <returns>Kết quả thực hiện xóa.</returns>
        [HttpDelete("{eventId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string eventId)
        {
            var result = await _mediator.Send(new DeleteEventCommand(eventId));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết một sự kiện theo ID.
        /// </summary>
        /// <param name="id">ID của sự kiện.</param>
        /// <returns>Thông tin chi tiết của sự kiện.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<EventModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetEventByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách tất cả các sự kiện (có phân trang).
        /// </summary>
        /// <param name="query">Tham số phân trang và lọc.</param>
        /// <returns>Danh sách sự kiện được phân trang.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<EventModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllEventsQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách các sự kiện sắp diễn ra.
        /// </summary>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách sự kiện sắp tới.</returns>
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<EventModel>>), 200)]
        public async Task<IActionResult> GetUpcoming([FromQuery] GetUpcomingEventsQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}
