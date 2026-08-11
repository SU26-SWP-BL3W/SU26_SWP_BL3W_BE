using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Domain.Base;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Demo.Commands.SetupDemoEvents
{
    public class SetupDemoEventsCommandHandler : IRequestHandler<SetupDemoEventsCommand, Result<BaseResponse<bool>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SetupDemoEventsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<BaseResponse<bool>>> Handle(SetupDemoEventsCommand request, CancellationToken cancellationToken)
        {
            // Reset thời gian về UTC chuẩn
            var targetDate = request.TargetDate.ToUniversalTime();

            // 1. Dọn dẹp Demo Events cũ (nếu có) để không bị rác khi ấn nhiều lần
            var oldEvents = await _unitOfWork.GetRepository<Event>().Entities
                .Where(e => e.EventName.StartsWith("[DEMO]"))
                .ToListAsync(cancellationToken);
            
            if (oldEvents.Any())
            {
                var oldEventIds = oldEvents.Select(e => e.Id).ToList();

                // 1. Xoá Appeals (Restrict theo Team)
                var oldAppeals = await _unitOfWork.GetRepository<Appeal>().Entities
                    .Where(a => a.Team != null && oldEventIds.Contains(a.Team.EventId))
                    .ToListAsync(cancellationToken);
                if (oldAppeals.Any()) _unitOfWork.GetRepository<Appeal>().DeleteRange(oldAppeals);

                // 2. Xoá FinalResults (Restrict theo Event/Team)
                var oldFinalResults = await _unitOfWork.GetRepository<FinalResult>().Entities
                    .Where(fr => fr.EventId != null && oldEventIds.Contains(fr.EventId))
                    .ToListAsync(cancellationToken);
                if (oldFinalResults.Any()) _unitOfWork.GetRepository<FinalResult>().DeleteRange(oldFinalResults);

                // 3. Xoá Scores (Restrict theo EventRole)
                var oldScores = await _unitOfWork.GetRepository<Score>().Entities
                    .Where(s => s.EventRole != null && oldEventIds.Contains(s.EventRole.EventId))
                    .ToListAsync(cancellationToken);
                if (oldScores.Any()) _unitOfWork.GetRepository<Score>().DeleteRange(oldScores);

                // 4. Xoá EventRoles (Restrict)
                var oldEventRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                    .Where(er => oldEventIds.Contains(er.EventId))
                    .ToListAsync(cancellationToken);
                if (oldEventRoles.Any()) _unitOfWork.GetRepository<EventRole>().DeleteRange(oldEventRoles);

                // Xoá Event (sẽ tự động cascade xoá Round, Track, Team, SubmitResult)
                _unitOfWork.GetRepository<Event>().DeleteRange(oldEvents);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 2. Lấy hoặc tạo Users cần thiết
            var fptSchool = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(s => s.SchoolName == "FPT University", cancellationToken);
            var schoolId = fptSchool?.Id ?? string.Empty;

            var ecUser = await GetOrCreateUserAsync("ec_demo@yopmail.com", "EC Demo", true, false, schoolId, cancellationToken);
            var judgeUser = await GetOrCreateUserAsync("judge_demo@yopmail.com", "Judge Demo", false, false, schoolId, cancellationToken);
            var student1 = await GetOrCreateUserAsync("student1_demo@yopmail.com", "Student 1 Demo", false, true, schoolId, cancellationToken, "SE111111", true, "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg");
            var student2 = await GetOrCreateUserAsync("student2_demo@yopmail.com", "Student 2 Demo", false, true, schoolId, cancellationToken, "SE222222", true, "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg");
            var student3 = await GetOrCreateUserAsync("student3_demo@yopmail.com", "Student 3 Demo", false, true, schoolId, cancellationToken, "SE333333", true, "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg");
            var student4 = await GetOrCreateUserAsync("student4_demo@yopmail.com", "Student 4 Demo", false, true, schoolId, cancellationToken, "SE444444", true, "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg");

            // Lấy một Template có sẵn (nếu có)
            var template = await _unitOfWork.GetRepository<Template>().Entities.FirstOrDefaultAsync(cancellationToken);

            // ==========================================
            // EVENT 1: NỘP BÀI (Submission Phase)
            // ==========================================
            var event1 = new Event
            {
                EventName = $"[DEMO] Sự kiện Nộp Bài - {targetDate:dd/MM/yyyy}",
                Description = "Sự kiện được thiết lập để demo việc sinh viên nộp bài thi. Đang trong khoảng thời gian nộp bài.",
                RegistrationStartDate = targetDate.AddDays(-30),
                RegistrationEndDate = targetDate.AddDays(-20),
                StartDate = targetDate.AddDays(-15),
                EndDate = targetDate.AddDays(15),
                Status = true,
                Year = targetDate.Year
            };
            await _unitOfWork.GetRepository<Event>().AddAsync(event1);
            
            var round1 = new Round
            {
                Event = event1,
                RoundName = "Vòng chung kết",
                RoundNumber = 1,
                AdvancementRule = "top:10",
                StartDate = targetDate.AddDays(-10),
                EndDate = targetDate.AddDays(10),
            };
            await _unitOfWork.GetRepository<Round>().AddAsync(round1);

            var track1 = new Track
            {
                Round = round1,
                TrackName = "Phần mềm",
                Description = "Hạng mục dành cho các sản phẩm phần mềm, ứng dụng web/mobile.",
                SubmissionRuleDescription = "Link github repository dự án\nLink demo",
                TemplateId = template?.Id,
                StartDate = targetDate.AddDays(-2), // Bắt đầu nộp bài trước đó 2 ngày
                EndDate = targetDate.AddDays(2),    // Kết thúc nộp bài sau 2 ngày (Đang mở nộp bài)
                ScoringStartDate = targetDate.AddDays(2),
                ScoringEndDate = targetDate.AddDays(10)
            };
            await _unitOfWork.GetRepository<Track>().AddAsync(track1);

            // Team 1
            var team1 = new Team { Event = event1, Name = "Team Nộp Bài 1", Status = TeamStatus.Registered };
            await _unitOfWork.GetRepository<Team>().AddAsync(team1);
            await AddEventRoleAsync(student1.Id, event1.Id, team1.Id, null, EventRoleType.TeamLeader, event1.EndDate);
            await AddEventRoleAsync(student2.Id, event1.Id, team1.Id, null, EventRoleType.TeamMember, event1.EndDate);

            // Team 2
            var team2 = new Team { Event = event1, Name = "Team Nộp Bài 2", Status = TeamStatus.Registered };
            await _unitOfWork.GetRepository<Team>().AddAsync(team2);
            await AddEventRoleAsync(student3.Id, event1.Id, team2.Id, null, EventRoleType.TeamLeader, event1.EndDate);
            await AddEventRoleAsync(student4.Id, event1.Id, team2.Id, null, EventRoleType.TeamMember, event1.EndDate);

            // Phân quyền EC cho event 1
            await AddEventRoleAsync(ecUser.Id, event1.Id, null, null, EventRoleType.EventCoordinator, event1.EndDate);

            // ==========================================
            // EVENT 2: CHẤM ĐIỂM (Scoring Phase)
            // ==========================================
            var event2 = new Event
            {
                EventName = $"[DEMO] Sự kiện Chấm Điểm - {targetDate:dd/MM/yyyy}",
                Description = "Sự kiện được thiết lập để demo việc giám khảo chấm bài. Đã kết thúc nộp bài và đang trong khoảng thời gian chấm điểm.",
                RegistrationStartDate = targetDate.AddDays(-45),
                RegistrationEndDate = targetDate.AddDays(-35),
                StartDate = targetDate.AddDays(-30),
                EndDate = targetDate.AddDays(15),
                Status = true,
                Year = targetDate.Year
            };
            await _unitOfWork.GetRepository<Event>().AddAsync(event2);

            var round2 = new Round
            {
                Event = event2,
                RoundName = "Vòng đánh giá",
                RoundNumber = 1,
                AdvancementRule = "percent:50",
                StartDate = targetDate.AddDays(-25),
                EndDate = targetDate.AddDays(10),
                ScoringStartDate = targetDate.AddDays(-2),
                ScoringEndDate = targetDate.AddDays(5)
            };
            await _unitOfWork.GetRepository<Round>().AddAsync(round2);

            var round3 = new Round
            {
                Event = event2,
                RoundName = "Vòng chung kết",
                RoundNumber = 2,
                AdvancementRule = "top:1",
                StartDate = targetDate.AddDays(11),
                EndDate = targetDate.AddDays(20),
                ScoringStartDate = targetDate.AddDays(15),
                ScoringEndDate = targetDate.AddDays(19)
            };
            await _unitOfWork.GetRepository<Round>().AddAsync(round3);

            var track2 = new Track
            {
                Round = round2,
                TrackName = "Thiết kế",
                Description = "Hạng mục dành cho các sản phẩm thiết kế đồ họa, UI/UX.",
                SubmissionRuleDescription = "Link demo\nLink báo cáo/slide",
                TemplateId = template?.Id,
                StartDate = targetDate.AddDays(-10), // Bắt đầu nộp bài
                EndDate = targetDate.AddDays(-3),    // ĐÃ HẾT HẠN NỘP BÀI
                ScoringStartDate = targetDate.AddDays(-2), // Bắt đầu chấm điểm
                ScoringEndDate = targetDate.AddDays(5)     // ĐANG TRONG THỜI GIAN CHẤM ĐIỂM
            };
            await _unitOfWork.GetRepository<Track>().AddAsync(track2);

            var track3 = new Track
            {
                Round = round2,
                TrackName = "Phần mềm",
                Description = "Hạng mục dành cho các sản phẩm phần mềm, ứng dụng web/mobile.",
                SubmissionRuleDescription = "Link github repository dự án\nLink demo",
                TemplateId = template?.Id,
                StartDate = targetDate.AddDays(-10), // Bắt đầu nộp bài
                EndDate = targetDate.AddDays(-3),    // ĐÃ HẾT HẠN NỘP BÀI
                ScoringStartDate = targetDate.AddDays(-2), // Bắt đầu chấm điểm
                ScoringEndDate = targetDate.AddDays(5)     // ĐANG TRONG THỜI GIAN CHẤM ĐIỂM
            };
            await _unitOfWork.GetRepository<Track>().AddAsync(track3);

            var track4 = new Track
            {
                Round = round3,
                TrackName = "Thiết kế",
                Description = "Hạng mục dành cho các sản phẩm thiết kế đồ họa, UI/UX (Vòng Chung kết).",
                SubmissionRuleDescription = "Link demo\nLink báo cáo/slide",
                TemplateId = template?.Id,
                StartDate = targetDate.AddDays(11), 
                EndDate = targetDate.AddDays(14),    
                ScoringStartDate = targetDate.AddDays(15), 
                ScoringEndDate = targetDate.AddDays(19)     
            };
            await _unitOfWork.GetRepository<Track>().AddAsync(track4);

            var track5 = new Track
            {
                Round = round3,
                TrackName = "Phần mềm",
                Description = "Hạng mục dành cho các sản phẩm phần mềm, ứng dụng web/mobile (Vòng Chung kết).",
                SubmissionRuleDescription = "Link github repository dự án\nLink demo",
                TemplateId = template?.Id,
                StartDate = targetDate.AddDays(11), 
                EndDate = targetDate.AddDays(14),    
                ScoringStartDate = targetDate.AddDays(15), 
                ScoringEndDate = targetDate.AddDays(19)     
            };
            await _unitOfWork.GetRepository<Track>().AddAsync(track5);

            // Team 3
            var team3 = new Team { Event = event2, Name = "Team Chấm Điểm 1", Status = TeamStatus.Registered };
            await _unitOfWork.GetRepository<Team>().AddAsync(team3);
            await AddEventRoleAsync(student1.Id, event2.Id, team3.Id, null, EventRoleType.TeamLeader, event2.EndDate);
            
            // Team 4
            var team4 = new Team { Event = event2, Name = "Team Chấm Điểm 2", Status = TeamStatus.Registered };
            await _unitOfWork.GetRepository<Team>().AddAsync(team4);
            await AddEventRoleAsync(student3.Id, event2.Id, team4.Id, null, EventRoleType.TeamLeader, event2.EndDate);

            // Phân quyền EC cho event 2
            await AddEventRoleAsync(ecUser.Id, event2.Id, null, null, EventRoleType.EventCoordinator, event2.EndDate);

            // Phân quyền Giám khảo cho event 2 (Chấm track 2 và track 3)
            await AddEventRoleAsync(judgeUser.Id, event2.Id, null, track2.Id, EventRoleType.Judge, event2.EndDate);
            await AddEventRoleAsync(judgeUser.Id, event2.Id, null, track3.Id, EventRoleType.Judge, event2.EndDate);

            // Lưu DB để có các ID hợp lệ
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Thêm bài nộp (SubmitResult) cho các Team ở Event 2
            var submit1 = new SubmitResult
            {
                TeamId = team3.Id,
                TrackId = track2.Id,
                SubmissionUrl = "https://github.com/team1/demo",
                Description = "Bài nộp siêu cấp VIP",
                IsActive = true,
                CreatedBy = student1.Id,
                CreatedTime = targetDate.AddDays(-4) // nộp trước hạn
            };
            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submit1);

            var submit2 = new SubmitResult
            {
                TeamId = team4.Id,
                TrackId = track2.Id,
                SubmissionUrl = "https://github.com/team2/demo",
                Description = "Bài nộp cực kì xuất sắc",
                IsActive = true,
                CreatedBy = student3.Id,
                CreatedTime = targetDate.AddDays(-5) // nộp trước hạn
            };
            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submit2);

            var submit1_track3 = new SubmitResult
            {
                TeamId = team3.Id,
                TrackId = track3.Id,
                SubmissionUrl = "https://github.com/team1/demo-phanmem",
                Description = "Bài nộp siêu cấp VIP (Phần mềm)",
                IsActive = true,
                CreatedBy = student1.Id,
                CreatedTime = targetDate.AddDays(-4) // nộp trước hạn
            };
            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submit1_track3);

            var submit2_track3 = new SubmitResult
            {
                TeamId = team4.Id,
                TrackId = track3.Id,
                SubmissionUrl = "https://github.com/team2/demo-phanmem",
                Description = "Bài nộp cực kì xuất sắc (Phần mềm)",
                IsActive = true,
                CreatedBy = student3.Id,
                CreatedTime = targetDate.AddDays(-5) // nộp trước hạn
            };
            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submit2_track3);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.OkResponse(true, "Đã tạo 2 sự kiện Demo thành công.");
        }

        private async Task<User> GetOrCreateUserAsync(string email, string fullName, bool isAdmin, bool isStudent, string schoolId, CancellationToken ct, string studentCode = null, bool isFpt = false, string photoUrl = null)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FullName = fullName,
                    PasswordHash = FixedSaltPasswordHasher.HashPassword("123456"),
                    IsAdmin = isAdmin,
                    IsStudent = isStudent,
                    IsApproved = true,
                    IsEmailVerified = true,
                    SchoolId = schoolId,
                    StudentCode = studentCode,
                    IsFpt = isFpt,
                    PhotoStudentCardUrl = photoUrl
                };
                await _unitOfWork.GetRepository<User>().AddAsync(user);
            }
            else
            {
                // Nếu tài khoản đã tồn tại, đảm bảo IsEmailVerified = true và IsApproved = true để luôn login được
                user.IsEmailVerified = true;
                user.IsApproved = true;
                _unitOfWork.GetRepository<User>().Update(user);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            return user;
        }

        private async Task AddEventRoleAsync(string userId, string eventId, string? teamId, string? trackId, EventRoleType roleName, DateTime? expiredAt)
        {
            var role = new EventRole
            {
                UserId = userId,
                EventId = eventId,
                TeamId = teamId,
                TrackId = trackId,
                RoleName = roleName,
                ExpiredAt = expiredAt
            };
            await _unitOfWork.GetRepository<EventRole>().AddAsync(role);
        }
    }
}



