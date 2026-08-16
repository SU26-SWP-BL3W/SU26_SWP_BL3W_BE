using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.FinalResults.Commands.CreateFinalResult;
using SEAL_Application.Features.FinalResults.Commands.CreateFinalResult.Models;
using SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult;
using SEAL_Application.Features.FinalResults.Commands.UpdateFinalResult.Models;
using SEAL_Application.Features.FinalResults.Commands.DeleteFinalResult;
using SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults;
using SEAL_Application.Features.FinalResults.Commands.PublishRoundResults;
using SEAL_Application.Features.FinalResults.Commands.SetRoundResultsPublishStatus;
using SEAL_Application.Features.FinalResults.Commands.SetRoundResultsPublishStatus.Models;
using SEAL_Application.Features.FinalResults.Commands.UnpublishRoundResults;
using SEAL_Application.Features.FinalResults.Commands.AssignPrize;
using SEAL_Application.Features.FinalResults.Queries.GetFinalResultById;
using SEAL_Application.Features.FinalResults.Queries.GetFinalResultsByRoundId;
using SEAL_Application.Features.FinalResults.Queries.GetFinalResultsByTeamId;
using SEAL_Application.Features.FinalResults.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý Kết quả chung cuộc (FinalResult) của các đội thi trong từng vòng thi.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Http.Tags("5. Scoring & Evaluation")]
    public class FinalResultsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public FinalResultsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo mới kết quả chung cuộc cho 1 đội trong 1 vòng thi (nhập thủ công).
        /// </summary>
        /// <param name="requestModel">Thông tin kết quả cần tạo.</param>
        /// <returns>Thông tin kết quả vừa tạo.</returns>
        [HttpPost]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<CreateFinalResultResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateFinalResultRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateFinalResultCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Tự động tính kết quả chung cuộc cho cả vòng thi: tính FinalScore (trung bình điểm giám khảo),
        /// xếp Rank theo điểm, và đánh dấu IsAdvanced cho top N đội. Chỉ EventCoordinator/Admin được gọi.
        /// LƯU Ý: kết quả tính ra ở trạng thái NHÁP (IsPublished=false) — chỉ EC/Admin xem để rà soát.
        /// Muốn công khai bảng xếp hạng phải gọi thêm POST publish/{roundId}.
        /// </summary>
        /// <remarks>
        /// Handler sẽ kiểm tra lại quyền dựa trên Event của Round.
        /// </remarks>
        /// <param name="roundId">ID của vòng thi cần tính.</param>
        /// <param name="topN">Số đội đầu bảng được thăng vòng.</param>
        /// <returns>Bảng xếp hạng vừa tính (đã lưu vào FinalResult ở trạng thái nháp).</returns>
        [HttpPost("calculate/{roundId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<System.Collections.Generic.List<SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models.CalculateRoundResultItemModel>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CalculateRoundResults(string roundId, [FromQuery] int topN)
        {
            var result = await _mediator.Send(new CalculateRoundResultsCommand(roundId, topN));
            return OkResponse(result);
        }

        /// <summary>
        /// CÔNG BỐ kết quả cả vòng thi: chuyển toàn bộ FinalResult của vòng từ NHÁP sang công khai
        /// (IsPublished=true) để mọi người xem được bảng xếp hạng. Gọi sau khi đã tính và rà soát.
        /// Chỉ EventCoordinator/Admin.
        /// </summary>
        /// <param name="roundId">ID của vòng thi cần công bố.</param>
        /// <returns>true nếu công bố thành công.</returns>
        [HttpPost("publish/{roundId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PublishRoundResults(string roundId)
        {
            var result = await _mediator.Send(new PublishRoundResultsCommand(roundId));
            return OkResponse(result);
        }

        /// <summary>
        /// ĐẶT TRẠNG THÁI CÔNG BỐ kết quả cả vòng thi — chuyển được CẢ HAI CHIỀU:
        /// body {"isPublished": true} = công bố (mọi người xem được bảng xếp hạng);
        /// body {"isPublished": false} = thu hồi về nháp (chỉ EC/Admin xem, ẩn khỏi người ngoài).
        /// GIỮ NGUYÊN Rank/FinalScore/IsAdvanced đã tính ở cả hai chiều, đảo qua lại thoải mái.
        /// KHÁC với DELETE round/{roundId} (xóa sạch kết quả, phải Calculate lại từ đầu).
        /// Idempotent. Chỉ EventCoordinator/Admin.
        /// </summary>
        /// <param name="roundId">ID của vòng thi.</param>
        /// <param name="requestModel">Trạng thái công bố mong muốn.</param>
        /// <returns>true nếu đổi trạng thái thành công.</returns>
        [HttpPut("round/{roundId}/publish-status")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SetRoundResultsPublishStatus(
            string roundId,
            [FromBody] SetRoundResultsPublishStatusRequestModel requestModel)
        {
            var result = await _mediator.Send(
                new SetRoundResultsPublishStatusCommand(roundId, requestModel.IsPublished));
            return OkResponse(result);
        }

        /// <summary>
        /// HỦY CÔNG BỐ kết quả cả vòng thi: xóa trọn bộ FinalResult của vòng để mở lại khóa
        /// chấm/sửa điểm (xử lý sai sót sau công bố), sau đó tính lại bằng calculate.
        /// Chỉ hủy được khi vòng sau chưa có bài nộp/kết quả. Chỉ EventCoordinator/Admin.
        /// </summary>
        /// <param name="roundId">ID của vòng thi cần hủy công bố.</param>
        /// <returns>true nếu hủy thành công.</returns>
        [HttpDelete("round/{roundId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> UnpublishRoundResults(string roundId)
        {
            var result = await _mediator.Send(new UnpublishRoundResultsCommand(roundId));
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật kết quả chung cuộc.
        /// </summary>
        /// <param name="id">ID của kết quả.</param>
        /// <param name="requestModel">Thông tin cập nhật.</param>
        /// <returns>Thông tin kết quả sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<UpdateFinalResultResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinalResultRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateFinalResultCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một kết quả chung cuộc.
        /// </summary>
        /// <param name="id">ID của kết quả cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteFinalResultCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Gán hoặc gỡ giải thưởng cho kết quả chung cuộc.
        /// </summary>
        /// <param name="id">ID của kết quả chung cuộc (FinalResult).</param>
        /// <param name="requestModel">Payload chứa PrizeId (nếu rỗng/null thì là gỡ giải thưởng).</param>
        /// <returns>Thành công true/false.</returns>
        [HttpPatch("{id}/assign-prize")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> AssignPrize(string id, [FromBody] AssignPrizeRequestModel requestModel)
        {
            var result = await _mediator.Send(new AssignPrizeCommand { FinalResultId = id, Payload = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết kết quả chung cuộc theo ID.
        /// </summary>
        /// <param name="id">ID của kết quả.</param>
        /// <returns>Thông tin chi tiết kết quả.</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<FinalResultModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetFinalResultByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy bảng xếp hạng các đội trong 1 vòng thi (mặc định sắp theo Rank tăng dần).
        /// EventCoordinator/Admin xem được cả kết quả NHÁP; người khác chỉ thấy kết quả ĐÃ công bố.
        /// </summary>
        /// <param name="roundId">ID của vòng thi.</param>
        /// <param name="pageNumber">Trang.</param>
        /// <param name="pageSize">Số dòng mỗi trang.</param>
        /// <param name="sortBy">Trường sắp xếp.</param>
        /// <param name="isAscending">true = tăng dần.</param>
        /// <param name="trackId">Lọc theo hạng mục (optional).</param>
        /// <returns>Bảng xếp hạng các đội (phân trang).</returns>
        [HttpGet("round/{roundId}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<FinalResultModel>>), 200)]
        public async Task<IActionResult> GetByRoundId(
            string roundId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? sortBy = null, 
            [FromQuery] bool isAscending = false,
            [FromQuery] string? trackId = null)
        {
            var query = new GetFinalResultsByRoundIdQuery 
            {
                RoundId = roundId,
                TrackId = trackId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                IsAscending = isAscending
            };
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy lịch sử kết quả chung cuộc của 1 đội qua các vòng thi.
        /// </summary>
        /// <param name="teamId">ID của đội thi.</param>
        /// <param name="pageNumber">Trang.</param>
        /// <param name="pageSize">Số dòng mỗi trang.</param>
        /// <param name="sortBy">Trường sắp xếp.</param>
        /// <param name="isAscending">true = tăng dần.</param>
        /// <returns>Lịch sử kết quả của đội (phân trang).</returns>
        [HttpGet("team/{teamId}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<FinalResultModel>>), 200)]
        public async Task<IActionResult> GetByTeamId(
            string teamId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? sortBy = null, 
            [FromQuery] bool isAscending = false)
        {
            var query = new GetFinalResultsByTeamIdQuery 
            {
                TeamId = teamId,
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
