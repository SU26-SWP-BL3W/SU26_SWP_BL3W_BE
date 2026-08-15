using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Scores.Commands.CreateScore;
using SEAL_Application.Features.Scores.Commands.CreateScore.Models;
using SEAL_Application.Features.Scores.Commands.UpdateScore;
using SEAL_Application.Features.Scores.Commands.UpdateScore.Models;
using SEAL_Application.Features.Scores.Commands.DeleteScore;
using SEAL_Application.Features.Scores.Commands.SaveScore;
using SEAL_Application.Features.Scores.Commands.SaveScore.Models;
using SEAL_Application.Features.Scores.Queries.GetScoreById;
using SEAL_Application.Features.Scores.Queries.GetScoreDetail;
using SEAL_Application.Features.Scores.Queries.GetScoreDetail.Models;
using SEAL_Application.Features.Scores.Queries.GetScoresByEventRoleId;
using SEAL_Application.Features.Scores.Queries.GetTeamScoreBreakdown;
using SEAL_Application.Features.Scores.Queries.GetTeamScoreBreakdown.Models;
using SEAL_Application.Features.Scores.Queries.GetTrackCalibration;
using SEAL_Application.Features.Scores.Queries.GetTrackCalibration.Models;
using SEAL_Application.Features.Scores.Queries.ExportScoresCsv;
using SEAL_Application.Features.Scores.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Phiếu chấm điểm (Score) của giám khảo.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ScoresController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public ScoresController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một phiếu chấm mới (TotalScore khởi tạo = 0, sẽ được tính lại khi thêm ScoreDetail).
        /// </summary>
        /// <param name="requestModel">Thông tin phiếu chấm cần tạo.</param>
        /// <returns>Thông tin phiếu chấm vừa tạo.</returns>
        /// <summary>
        /// Thí sinh (thành viên đội) xem breakdown điểm theo từng tiêu chí + từng giám khảo cho đội mình.
        /// Chỉ thành viên của đội, EventCoordinator hoặc Admin được xem (kiểm tra trong handler).
        /// </summary>
        /// <param name="teamId">ID của đội.</param>
        /// <returns>Breakdown điểm theo bài nộp → giám khảo → tiêu chí.</returns>
        [HttpGet("team/{teamId}/breakdown")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<TeamScoreBreakdownModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTeamBreakdown(string teamId)
        {
            var result = await _mediator.Send(new GetTeamScoreBreakdownQuery(teamId));
            return OkResponse(result);
        }

        /// <summary>
        /// Ma trận hiệu chuẩn: điểm từng giám khảo × bài nộp của hạng mục (RBL).
        /// Chỉ Admin hoặc Điều phối viên của sự kiện.
        /// </summary>
        [HttpGet("track/{trackId}/calibration")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<TrackCalibrationModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTrackCalibration(string trackId)
        {
            var result = await _mediator.Send(new GetTrackCalibrationQuery(trackId));
            return OkResponse(result);
        }

        /// <summary>
        /// Xuất CSV điểm theo tiêu chí. Mặc định ẩn danh (Team-Tn / Judge-Jn) phục vụ RBL.
        /// Trả file thô (không bọc BaseResponse) để FE tải blob.
        /// </summary>
        [HttpGet("export/{eventId}")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ExportScoresCsv(string eventId, [FromQuery] bool anonymize = true)
        {
            var result = await _mediator.Send(new ExportScoresCsvQuery
            {
                EventId = eventId,
                Anonymize = anonymize
            });
            if (result.IsFailure)
            {
                return OkResponse(result);
            }
            return File(result.Value, "text/csv; charset=utf-8", $"SEAL_Scores_{eventId}.csv");
        }

        /// <summary>
        /// Tạo một phiếu chấm mới (TotalScore khởi tạo = 0, sẽ được tính lại khi thêm ScoreDetail).
        /// </summary>
        /// <param name="requestModel">Thông tin phiếu chấm cần tạo.</param>
        /// <returns>Thông tin phiếu chấm vừa tạo.</returns>
        [HttpPost]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<CreateScoreResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateScoreRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateScoreCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// API GỘP: Lưu (tạo mới hoặc cập nhật) phiếu chấm kèm toàn bộ điểm chi tiết trong 1 lần gọi.
        /// Định danh theo (EventRoleId, SubmitResultId). Danh sách Details là nguồn chuẩn:
        /// tiêu chí mới sẽ thêm, đã có sẽ cập nhật điểm, không còn sẽ bị xóa. TotalScore tự tính lại.
        /// </summary>
        /// <param name="requestModel">Phiếu chấm và danh sách điểm chi tiết theo từng tiêu chí.</param>
        /// <returns>Phiếu chấm sau khi lưu kèm điểm chi tiết.</returns>
        [HttpPost("save")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<SaveScoreResponseModel>), 200)]
        public async Task<IActionResult> Save([FromBody] SaveScoreRequestModel requestModel)
        {
            var result = await _mediator.Send(new SaveScoreCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin phiếu chấm (không cho phép sửa TotalScore — tự tính từ ScoreDetail).
        /// </summary>
        /// <param name="id">ID của phiếu chấm.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin phiếu chấm sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<UpdateScoreResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateScoreRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateScoreCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một phiếu chấm (Cascade xóa toàn bộ ScoreDetail liên quan).
        /// </summary>
        /// <param name="id">ID của phiếu chấm cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteScoreCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết phiếu chấm theo ID.
        /// </summary>
        /// <param name="id">ID của phiếu chấm.</param>
        /// <returns>Thông tin phiếu chấm chi tiết.</returns>
        [HttpGet("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<ScoreModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetScoreByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// API GỘP: Lấy chi tiết phiếu chấm kèm toàn bộ điểm chi tiết theo từng tiêu chí trong 1 lần gọi.
        /// </summary>
        /// <param name="id">ID của phiếu chấm.</param>
        /// <returns>Phiếu chấm kèm danh sách điểm chi tiết (ScoreDetail).</returns>
        [HttpGet("{id}/detail")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<ScoreWithDetailsModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDetail(string id)
        {
            var result = await _mediator.Send(new GetScoreDetailQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách phiếu chấm theo ID giám khảo (EventRole).
        /// </summary>
        /// <param name="eventRoleId">ID của EventRole (giám khảo).</param>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách phiếu chấm của giám khảo (phân trang).</returns>
        [HttpGet("event-role/{eventRoleId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<ScoreModel>>), 200)]
        public async Task<IActionResult> GetByEventRoleId(
            string eventRoleId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? sortBy = null, 
            [FromQuery] bool isAscending = false)
        {
            var query = new GetScoresByEventRoleIdQuery 
            {
                EventRoleId = eventRoleId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                IsAscending = isAscending
            };
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}
