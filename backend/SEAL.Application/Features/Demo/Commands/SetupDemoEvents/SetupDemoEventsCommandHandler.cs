using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Domain.Base;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Domain.Ultis;
using System;
using System.Collections.Generic;
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

            // 1. Dọn event seed cũ (tên mới + các prefix cũ) để bấm lại không bị trùng/rác
            var oldEvents = await _unitOfWork.GetRepository<Event>().Entities
                .Where(e => e.EventName.StartsWith("Nộp Bài & Chấm -")
                         || e.EventName.StartsWith("[DEMO LIVE] Nộp Bài")
                         || e.EventName.StartsWith("[DEMO] Sự kiện Nộp Bài")
                         || e.EventName.StartsWith("[DEMO] Sự kiện Chấm Điểm"))
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

            var ecUser = await GetOrCreateUserAsync("ec_demo@yopmail.com", "EC Demo", false, false, schoolId, cancellationToken);
            var judgeUser = await GetOrCreateUserAsync("judge_demo@yopmail.com", "Judge Demo", false, false, schoolId, cancellationToken);
            var mentorUser = await GetOrCreateUserAsync("mentor_demo@yopmail.com", "Mentor Demo", false, false, schoolId, cancellationToken);
            var photo = "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg";
            var student1 = await GetOrCreateUserAsync("student1_demo@yopmail.com", "Student 1 Demo", false, true, schoolId, cancellationToken, "SE111111", true, photo);
            var student2 = await GetOrCreateUserAsync("student2_demo@yopmail.com", "Student 2 Demo", false, true, schoolId, cancellationToken, "SE222222", true, photo);
            var student3 = await GetOrCreateUserAsync("student3_demo@yopmail.com", "Student 3 Demo", false, true, schoolId, cancellationToken, "SE333333", true, photo);
            var student4 = await GetOrCreateUserAsync("student4_demo@yopmail.com", "Student 4 Demo", false, true, schoolId, cancellationToken, "SE444444", true, photo);

            // Gỡ toàn bộ vai trò cũ của tài khoản demo (tránh student1 còn đội Forming ở event khác)
            await ClearDemoUserRolesAsync(
                new[] { ecUser.Id, judgeUser.Id, mentorUser.Id, student1.Id, student2.Id, student3.Id, student4.Id },
                cancellationToken);

            // Lấy một Template có sẵn (nếu có) + tiêu chí để seed ScoreDetail
            var template = await _unitOfWork.GetRepository<Template>().Entities.FirstOrDefaultAsync(cancellationToken);
            var templateCriteria = template == null
                ? new List<TemplateCriteria>()
                : await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                    .Where(tc => tc.TemplateId == template.Id)
                    .OrderBy(tc => tc.CriteriaId)
                    .ToListAsync(cancellationToken);

            // Một sự kiện: Nộp → Chấm → Mentor → EC tính/công bố
            // Team 1 trống (student1 nộp); Team 2 đã nộp + chấm sẵn
            var event1 = new Event
            {
                EventName = $"Nộp Bài & Chấm - {targetDate:dd/MM/yyyy}",
                Description = "Nộp link → Judge chấm bài mới → Mentor feedback → EC tính & công bố. Team 2 đã có bài+điểm sẵn.",
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
                StartDate = targetDate.AddDays(-2),
                EndDate = targetDate.AddDays(2),
                ScoringStartDate = targetDate.AddDays(-1),
                ScoringEndDate = targetDate.AddDays(10)
            };
            await _unitOfWork.GetRepository<Round>().AddAsync(round1);

            var track1 = new Track
            {
                Event = event1,
                TrackName = "Phần mềm",
                Description = "Hạng mục dành cho các sản phẩm phần mềm, ứng dụng web/mobile.",
                SubmissionRuleDescription = "Link github repository dự án\nLink demo\nLink slide",
                TemplateId = template?.Id,
                StartDate = targetDate.AddDays(-2),
                EndDate = targetDate.AddDays(2),
                ScoringStartDate = targetDate.AddDays(-1),
                ScoringEndDate = targetDate.AddDays(10)
            };
            await _unitOfWork.GetRepository<Track>().AddAsync(track1);

            var team1 = new Team { Event = event1, Name = "Team Nộp Bài 1", Status = TeamStatus.Registered, TrackId = track1.Id };
            await _unitOfWork.GetRepository<Team>().AddAsync(team1);
            await AddEventRoleAsync(student1.Id, event1.Id, team1.Id, null, EventRoleType.TeamLeader, event1.EndDate);
            await AddEventRoleAsync(student2.Id, event1.Id, team1.Id, null, EventRoleType.TeamMember, event1.EndDate);

            var team2 = new Team { Event = event1, Name = "Team Nộp Bài 2", Status = TeamStatus.Registered, TrackId = track1.Id };
            await _unitOfWork.GetRepository<Team>().AddAsync(team2);
            await AddEventRoleAsync(student3.Id, event1.Id, team2.Id, null, EventRoleType.TeamLeader, event1.EndDate);
            await AddEventRoleAsync(student4.Id, event1.Id, team2.Id, null, EventRoleType.TeamMember, event1.EndDate);

            await AddEventRoleAsync(ecUser.Id, event1.Id, null, null, EventRoleType.EventCoordinator, event1.EndDate);
            await AddEventRoleAsync(judgeUser.Id, event1.Id, null, track1.Id, EventRoleType.Judge, event1.EndDate);
            await AddEventRoleAsync(mentorUser.Id, event1.Id, null, track1.Id, EventRoleType.Mentor, event1.EndDate);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var judgeEvent1Role = await _unitOfWork.GetRepository<EventRole>().Entities
                .FirstAsync(er => er.UserId == judgeUser.Id
                               && er.EventId == event1.Id
                               && er.RoleName == EventRoleType.Judge
                               && er.TrackId == track1.Id, cancellationToken);

            // Chỉ Team 2 có bài+điểm sẵn; Team 1 để trống nộp
            var submitReady2 = new SubmitResult
            {
                TeamId = team2.Id,
                TrackId = track1.Id,
                RoundId = round1.Id,
                SubmissionUrl = "https://github.com/dotnet/aspnetcore",
                RepoUrl = "https://github.com/dotnet/aspnetcore",
                DemoUrl = "https://dotnet.microsoft.com",
                SlideUrl = "https://docs.google.com",
                Description = "Bài nộp Team Nộp Bài 2 (đã chấm sẵn).",
                IsActive = true,
                CreatedBy = student3.Id,
                CreatedTime = targetDate.AddDays(-1)
            };
            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submitReady2);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var scoreReady2 = new Score
            {
                EventRoleId = judgeEvent1Role.Id,
                SubmitResultId = submitReady2.Id,
                TotalScore = 8.50m,
                Comment = "Điểm đã chốt cho Team Nộp Bài 2.",
                IsSubmitted = true
            };
            await _unitOfWork.GetRepository<Score>().AddAsync(scoreReady2);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (template != null && templateCriteria.Count > 0)
            {
                var detailValues2 = new[] { 8.5m, 8.5m, 8.5m, 8.5m };
                var details = new List<ScoreDetail>();
                for (int i = 0; i < templateCriteria.Count; i++)
                {
                    details.Add(new ScoreDetail
                    {
                        ScoreId = scoreReady2.Id,
                        TemplateId = template.Id,
                        CriteriaId = templateCriteria[i].CriteriaId,
                        Value = detailValues2[Math.Min(i, detailValues2.Length - 1)]
                    });
                }
                await _unitOfWork.GetRepository<ScoreDetail>().AddRangeAsync(details);
            }

            await _unitOfWork.GetRepository<MentorFeedback>().AddAsync(new MentorFeedback
            {
                SubmitResultId = submitReady2.Id,
                MentorId = mentorUser.Id,
                Comment = "Kiến trúc ổn, nên bổ sung README và video demo ngắn trước vòng chấm."
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.OkResponse(true,
                $"Đã tạo Nộp Bài & Chấm (eventId={event1.Id}). Team 1 trống (student1_demo, Registered); Team 2 đã chấm sẵn. Pass: 123456.");
        }

        /// <summary>
        /// Xóa mọi EventRole (và Score liên quan) của tài khoản demo trước khi gán lại — tránh nhầm đội Forming ở sự kiện cũ.
        /// </summary>
        private async Task ClearDemoUserRolesAsync(IReadOnlyCollection<string> demoUserIds, CancellationToken cancellationToken)
        {
            if (demoUserIds.Count == 0) return;

            var demoRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => demoUserIds.Contains(er.UserId))
                .ToListAsync(cancellationToken);
            if (demoRoles.Count == 0) return;

            var demoRoleIds = demoRoles.Select(r => r.Id).ToList();

            var demoScores = await _unitOfWork.GetRepository<Score>().Entities
                .Where(s => demoRoleIds.Contains(s.EventRoleId))
                .ToListAsync(cancellationToken);
            if (demoScores.Any())
            {
                var demoScoreIds = demoScores.Select(s => s.Id).ToList();
                var demoScoreDetails = await _unitOfWork.GetRepository<ScoreDetail>().Entities
                    .Where(sd => demoScoreIds.Contains(sd.ScoreId))
                    .ToListAsync(cancellationToken);
                if (demoScoreDetails.Any()) _unitOfWork.GetRepository<ScoreDetail>().DeleteRange(demoScoreDetails);
                _unitOfWork.GetRepository<Score>().DeleteRange(demoScores);
            }

            _unitOfWork.GetRepository<EventRole>().DeleteRange(demoRoles);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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
                AssignedAt = DateTime.UtcNow,
                ExpiredAt = expiredAt
            };
            await _unitOfWork.GetRepository<EventRole>().AddAsync(role);
        }
    }
}



