using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Nạp danh sách sinh viên FPT vào DB thật (bảng FptStudents) từ Google Sheet
    /// "mockapifpt" — CHỈ MỘT LẦN khi bảng còn trống. Sau lần seed đầu tiên, xác thực
    /// sinh viên (Student Verification) đọc thẳng từ DB, không còn gọi ra Google Sheet
    /// ở mỗi request như bản mock cũ (FptMockController) — hết phụ thuộc dịch vụ ngoài
    /// không xác thực, hết nguy cơ rò rỉ dữ liệu qua endpoint công khai.
    /// Muốn thêm/sửa sinh viên sau khi đã seed: dùng API quản trị (không còn qua Sheet).
    /// </summary>
    public class FptStudentSeeder : IDataSeeder
    {
        private const string SheetCsvUrl =
            "https://docs.google.com/spreadsheets/d/1CFWCaZ_xMhI7M1sFbZKmZsz_6ZBvcjZcn-_e55RZgqg/export?format=csv&gid=0";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FptStudentSeeder> _logger;

        public FptStudentSeeder(IHttpClientFactory httpClientFactory, ILogger<FptStudentSeeder> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public int Order => 2;

        public async Task SeedAsync(DatabaseContext context)
        {
            if (await context.FptStudents.AnyAsync())
            {
                return; // đã seed rồi — quản lý tiếp qua API, không ghi đè lại từ Sheet nữa.
            }

            var client = _httpClientFactory.CreateClient();
            string csv;
            try
            {
                csv = await client.GetStringAsync(SheetCsvUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Khong tai duoc Google Sheet FPT de seed lan dau. Bo qua - co the seed thu cong sau qua API.");
                return;
            }

            var lines = ParseCsvLines(csv);
            var seeded = 0;

            // Dòng 1 là header (studentCode, fullName, email, major, campus, enrollYear, status) — bỏ qua.
            foreach (var cols in lines.Skip(1))
            {
                if (cols.Count < 7) continue;

                var studentCode = cols[0].Trim();
                if (string.IsNullOrWhiteSpace(studentCode)) continue;
                if (!int.TryParse(cols[5].Trim(), out var enrollYear)) continue;

                context.FptStudents.Add(new FptStudent
                {
                    StudentCode = studentCode,
                    FullName = cols[1].Trim(),
                    Email = cols[2].Trim(),
                    Major = cols[3].Trim(),
                    Campus = cols[4].Trim(),
                    EnrollYear = enrollYear,
                    Status = cols[6].Trim(),
                });
                seeded++;
            }

            if (seeded > 0)
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} FptStudent tu Google Sheet (lan dau, mot lan duy nhat).", seeded);
            }
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
    }
}
