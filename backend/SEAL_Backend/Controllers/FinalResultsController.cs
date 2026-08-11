using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý Kết quả chung cuộc (FinalResult). Hiện chỉ có action tính kết quả
    /// (thuộc luồng Chấm điểm — cần để kiểm chứng bản sửa lỗi tính điểm theo Track). Các action
    /// còn lại (Create/Update/Delete/Publish/AssignPrize/Get...) thuộc Luồng 5 — Kết quả/Giải
    /// thưởng, sẽ được thêm khi flow đó được build riêng.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FinalResultsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public FinalResultsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tự động tính kết quả cho từng Hạng mục (Track) đã đủ giám khảo chấm xong trong vòng thi:
        /// tính FinalScore, xếp Rank, đánh dấu IsAdvanced theo Round.AdvancementRule — RIÊNG cho từng
        /// Track (không gộp điểm giữa các Track khác nhau của cùng vòng, vì 1 đội chỉ thi 1 Track).
        /// Track nào chưa đủ giám khảo chấm sẽ bị bỏ qua (trả về trong SkippedTracks), không chặn
        /// các Track khác đã sẵn sàng. Kết quả tính ra ở trạng thái NHÁP (IsPublished=false).
        /// </summary>
        [HttpPost("calculate/{roundId}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator)]
        [ProducesResponseType(typeof(BaseResponse<SEAL_Application.Features.FinalResults.Commands.CalculateRoundResults.Models.CalculateRoundResultsResponseModel>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CalculateRoundResults(string roundId, [FromQuery] int topN)
        {
            var result = await _mediator.Send(new CalculateRoundResultsCommand(roundId, topN));
            return OkResponse(result);
        }
    }
}
