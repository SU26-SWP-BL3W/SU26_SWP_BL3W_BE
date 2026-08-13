using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Tracks.Commands.CreateTrack;
using SEAL_Application.Features.Tracks.Commands.CreateTrack.Models;
using SEAL_Application.Features.Tracks.Commands.UpdateTrack;
using SEAL_Application.Features.Tracks.Commands.UpdateTrack.Models;
using SEAL_Application.Features.Tracks.Commands.DeleteTrack;
using SEAL_Application.Features.Tracks.Commands.AssignTemplateToTrack;
using SEAL_Application.Features.Tracks.Commands.AssignTemplateToTrack.Models;
using SEAL_Application.Features.Tracks.Queries.GetTrackById;
using SEAL_Application.Features.Tracks.Queries.GetTracksByEventId;
using SEAL_Application.Features.Tracks.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Hạng mục (Track) trong các Vòng thi.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TracksController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public TracksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một hạng mục (Track) mới.
        /// </summary>
        /// <param name="requestModel">Thông tin hạng mục cần tạo.</param>
        /// <returns>Thông tin hạng mục vừa tạo.</returns>
        [HttpPost]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<CreateTrackResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateTrackRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateTrackCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin hạng mục.
        /// </summary>
        /// <param name="id">ID của hạng mục.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin hạng mục sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<UpdateTrackResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTrackRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateTrackCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một hạng mục (xóa vĩnh viễn).
        /// </summary>
        /// <param name="id">ID của hạng mục cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteTrackCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Gán mẫu tiêu chí (Template) cho hạng mục.
        /// </summary>
        /// <param name="id">ID của hạng mục.</param>
        /// <param name="requestModel">Thông tin ID mẫu tiêu chí cần gán.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpPatch("{id}/assign-template")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> AssignTemplate(string id, [FromBody] AssignTemplateToTrackRequestModel requestModel)
        {
            var result = await _mediator.Send(new AssignTemplateToTrackCommand { TrackId = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết hạng mục theo ID.
        /// </summary>
        /// <param name="id">ID của hạng mục.</param>
        /// <returns>Thông tin hạng mục chi tiết.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<TrackModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetTrackByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách hạng mục theo ID sự kiện.
        /// </summary>
        /// <param name="query">Tham số phân trang và EventId.</param>
        /// <returns>Danh sách hạng mục thuộc sự kiện đó.</returns>
        [HttpGet("event")]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<TrackModel>>), 200)]
        public async Task<IActionResult> GetByEventId([FromQuery] GetTracksByEventIdQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}
