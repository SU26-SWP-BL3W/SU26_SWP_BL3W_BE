using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult;
using SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult.Models;
using SEAL_Application.Features.SubmitResults.Commands.DeleteSubmitResult;
using SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult;
using SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult.Models;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultById.Models;
using SEAL_Application.Commons;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Bài nộp giải pháp (SubmitResult) của các Nhóm trong từng Vòng thi.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SubmitResultsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public SubmitResultsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Nộp bài giải (SubmitResult) mới cho một Track (Vòng thi suy ra qua Track).
        /// </summary>
        /// <param name="requestModel">Thông tin bài nộp giải pháp (bao gồm TeamId và TrackId).</param>
        /// <returns>Thông tin bài nộp vừa tạo.</returns>
        [HttpPost]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<CreateSubmitResultResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateSubmitResultRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateSubmitResultCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin bài giải đã nộp. Chỉ TeamLeader hoặc EventCoordinator được phép.
        /// </summary>
        /// <param name="id">ID của bài nộp.</param>
        /// <param name="requestModel">Thông tin cập nhật bài nộp mới.</param>
        /// <returns>Thông tin bài nộp sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<UpdateSubmitResultResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSubmitResultRequestModel requestModel)
        {
            requestModel.Id = id;
            var result = await _mediator.Send(new UpdateSubmitResultCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một bài giải đã nộp. Chỉ TeamLeader hoặc EventCoordinator được phép.
        /// </summary>
        /// <param name="id">ID của bài nộp cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.TeamLeader)]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteSubmitResultCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin bài nộp theo ID. Chỉ Ban tổ chức hoặc Giám khảo được phép xem.
        /// </summary>
        /// <param name="id">ID của bài nộp.</param>
        /// <returns>Thông tin bài nộp chi tiết.</returns>
        [HttpGet("{id}")]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge, EventRoleType.Mentor)]
        [ProducesResponseType(typeof(BaseResponse<SubmitResultDetailResponseModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetSubmitResultByIdQuery(id));
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách bài nộp (có phân trang). BTC/Giám khảo/Mentor xem theo bộ lọc;
        /// thành viên đội chỉ thấy bài nộp của đội mình (handler tự cưỡng bức phạm vi).
        /// </summary>
        /// <param name="query">Tham số phân trang và bộ lọc.</param>
        /// <returns>Danh sách bài nộp tương ứng.</returns>
        [HttpGet]
        [EventRoleAuthorize(EventRoleType.EventCoordinator, EventRoleType.Judge, EventRoleType.Mentor, EventRoleType.TeamLeader, EventRoleType.TeamMember)]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<SubmitResultListItemModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetSubmitResultsListQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }
    }
}