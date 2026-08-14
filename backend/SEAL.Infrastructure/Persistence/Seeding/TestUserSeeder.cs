using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Persistence.Seeding
{
    public class TestUserSeeder : IDataSeeder
    {
        private readonly ILogger<TestUserSeeder> _logger;

        public TestUserSeeder(ILogger<TestUserSeeder> logger)
        {
            _logger = logger;
        }

        public int Order => 2; // Same as AdminSeeder or slightly after

        public async Task SeedAsync(DatabaseContext context)
        {
            var systemSchool = await context.Schools.FirstOrDefaultAsync(s => s.SchoolName == "System");
            if (systemSchool == null) return;

            var testUsers = new List<User>
            {
                new User
                {
                    Email = "ec.coordinator@seal.edu.vn",
                    FullName = "Điều Phối Viên Sự Kiện",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id
                },
                new User
                {
                    Email = "ec_demo@yopmail.com",
                    FullName = "EC Demo",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id
                },
                new User
                {
                    Email = "judge1@example.com",
                    FullName = "John Judge",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id
                },
                new User
                {
                    Email = "judge_demo@yopmail.com",
                    FullName = "Judge Demo",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id
                },
                new User
                {
                    Email = "judge.ai@seal.edu.vn",
                    FullName = "Giám Khảo Trí Tuệ Nhân Tạo",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id
                },
                new User
                {
                    Email = "mentor.ai@seal.edu.vn",
                    FullName = "Cố Vấn Chuyên Môn AI",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = false,
                    SchoolId = systemSchool.Id
                },
                new User
                {
                    Email = "student1@example.com",
                    FullName = "Alice Student",
                    StudentCode = "SE123456",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = true,
                    SchoolId = systemSchool.Id
                },
                new User
                {
                    Email = "student2@example.com",
                    FullName = "Bob Student",
                    StudentCode = "SE654321",
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = false,
                    IsApproved = true,
                    IsStudent = true,
                    SchoolId = systemSchool.Id
                }
            };

            foreach (var user in testUsers)
            {
                if (!await context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    context.Users.Add(user);
                    _logger.LogInformation("Seeded test User: {Email}", user.Email);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
