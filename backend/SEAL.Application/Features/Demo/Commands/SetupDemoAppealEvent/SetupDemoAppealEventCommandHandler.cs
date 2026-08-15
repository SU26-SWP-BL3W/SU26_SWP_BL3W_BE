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

namespace SEAL_Application.Features.Demo.Commands.SetupDemoAppealEvent
{
    public class SetupDemoAppealEventCommandHandler : IRequestHandler<SetupDemoAppealEventCommand, Result<BaseResponse<bool>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SetupDemoAppealEventCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<BaseResponse<bool>>> Handle(SetupDemoAppealEventCommand request, CancellationToken cancellationToken)
        {
            var targetDate = request.TargetDate.ToUniversalTime();

            // 1. Dọn dẹp Demo Event Phúc Khảo cũ (nếu có) để không bị rác khi gọi lại nhiều lần.
            //    Chỉ lọc đúng tiền tố của handler này — tránh dọn nhầm demo event của SetupDemoEvents.
            var oldEvents = await _unitOfWork.GetRepository<Event>().Entities
                .Where(e => e.EventName.StartsWith("[DEMO] Sự kiện Phúc Khảo"))
                .ToListAsync(cancellationToken);

            if (oldEvents.Any())
            {
                var oldEventIds = oldEvents.Select(e => e.Id).ToList();

                var oldAppeals = await _unitOfWork.GetRepository<Appeal>().Entities
                    .Where(a => a.Team != null && oldEventIds.Contains(a.Team.EventId))
                    .ToListAsync(cancellationToken);
                if (oldAppeals.Any()) _unitOfWork.GetRepository<Appeal>().DeleteRange(oldAppeals);

                var oldFinalResults = await _unitOfWork.GetRepository<FinalResult>().Entities
                    .Where(fr => fr.EventId != null && oldEventIds.Contains(fr.EventId))
                    .ToListAsync(cancellationToken);
                if (oldFinalResults.Any()) _unitOfWork.GetRepository<FinalResult>().DeleteRange(oldFinalResults);

                var oldScores = await _unitOfWork.GetRepository<Score>().Entities
                    .Where(s => s.EventRole != null && oldEventIds.Contains(s.EventRole.EventId))
                    .ToListAsync(cancellationToken);
                if (oldScores.Any()) _unitOfWork.GetRepository<Score>().DeleteRange(oldScores);

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

            var ecUser = await GetOrCreateUserAsync("ec_demo@yopmail.com", "EC Demo", false, false, schoolId, cancellationToken);
            var judgeUser = await GetOrCreateUserAsync("judge_demo@yopmail.com", "Judge Demo", false, false, schoolId, cancellationToken);
            var student1 = await GetOrCreateUserAsync("student1_demo@yopmail.com", "Student 1 Demo", false, true, schoolId, cancellationToken, "SE111111", true, "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg");
            var student3 = await GetOrCreateUserAsync("student3_demo@yopmail.com", "Student 3 Demo", false, true, schoolId, cancellationToken, "SE333333", true, "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg");

            var template = await _unitOfWork.GetRepository<Template>().Entities.FirstOrDefaultAsync(cancellationToken);

            // ==========================================
            // EVENT 3: PHÚC KHẢO (Appeal Phase)
            // ==========================================
            var event3 = new Event
            {
                EventName = $"[DEMO] Sự kiện Phúc Khảo - {targetDate:dd/MM/yyyy}",
                Description = "Sự kiện được thiết lập để demo việc sinh viên phúc khảo. Đã kết thúc nộp bài, đã có điểm và đang trong khoảng thời gian phúc khảo.",
                RegistrationStartDate = targetDate.AddDays(-60),
                RegistrationEndDate = targetDate.AddDays(-50),
                StartDate = targetDate.AddDays(-45),
                EndDate = targetDate.AddDays(15),
                Status = true,
                Year = targetDate.Year
            };
            await _unitOfWork.GetRepository<Event>().AddAsync(event3);

            var round3 = new Round
            {
                Event = event3,
                RoundName = "Vòng chung kết",
                RoundNumber = 1,
                AdvancementRule = "top:10",
                StartDate = targetDate.AddDays(-40),
                EndDate = targetDate.AddDays(10),
                ScoringStartDate = targetDate.AddDays(-15),
                ScoringEndDate = targetDate.AddDays(-5), // Đã chấm xong
                AppealStartDate = targetDate.AddDays(-2), // Đang mở phúc khảo
                AppealEndDate = targetDate.AddDays(5)
            };
            await _unitOfWork.GetRepository<Round>().AddAsync(round3);

            var track3 = new Track
            {
                Event = event3,
                TrackName = "Phần mềm",
                Description = "Hạng mục phần mềm",
                SubmissionRuleDescription = "Link demo",
                TemplateId = template?.Id,
                StartDate = targetDate.AddDays(-30),
                EndDate = targetDate.AddDays(-20),
                ScoringStartDate = targetDate.AddDays(-15),
                ScoringEndDate = targetDate.AddDays(-5)
            };
            await _unitOfWork.GetRepository<Track>().AddAsync(track3);

            // Teams
            var team1 = new Team { Event = event3, Name = "Team Phúc Khảo 1", Status = TeamStatus.Registered };
            await _unitOfWork.GetRepository<Team>().AddAsync(team1);
            await AddEventRoleAsync(student1.Id, event3.Id, team1.Id, null, EventRoleType.TeamLeader, event3.EndDate);

            var team2 = new Team { Event = event3, Name = "Team Phúc Khảo 2", Status = TeamStatus.Registered };
            await _unitOfWork.GetRepository<Team>().AddAsync(team2);
            await AddEventRoleAsync(student3.Id, event3.Id, team2.Id, null, EventRoleType.TeamLeader, event3.EndDate);

            await AddEventRoleAsync(ecUser.Id, event3.Id, null, null, EventRoleType.EventCoordinator, event3.EndDate);
            var judgeRole = await AddEventRoleAndReturnAsync(judgeUser.Id, event3.Id, null, track3.Id, EventRoleType.Judge, event3.EndDate);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Add Submissions
            var submit1 = new SubmitResult
            {
                TeamId = team1.Id,
                TrackId = track3.Id,
                SubmissionUrl = "https://github.com/team1",
                Description = "Bài nộp team 1",
                IsActive = true,
                CreatedBy = student1.Id,
                CreatedTime = targetDate.AddDays(-25)
            };
            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submit1);

            var submit2 = new SubmitResult
            {
                TeamId = team2.Id,
                TrackId = track3.Id,
                SubmissionUrl = "https://github.com/team2",
                Description = "Bài nộp team 2",
                IsActive = true,
                CreatedBy = student3.Id,
                CreatedTime = targetDate.AddDays(-22)
            };
            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submit2);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Add Scores
            var score1 = new Score
            {
                EventRoleId = judgeRole.Id,
                SubmitResultId = submit1.Id,
                TotalScore = 75,
                Comment = "Khá tốt nhưng chưa hoàn thiện."
            };
            await _unitOfWork.GetRepository<Score>().AddAsync(score1);

            var score2 = new Score
            {
                EventRoleId = judgeRole.Id,
                SubmitResultId = submit2.Id,
                TotalScore = 90,
                Comment = "Xuất sắc."
            };
            await _unitOfWork.GetRepository<Score>().AddAsync(score2);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Add Appeals
            var appeal1 = new Appeal
            {
                TeamId = team1.Id,
                SubmitResultId = submit1.Id,
                Reason = "Tôi nghĩ bài của nhóm tôi xứng đáng điểm cao hơn.",
                Status = AppealStatus.Pending
            };
            await _unitOfWork.GetRepository<Appeal>().AddAsync(appeal1);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.OkResponse(true, "Đã tạo sự kiện Demo có dữ liệu Phúc khảo thành công.");
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

        private async Task<EventRole> AddEventRoleAndReturnAsync(string userId, string eventId, string? teamId, string? trackId, EventRoleType roleName, DateTime? expiredAt)
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
            return role;
        }
    }
}



