using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SEAL_Application.Features.Users.Commands.CreateUser.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller giả lập hệ thống quản lý sinh viên của FPT (Mock Service).
    /// Dùng để kiểm tra tính năng xác thực sinh viên (Student Verification).
    /// </summary>
    [ApiController]
    [Route("api/fpt-mock")]
    public class FptMockController : ControllerBase
    {
        private static readonly List<FptStudentRecord> _students = new()
        {
            new("SE123456", "Nguyen Van A",    "se123456@fpt.edu.vn",    "SE",  2021),
            new("SE789012", "Tran Thi B",      "se789012@fpt.edu.vn",    "SE",  2022),
            new("CS001122", "Le Van C",        "cs001122@fpt.edu.vn",    "CS",  2021),
            new("IA334455", "Pham Thi D",      "ia334455@fpt.edu.vn",    "IA",  2023),
            new("SS667788", "Hoang Van E",     "ss667788@fpt.edu.vn",    "SS",  2022),
        };

        /// <summary>
        /// Lấy thông tin sinh viên FPT theo mã sinh viên (Student Code).
        /// </summary>
        /// <param name="studentCode">Mã sinh viên cần kiểm tra (ví dụ: SE123456).</param>
        /// <returns>Thông tin sinh viên nếu tìm thấy, ngược lại trả về 404.</returns>
        [HttpGet("students/{studentCode}")]
        [ProducesResponseType(typeof(FptStudentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetStudent([FromRoute] string studentCode)
        {
            var student = _students.FirstOrDefault(
                s => s.StudentCode.Equals(studentCode, StringComparison.OrdinalIgnoreCase)
            );

            if (student is null)
                return NotFound(new { message = $"Sinh viên '{studentCode}' không tồn tại trong hệ thống FPT." });

            // Shape khớp với SEAL_Application.Features.Users.Commands.CreateUser.Models.FptStudentResponse
            // (model mà UpdateStudentProfileCommandHandler thực sự deserialize vào — {IsValid, Data:{...}, Message}),
            // trước đây controller này tự khai 1 model FLAT riêng khiến Data luôn null khi deserialize.
            return Ok(new FptStudentResponse
            {
                IsValid = true,
                Data = new FptStudentData
                {
                    StudentCode = student.StudentCode,
                    FullName = student.FullName,
                    Email = student.Email,
                    Major = student.Major,
                    EnrollYear = student.EnrollYear
                }
            });
        }
    }

    /// <summary>
    /// Record đại diện cho một bản ghi sinh viên trong Mock Database.
    /// </summary>
    public record FptStudentRecord(string StudentCode, string FullName, string Email, string Major, int EnrollYear);
}
