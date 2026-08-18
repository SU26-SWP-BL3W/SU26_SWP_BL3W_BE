using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Nạp một loạt tài khoản demo (yopmail.com) để test nhanh không cần đăng ký tay:
    /// demo1-20 (thí sinh chung), ec1-3 (Điều phối viên), j1-5 (Giám khảo), mentor1-5 (Cố vấn).
    /// Tất cả dùng chung mật khẩu "123456", đã duyệt/xác thực sẵn. KHÔNG tự gán EventRole cho
    /// ec/j/mentor vào sự kiện nào — gán thủ công qua trang "Mời Giám Khảo & Cố Vấn" khi cần.
    /// </summary>
    public class DemoAccountSeeder : IDataSeeder
    {
        private readonly ILogger<DemoAccountSeeder> _logger;

        public DemoAccountSeeder(ILogger<DemoAccountSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 2;

        public async Task SeedAsync(DatabaseContext context)
        {
            var systemSchool = await context.Schools.FirstOrDefaultAsync(s => s.SchoolName == "System");
            if (systemSchool == null) return;

            var demoUsers = new List<User>();

            for (int i = 1; i <= 20; i++)
            {
                demoUsers.Add(new User
                {
                    Email = $"demo{i}@yopmail.com",
                    FullName = $"Thí Sinh Demo {i}",
                    StudentCode = $"DEMO{i:D4}",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsEmailVerified = true,
                    IsStudent = true,
                    SchoolId = systemSchool.Id,
                });
            }

            for (int i = 1; i <= 3; i++)
            {
                demoUsers.Add(new User
                {
                    Email = $"ec{i}@yopmail.com",
                    FullName = $"Điều Phối Viên Demo {i}",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsEmailVerified = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id,
                });
            }

            for (int i = 1; i <= 5; i++)
            {
                demoUsers.Add(new User
                {
                    Email = $"j{i}@yopmail.com",
                    FullName = $"Giám Khảo Demo {i}",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsEmailVerified = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id,
                });
            }

            for (int i = 1; i <= 5; i++)
            {
                demoUsers.Add(new User
                {
                    Email = $"mentor{i}@yopmail.com",
                    FullName = $"Cố Vấn Demo {i}",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsEmailVerified = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id,
                });
            }

            var seeded = 0;
            foreach (var user in demoUsers)
            {
                var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existing == null)
                {
                    context.Users.Add(user);
                    seeded++;
                }
            }

            if (seeded > 0)
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} tai khoan demo (yopmail.com).", seeded);
            }
        }
    }
}
