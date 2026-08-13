using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Commons;
using SEAL_Application.Features.Templates.Commands.CreateTemplate;
using SEAL_Application.Features.Templates.Commands.CreateTemplate.Models;
using SEAL_Application.Features.Templates.Commands.UpdateTemplate;
using SEAL_Application.Features.Templates.Commands.UpdateTemplate.Models;
using SEAL_Application.Features.Templates.Commands.DeleteTemplate;
using SEAL_Application.Features.Templates.Commands.AddCriteriaToTemplate;
using SEAL_Application.Features.Templates.Commands.AddCriteriaToTemplate.Models;
using SEAL_Application.Features.Templates.Commands.RemoveCriteriaFromTemplate;
using SEAL_Application.Features.Templates.Commands.UpdateTemplateCriteriaConfig;
using SEAL_Application.Features.Templates.Commands.UpdateTemplateCriteriaConfig.Models;
using SEAL_Application.Features.Templates.Queries.GetAllTemplates;
using SEAL_Application.Features.Templates.Queries.GetTemplateById;
using SEAL_Application.Features.Templates.Models;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Base;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Mẫu tiêu chí (Template).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TemplatesController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public TemplatesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một mẫu tiêu chí mới.
        /// </summary>
        /// <param name="requestModel">Thông tin mẫu tiêu chí cần tạo.</param>
        /// <returns>Thông tin mẫu tiêu chí vừa tạo.</returns>
        [HttpPost]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<CreateTemplateResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateTemplateRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateTemplateCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin cơ bản của mẫu tiêu chí.
        /// </summary>
        /// <param name="id">ID của mẫu tiêu chí.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin mẫu tiêu chí sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<UpdateTemplateResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTemplateRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateTemplateCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa một mẫu tiêu chí (xóa vĩnh viễn).
        /// </summary>
        /// <param name="id">ID của mẫu tiêu chí cần xóa.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteTemplateCommand(id));
            return OkResponse(result);
        }

        /// <summary>
        /// Thêm một tiêu chí vào mẫu tiêu chí.
        /// </summary>
        /// <param name="id">ID của mẫu tiêu chí.</param>
        /// <param name="requestModel">Cấu hình tiêu chí (ID tiêu chí, Trọng số, Điểm tối đa).</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpPost("{id}/criteria")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> AddCriteria(string id, [FromBody] AddCriteriaToTemplateRequestModel requestModel)
        {
            var result = await _mediator.Send(new AddCriteriaToTemplateCommand { TemplateId = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật cấu hình của một tiêu chí trong mẫu.
        /// </summary>
        /// <param name="id">ID của mẫu tiêu chí.</param>
        /// <param name="criteriaId">ID của tiêu chí cần cập nhật cấu hình.</param>
        /// <param name="requestModel">Thông tin cấu hình mới (Weight, MaxScore).</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpPut("{id}/criteria/{criteriaId}")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> UpdateCriteriaConfig(string id, string criteriaId, [FromBody] UpdateTemplateCriteriaConfigRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateTemplateCriteriaConfigCommand { TemplateId = id, CriteriaId = criteriaId, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Gỡ bỏ một tiêu chí ra khỏi mẫu tiêu chí.
        /// </summary>
        /// <param name="id">ID của mẫu tiêu chí.</param>
        /// <param name="criteriaId">ID của tiêu chí cần gỡ bỏ.</param>
        /// <returns>Kết quả thực hiện.</returns>
        [HttpDelete("{id}/criteria/{criteriaId}")]
        [AdminOrCoordinatorAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> RemoveCriteria(string id, string criteriaId)
        {
            var result = await _mediator.Send(new RemoveCriteriaFromTemplateCommand { TemplateId = id, CriteriaId = criteriaId });
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách tất cả các mẫu tiêu chí (có phân trang).
        /// </summary>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách các mẫu tiêu chí.</returns>
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<TemplateModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllTemplatesQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một mẫu tiêu chí (kèm danh sách tiêu chí bên trong).
        /// </summary>
        /// <param name="id">ID của mẫu tiêu chí.</param>
        /// <returns>Thông tin mẫu tiêu chí chi tiết.</returns>
        [HttpGet("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ProducesResponseType(typeof(BaseResponse<TemplateModel>), 200)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetTemplateByIdQuery(id));
            return OkResponse(result);
        }
    }
}
