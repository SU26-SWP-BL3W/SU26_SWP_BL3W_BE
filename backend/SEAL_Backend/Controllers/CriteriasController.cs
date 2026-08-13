using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Criterias.Commands.CreateCriteria;
using SEAL_Application.Features.Criterias.Commands.CreateCriteria.Models;
using SEAL_Application.Features.Criterias.Commands.UpdateCriteria;
using SEAL_Application.Features.Criterias.Commands.UpdateCriteria.Models;
using SEAL_Application.Features.Criterias.Commands.DeleteCriteria;
using SEAL_Application.Features.Criterias.Commands.ToggleCriteriaStatus;
using SEAL_Application.Features.Criterias.Queries.GetAllCriteria;
using SEAL_Application.Features.Criterias.Queries.GetCriteriaById;
using SEAL_Application.Features.Criterias.Models;
using SEAL_Backend.Helpers;
using SEAL_Domain.Base;
using SEAL_Domain.Ultis;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using SEAL_Backend.Filters;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Tiêu chí chấm điểm gốc (Criteria).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CriteriasController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public CriteriasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một tiêu chí chấm điểm mới.
        /// </summary>
        /// <param name="requestModel">Thông tin tiêu chí cần tạo.</param>
        /// <returns>Thông tin tiêu chí vừa tạo.</returns>
        [HttpPost]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<CreateCriteriaResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateCriteriaRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateCriteriaCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin tiêu chí.
        /// </summary>
        /// <param name="id">ID của tiêu chí.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<UpdateCriteriaResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCriteriaRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateCriteriaCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa tiêu chí (xóa vĩnh viễn). Chỉ Admin hệ thống mới được phép.
        /// </summary>
        /// <param name="id">ID của tiêu chí cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteCriteriaCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động của tiêu chí.
        /// </summary>
        /// <param name="id">ID của tiêu chí.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpPatch("{id}/toggle-status")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var result = await _mediator.Send(new ToggleCriteriaStatusCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách tất cả các tiêu chí gốc (có phân trang).
        /// </summary>
        /// <param name="query">Tham số phân trang và lọc.</param>
        /// <returns>Danh sách các tiêu chí.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<CriteriaModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllCriteriaQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết tiêu chí theo ID.
        /// </summary>
        /// <param name="id">ID của tiêu chí.</param>
        /// <returns>Thông tin tiêu chí chi tiết.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<CriteriaModel>), 200)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetCriteriaByIdQuery(id));
            return OkResponse(result);
        }
    }
}
