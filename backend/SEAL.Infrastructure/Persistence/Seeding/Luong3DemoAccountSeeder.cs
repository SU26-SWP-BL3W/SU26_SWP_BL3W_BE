using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Tài khoản demo rút gọn riêng cho sự kiện "Demo Luồng 3" (yopmail.com):
    /// member1-10luong3 (thí sinh), ec1-5luong3 (Điều phối viên),
    /// mentor1-5luong3 (Cố vấn), j1-5luong3 (Giám khảo).
    /// Tất cả dùng chung mật khẩu "12345678", đã duyệt/xác thực sẵn.
    /// KHÔNG tự gán EventRole — gán thủ công qua trang mời/quản lý phân công khi cần.
    /// </summary>
    public class Luong3DemoAccountSeeder : IDataSeeder
    {
        private readonly ILogger<Luong3DemoAccountSeeder> _logger;

        public Luong3DemoAccountSeeder(ILogger<Luong3DemoAccountSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 2;

        public async Task SeedAsync(DatabaseContext context)
        {
            var systemSchool = await context.Schools.FirstOrDefaultAsync(s => s.SchoolName == "System");
            if (systemSchool == null) return;

            const string password = "12345678";
            var passwordHash = FixedSaltPasswordHasher.HashPassword(password);
            var demoUsers = new List<User>();

            for (int i = 1; i <= 10; i++)
            {
                demoUsers.Add(new User
                {
                    Email = $"member{i}luong3@yopmail.com",
                    FullName = $"Thành Viên Luồng 3 - {i}",
                    StudentCode = $"LUONG3M{i:D3}",
                    PasswordHash = passwordHash,
                    IsAdmin = false,
                    IsApproved = true,
                    IsEmailVerified = true,
                    IsStudent = true,
                    SchoolId = systemSchool.Id,
                });
            }

            for (int i = 1; i <= 5; i++)
            {
                demoUsers.Add(new User
                {
                    Email = $"ec{i}luong3@yopmail.com",
                    FullName = $"Điều Phối Viên Luồng 3 - {i}",
                    PasswordHash = passwordHash,
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
                    Email = $"j{i}luong3@yopmail.com",
                    FullName = $"Giám Khảo Luồng 3 - {i}",
                    PasswordHash = passwordHash,
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
                    Email = $"mentor{i}luong3@yopmail.com",
                    FullName = $"Cố Vấn Luồng 3 - {i}",
                    PasswordHash = passwordHash,
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
                _logger.LogInformation("Seeded {Count} tai khoan demo Luong 3 (yopmail.com).", seeded);
            }
        }
    }
}
