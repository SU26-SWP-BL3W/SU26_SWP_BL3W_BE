using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail;
using SEAL_Application.Features.ScoreDetails.Commands.CreateScoreDetail.Models;
using SEAL_Application.Features.ScoreDetails.Commands.UpdateScoreDetail;
using SEAL_Application.Features.ScoreDetails.Commands.UpdateScoreDetail.Models;
using SEAL_Application.Features.ScoreDetails.Commands.DeleteScoreDetail;
using SEAL_Application.Features.ScoreDetails.Queries.GetScoreDetailById;
using SEAL_Application.Features.ScoreDetails.Queries.GetScoreDetailsByScoreId;
using SEAL_Application.Features.ScoreDetails.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Điểm chi tiết (ScoreDetail) của từng tiêu chí trong phiếu chấm.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ScoreDetailsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public ScoreDetailsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một điểm chi tiết cho 1 tiêu chí trong phiếu chấm (tự cập nhật TotalScore của Score cha).
        /// </summary>
        /// <param name="requestModel">Thông tin điểm chi tiết cần tạo.</param>
        /// <returns>Thông tin điểm chi tiết vừa tạo + tổng điểm mới của phiếu chấm.</returns>
        [HttpPost]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<CreateScoreDetailResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateScoreDetailRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateScoreDetailCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật giá trị điểm chi tiết (tự cập nhật TotalScore của Score cha).
        /// </summary>
        /// <param name="id">ID của điểm chi tiết.</param>
        /// <param name="requestModel">Giá trị điểm mới.</param>
        /// <returns>Thông tin điểm chi tiết sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<UpdateScoreDetailResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateScoreDetailRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateScoreDetailCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một điểm chi tiết (tự cập nhật TotalScore của Score cha).
        /// </summary>
        /// <param name="id">ID của điểm chi tiết cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteScoreDetailCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết điểm theo ID.
        /// </summary>
        /// <param name="id">ID của điểm chi tiết.</param>
        /// <returns>Thông tin điểm chi tiết.</returns>
        [HttpGet("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<ScoreDetailModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetScoreDetailByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách điểm chi tiết theo ID phiếu chấm.
        /// </summary>
        /// <param name="scoreId">ID của phiếu chấm (Score).</param>
        /// <param name="pageNumber">Trang.</param>
        /// <param name="pageSize">Số dòng mỗi trang.</param>
        /// <param name="sortBy">Trường sắp xếp.</param>
        /// <param name="isAscending">true = tăng dần.</param>
        /// <returns>Danh sách điểm chi tiết của phiếu chấm (phân trang).</returns>
        [HttpGet("score/{scoreId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge)]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<ScoreDetailModel>>), 200)]
        public async Task<IActionResult> GetByScoreId(
            string scoreId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? sortBy = null, 
            [FromQuery] bool isAscending = false)
        {
            var query = new GetScoreDetailsByScoreIdQuery 
            {
                ScoreId = scoreId,
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
