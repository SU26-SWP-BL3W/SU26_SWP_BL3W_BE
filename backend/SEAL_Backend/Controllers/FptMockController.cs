using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            new("SE123456", "Nguyen Van A",    "SE",  2021),
            new("SE789012", "Tran Thi B",      "SE",  2022),
            new("CS001122", "Le Van C",        "CS",  2021),
            new("IA334455", "Pham Thi D",      "IA",  2023),
            new("SS667788", "Hoang Van E",     "SS",  2022),
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

            return Ok(new FptStudentResponse
            {
                IsValid    = true,
                StudentCode = student.StudentCode,
                FullName   = student.FullName,
                Major      = student.Major,
                EnrollYear = student.EnrollYear
            });
        }
    }

    /// <summary>
    /// Record đại diện cho một bản ghi sinh viên trong Mock Database.
    /// </summary>
    public record FptStudentRecord(string StudentCode, string FullName, string Major, int EnrollYear);

    /// <summary>
    /// Model phản hồi thông tin sinh viên từ Mock Service.
    /// </summary>
    public class FptStudentResponse
    {
        /// <summary>
        /// Trạng thái hợp lệ của sinh viên.
        /// </summary>
        public bool   IsValid     { get; set; }

        /// <summary>
        /// Mã số sinh viên.
        /// </summary>
        public string StudentCode { get; set; } = default!;

        /// <summary>
        /// Họ và tên sinh viên.
        /// </summary>
        public string FullName    { get; set; } = default!;

        /// <summary>
        /// Chuyên ngành.
        /// </summary>
        public string Major       { get; set; } = default!;

        /// <summary>
        /// Năm nhập học.
        /// </summary>
        public int    EnrollYear  { get; set; }
    }
}
