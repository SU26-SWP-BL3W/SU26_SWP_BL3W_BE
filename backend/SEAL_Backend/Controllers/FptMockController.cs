using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SEAL_Backend.Controllers
{
    /// <summary>
    /// Controller giả lập hệ thống quản lý sinh viên của FPT (Mock Service).
    /// Dùng để kiểm tra tính năng xác thực sinh viên (Student Verification).
    /// Danh sách sinh viên đọc TRỰC TIẾP từ Google Sheet "mockapifpt" (dạng CSV
    /// export công khai) — ai trong nhóm có quyền chỉnh sheet đều thêm/sửa/xoá
    /// sinh viên test được ngay, không cần đụng tới code hay deploy lại. Sheet
    /// được gọi lại TỪ ĐẦU mỗi lần gọi API (không cache).
    /// Sheet: https://docs.google.com/spreadsheets/d/1CFWCaZ_xMhI7M1sFbZKmZsz_6ZBvcjZcn-_e55RZgqg
    /// Yêu cầu chia sẻ ở chế độ "Anyone with the link — Viewer".
    /// </summary>
    [ApiController]
    [Route("api/fpt-mock")]
    public class FptMockController : ControllerBase
    {
        private const string SheetCsvUrl =
            "https://docs.google.com/spreadsheets/d/1CFWCaZ_xMhI7M1sFbZKmZsz_6ZBvcjZcn-_e55RZgqg/export?format=csv&gid=0";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FptMockController> _logger;

        public FptMockController(IHttpClientFactory httpClientFactory, ILogger<FptMockController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private async Task<List<FptStudentRecord>> LoadStudentsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            string csv;
            try
            {
                csv = await client.GetStringAsync(SheetCsvUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không tải được Google Sheet mock FPT.");
                return new List<FptStudentRecord>();
            }

            var records = new List<FptStudentRecord>();
            var lines = ParseCsvLines(csv);

            // Dòng 1 là header (studentCode, fullName, email, major, campus, enrollYear, status) — bỏ qua.
            foreach (var cols in lines.Skip(1))
            {
                if (cols.Count < 7) continue;

                var studentCode = cols[0].Trim();
                if (string.IsNullOrWhiteSpace(studentCode)) continue;
                if (!int.TryParse(cols[5].Trim(), out var enrollYear)) continue;

                records.Add(new FptStudentRecord(
                    studentCode,
                    cols[1].Trim(),
                    cols[2].Trim(),
                    cols[3].Trim(),
                    cols[4].Trim(),
                    enrollYear,
                    cols[6].Trim()
                ));
            }

            return records;
        }

        /// <summary>
        /// Parser CSV đơn giản có hỗ trợ field đặt trong dấu ngoặc kép (Google Sheets
        /// export field chứa dấu phẩy/xuống dòng theo kiểu này).
        /// </summary>
        private static List<List<string>> ParseCsvLines(string csv)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                var c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { currentRow.Add(field.ToString()); field.Clear(); }
                    else if (c == '\r') { /* bỏ qua, xử lý bằng \n */ }
                    else if (c == '\n')
                    {
                        currentRow.Add(field.ToString());
                        field.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                    }
                    else field.Append(c);
                }
            }

            if (field.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(field.ToString());
                rows.Add(currentRow);
            }

            return rows.Where(r => r.Count > 0 && !(r.Count == 1 && r[0].Length == 0)).ToList();
        }

        /// <summary>
        /// Lấy thông tin sinh viên FPT theo mã sinh viên (Student Code).
        /// </summary>
        /// <param name="studentCode">Mã sinh viên cần kiểm tra (ví dụ: SE182507).</param>
        /// <returns>Thông tin sinh viên nếu tìm thấy, ngược lại trả về 404.</returns>
        [HttpGet("students/{studentCode}")]
        [ProducesResponseType(typeof(FptStudentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudent([FromRoute] string studentCode)
        {
            var students = await LoadStudentsAsync();
            var student = students.FirstOrDefault(
                s => s.StudentCode.Equals(studentCode, StringComparison.OrdinalIgnoreCase)
            );

            // Không tồn tại HOẶC không ở trạng thái ACTIVE (đã tốt nghiệp/bảo lưu...)
            // đều coi là không xác thực được qua hệ thống FPT.
            if (student is null || !student.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = $"Sinh viên '{studentCode}' không tồn tại trong hệ thống FPT." });

            return Ok(new FptStudentResponse
            {
                IsValid    = true,
                StudentCode = student.StudentCode,
                FullName   = student.FullName,
                Email      = student.Email,
                Major      = student.Major,
                Campus     = student.Campus,
                EnrollYear = student.EnrollYear,
                Status     = student.Status
            });
        }
    }

    /// <summary>
    /// Record đại diện cho một bản ghi sinh viên trong Mock Database.
    /// </summary>
    public record FptStudentRecord(string StudentCode, string FullName, string Email, string Major, string Campus, int EnrollYear, string Status);

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
        /// Email sinh viên (FPT hoặc email cá nhân dùng để đăng ký).
        /// </summary>
        public string Email       { get; set; } = default!;

        /// <summary>
        /// Chuyên ngành.
        /// </summary>
        public string Major       { get; set; } = default!;

        /// <summary>
        /// Cơ sở đang theo học.
        /// </summary>
        public string Campus      { get; set; } = default!;

        /// <summary>
        /// Năm nhập học.
        /// </summary>
        public int    EnrollYear  { get; set; }

        /// <summary>
        /// Trạng thái sinh viên (ACTIVE/GRADUATED/SUSPENDED...).
        /// </summary>
        public string Status      { get; set; } = default!;
    }
}
