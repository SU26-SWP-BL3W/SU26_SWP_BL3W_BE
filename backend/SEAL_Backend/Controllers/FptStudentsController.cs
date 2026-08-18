using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.FptStudents.Models;
using SEAL_Application.Features.FptStudents.Queries.GetFptStudentByCode;
using SEAL_Backend.Helpers;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Xác thực sinh viên FPT (Student Verification) — tra cứu trong bảng FptStudents thật
    /// của hệ thống, không còn gọi ra Google Sheet ngoài ở mỗi request (xem FptStudentSeeder
    /// để biết cách nạp dữ liệu ban đầu). Yêu cầu đăng nhập để tránh lộ thông tin sinh viên
    /// (họ tên, email, ngành...) cho bất kỳ ai không xác thực.
    /// </summary>
    [ApiController]
    [Route("api/fpt-students")]
    [Authorize]
    public class FptStudentsController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public FptStudentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy thông tin sinh viên FPT theo mã sinh viên (Student Code).
        /// </summary>
        /// <param name="studentCode">Mã sinh viên cần kiểm tra (ví dụ: SE182507).</param>
        [HttpGet("{studentCode}")]
        [ProducesResponseType(typeof(FptStudentModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudent([FromRoute] string studentCode)
        {
            var result = await _mediator.Send(new GetFptStudentByCodeQuery { StudentCode = studentCode });
            if (result.IsFailure) return OkResponse((SEAL_Domain.Base.Result)result);
            if (result.Value == null)
            {
                return NotFound(new { message = $"Sinh viên '{studentCode}' không tồn tại trong hệ thống FPT." });
            }
            return OkResponse(result);
        }
    }
}
