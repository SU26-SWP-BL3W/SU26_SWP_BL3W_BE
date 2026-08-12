using MediatR;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.Schools.Commands.CreateSchool;
using SEAL_Application.Features.Schools.Commands.CreateSchool.Models;
using SEAL_Application.Features.Schools.Commands.DeleteSchool;
using SEAL_Application.Features.Schools.Commands.UpdateSchool;
using SEAL_Application.Features.Schools.Commands.UpdateSchool.Models;
using SEAL_Application.Features.Schools.Models;
using SEAL_Application.Features.Schools.Queries.GetAllSchools;
using SEAL_Application.Features.Schools.Queries.GetSchoolById;
using SEAL_Application.Features.Schools.Queries.GetSchoolsWithUserCount;
using SEAL_Backend.Helpers;
using SEAL_Backend.Filters;
using SEAL_Domain.Base;
using System.Collections.Generic;
using System.Threading.Tasks;
using SEAL_Application.Commons;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller quản lý các Trường học (School).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SchoolsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public SchoolsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách tất cả các trường học (có phân trang).
        /// </summary>
        /// <param name="query">Tham số phân trang.</param>
        /// <returns>Danh sách trường học.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<PagedResult<SchoolModel>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllSchoolsQuery query)
        {
            var result = await _mediator.Send(query);
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy chi tiết một trường học theo ID.
        /// </summary>
        /// <param name="id">ID của trường học.</param>
        /// <returns>Thông tin trường học.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<SchoolModel>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _mediator.Send(new GetSchoolByIdQuery { Id = id });
            if (result == null) return NotFound();
            return OkResponse(result);
        }

        /// <summary>
        /// Lấy danh sách trường học kèm theo số lượng người dùng.
        /// </summary>
        /// <returns>Danh sách thông tin trường học và số lượng user.</returns>
        [HttpGet("with-user-count")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<SchoolWithUserCountModel>>), 200)]
        public async Task<IActionResult> GetWithUserCount()
        {
            var result = await _mediator.Send(new GetSchoolsWithUserCountQuery());
            return OkResponse(result);
        }

        /// <summary>
        /// Tạo một trường học mới.
        /// </summary>
        /// <param name="requestModel">Thông tin trường học cần tạo.</param>
        /// <returns>Thông tin trường học vừa tạo.</returns>
        [HttpPost]
        [AdminAuthorize]
        [ProducesResponseType(typeof(BaseResponse<CreateSchoolResponseModel>), 200)]
        public async Task<IActionResult> Create([FromBody] CreateSchoolRequestModel requestModel)
        {
            var result = await _mediator.Send(new CreateSchoolCommand { Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Cập nhật thông tin trường học.
        /// </summary>
        /// <param name="id">ID của trường học.</param>
        /// <param name="requestModel">Thông tin cập nhật mới.</param>
        /// <returns>Thông tin trường học sau khi cập nhật.</returns>
        [HttpPut("{id}")]
        [AdminAuthorize]
        [ProducesResponseType(typeof(BaseResponse<UpdateSchoolResponseModel>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSchoolRequestModel requestModel)
        {
            var result = await _mediator.Send(new UpdateSchoolCommand { Id = id, Model = requestModel });
            return OkResponse(result);
        }

        /// <summary>
        /// Xóa trường học (Soft Delete).
        /// </summary>
        /// <param name="id">ID của trường học.</param>
        /// <returns>Kết quả xóa.</returns>
        [HttpDelete("{id}")]
        [AdminAuthorize]
        [ProducesResponseType(typeof(BaseResponse<bool>), 200)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _mediator.Send(new DeleteSchoolCommand { Id = id });
            return OkResponse(result);
        }
    }
}
