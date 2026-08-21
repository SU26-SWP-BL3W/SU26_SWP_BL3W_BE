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

namespace SEAL_Application.Features.Demo.Commands.SetupFullEventDemo
{
    public class SetupFullEventDemoCommandHandler : IRequestHandler<SetupFullEventDemoCommand, Result<BaseResponse<object>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SetupFullEventDemoCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<BaseResponse<object>>> Handle(SetupFullEventDemoCommand request, CancellationToken cancellationToken)
        {
            var targetDate = request.TargetDate.ToUniversalTime();

            // 1. Dọn dẹp dữ liệu Demo Full cũ (nếu có)
            var oldEvents = await _unitOfWork.GetRepository<Event>().Entities
                .Where(e => e.EventName.StartsWith("[DEMO FULL]"))
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

                var oldPrizes = await _unitOfWork.GetRepository<Prize>().Entities
                    .Where(p => oldEventIds.Contains(p.EventId))
                    .ToListAsync(cancellationToken);
                if (oldPrizes.Any()) _unitOfWork.GetRepository<Prize>().DeleteRange(oldPrizes);

                _unitOfWork.GetRepository<Event>().DeleteRange(oldEvents);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 2. Trường học (School)
            var school = await _unitOfWork.GetRepository<School>().Entities
                .FirstOrDefaultAsync(s => s.SchoolName == "FPT University", cancellationToken);
            if (school == null)
            {
                school = new School
                {
                    SchoolName = "FPT University",
                    Address = "Khu Công Nghệ Cao Hòa Lạc, Hà Nội"
                };
                await _unitOfWork.GetRepository<School>().AddAsync(school);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3. Mẫu tiêu chí (Template & Criteria) chuẩn 100%
            var template = await _unitOfWork.GetRepository<Template>().Entities
                .FirstOrDefaultAsync(t => t.TemplateName == "[DEMO] Chuẩn Đánh Giá Hackathon 2026", cancellationToken);
            if (template == null)
            {
                template = new Template
                {
                    TemplateName = "[DEMO] Chuẩn Đánh Giá Hackathon 2026",
                    Description = "Khung tiêu chuẩn đánh giá dự án lập trình Hackathon chuẩn quốc tế."
                };
                await _unitOfWork.GetRepository<Template>().AddAsync(template);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var criteria1 = new Criteria { CriteriaName = "Ý tưởng & Tính sáng tạo", Description = "Tính mới, tính đột phá và sự độc đáo của giải pháp", IsActive = true };
                var criteria2 = new Criteria { CriteriaName = "Chất lượng kỹ thuật & Kiến trúc", Description = "Kiến trúc hệ thống, độ hoàn thiện code, công nghệ áp dụng", IsActive = true };
                var criteria3 = new Criteria { CriteriaName = "Tính khả thi & Tác động thực tế", Description = "Giá trị xã hội, khả năng ứng dụng thực tiễn và nhân rộng", IsActive = true };
                var criteria4 = new Criteria { CriteriaName = "Kỹ năng thuyết trình & Demo", Description = "Trình bày trực quan, demo chạy mượt mà, phản biện sắc bén", IsActive = true };

                await _unitOfWork.GetRepository<Criteria>().AddRangeAsync(new[] { criteria1, criteria2, criteria3, criteria4 });
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var tcList = new List<TemplateCriteria>
                {
                    new TemplateCriteria { TemplateId = template.Id, CriteriaId = criteria1.Id, Weight = 30m, MaxScore = 10 },
                    new TemplateCriteria { TemplateId = template.Id, CriteriaId = criteria2.Id, Weight = 30m, MaxScore = 10 },
                    new TemplateCriteria { TemplateId = template.Id, CriteriaId = criteria3.Id, Weight = 20m, MaxScore = 10 },
                    new TemplateCriteria { TemplateId = template.Id, CriteriaId = criteria4.Id, Weight = 20m, MaxScore = 10 }
                };
                await _unitOfWork.GetRepository<TemplateCriteria>().AddRangeAsync(tcList);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var allCriteria = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                .Where(tc => tc.TemplateId == template.Id)
                .Include(tc => tc.Criteria)
                .ToListAsync(cancellationToken);

            // 4. Tạo Sự kiện Full (Event)
            var fullEvent = new Event
            {
                EventName = $"[DEMO FULL] FPT EDU HACKATHON 2026 - AI & SUSTAINABILITY",
                Season = "Summer 2026",
                Year = targetDate.Year,
                Description = "Cuộc thi lập trình công nghệ thường niên dành cho sinh viên FPT Edu với chủ đề Ứng dụng AI vào Phát triển Bền vững.",
                RegistrationStartDate = targetDate.AddDays(-30),
                RegistrationEndDate = targetDate.AddDays(-15),
                StartDate = targetDate.AddDays(-25),
                EndDate = targetDate.AddDays(25),
                Status = true,
                MaxTeams = 20,
                PhotoEventUrl = "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg"
            };
            await _unitOfWork.GetRepository<Event>().AddAsync(fullEvent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Cơ cấu giải thưởng (Prizes)
            var prize1 = new Prize { EventId = fullEvent.Id, PrizeName = "Giải Vô Địch (Champion)", Value = "100.000.000 VNĐ", Quantity = 1 };
            var prize2 = new Prize { EventId = fullEvent.Id, PrizeName = "Giải Nhì (First Runner-Up)", Value = "30.000.000 VNĐ", Quantity = 1 };
            var prize3 = new Prize { EventId = fullEvent.Id, PrizeName = "Giải Ba (Second Runner-Up)", Value = "15.000.000 VNĐ", Quantity = 1 };
            var prize4 = new Prize { EventId = fullEvent.Id, PrizeName = "Giải Sáng Tạo (Innovation Prize)", Value = "5.000.000 VNĐ", Quantity = 1 };
            await _unitOfWork.GetRepository<Prize>().AddRangeAsync(new[] { prize1, prize2, prize3, prize4 });

            // 6. Vòng thi (Rounds)
            var round1 = new Round
            {
                EventId = fullEvent.Id,
                RoundName = "Vòng Sơ Loại (Preliminary Round)",
                RoundNumber = 1,
                AdvancementRule = "top:3",
                StartDate = targetDate.AddDays(-25),
                EndDate = targetDate.AddDays(-5),
                ScoringStartDate = targetDate.AddDays(-4),
                // DEMO LIVE: mở cửa sổ chấm tới +7 ngày để giám khảo chấm trực tiếp
                ScoringEndDate = targetDate.AddDays(7),
                AppealStartDate = targetDate.AddDays(-1),
                AppealEndDate = targetDate.AddDays(3)
            };
            var round2 = new Round
            {
                EventId = fullEvent.Id,
                RoundName = "Vòng Chung Kết (Grand Final)",
                RoundNumber = 2,
                AdvancementRule = "top:1",
                StartDate = targetDate.AddDays(5),
                EndDate = targetDate.AddDays(20),
                ScoringStartDate = targetDate.AddDays(15),
                ScoringEndDate = targetDate.AddDays(19)
            };
            await _unitOfWork.GetRepository<Round>().AddRangeAsync(new[] { round1, round2 });

            // 7. Hạng mục (Tracks)
            var track1 = new Track
            {
                EventId = fullEvent.Id,
                TrackName = "AI & Machine Learning Track",
                Description = "Hạng mục dành cho các giải pháp ứng dụng Trí tuệ nhân tạo, Thị giác máy tính và Xử lý ngôn ngữ tự nhiên.",
                TemplateId = template.Id,
                SubmissionRuleDescription = "Nộp link GitHub Repo dự án (kèm file README.md, mã nguồn sạch) + Link Video Demo sản phẩm + Slide thuyết trình PDF.",
                StartDate = targetDate.AddDays(-25),
                EndDate = targetDate.AddDays(-5),
                ScoringStartDate = targetDate.AddDays(-4),
                ScoringEndDate = targetDate.AddDays(7) // DEMO LIVE: mở cửa sổ chấm tới +7 ngày để giám khảo chấm trực tiếp
            };
            var track2 = new Track
            {
                EventId = fullEvent.Id,
                TrackName = "Web & Mobile App Innovation Track",
                Description = "Hạng mục dành cho các ứng dụng Web/Mobile tối ưu hóa trải nghiệm người dùng và chuyển đổi xanh.",
                TemplateId = template.Id,
                SubmissionRuleDescription = "Nộp link GitHub Repo dự án + Link Live Website/App Demo + Slide thuyết trình PDF.",
                StartDate = targetDate.AddDays(-25),
                EndDate = targetDate.AddDays(-5),
                ScoringStartDate = targetDate.AddDays(-4),
                ScoringEndDate = targetDate.AddDays(7) // DEMO LIVE: mở cửa sổ chấm tới +7 ngày để giám khảo chấm trực tiếp
            };
            await _unitOfWork.GetRepository<Track>().AddRangeAsync(new[] { track1, track2 });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. Tạo / Lấy Users (EC, Giám khảo, Cố vấn, 25 Thí sinh)
            var ecUser = await GetOrCreateUserAsync("ec_full@yopmail.com", "Nguyễn Văn Điều Phối", false, false, school.Id, cancellationToken);
            var judge1 = await GetOrCreateUserAsync("judge1_full@yopmail.com", "TS. Trần Giám Khảo AI", false, false, school.Id, cancellationToken);
            var judge2 = await GetOrCreateUserAsync("judge2_full@yopmail.com", "ThS. Lê Giám Khảo Web", false, false, school.Id, cancellationToken);
            var mentor1 = await GetOrCreateUserAsync("mentor1_full@yopmail.com", "KSC. Phạm Cố Vấn ML", false, false, school.Id, cancellationToken);
            var mentor2 = await GetOrCreateUserAsync("mentor2_full@yopmail.com", "KSC. Hoàng Cố Vấn App", false, false, school.Id, cancellationToken);

            var studentUsers = new List<User>();
            string[] studentNames = new[]
            {
                "Nguyễn Trọng Anh", "Trần Bảo Bình", "Lê Cảnh Chung", "Phạm Đức Dũng", "Hoàng Gia Em",
                "Vũ Hải Giang", "Đặng Hữu Hùng", "Bùi Kim Khoa", "Đỗ Long Linh", "Hồ Minh Nam",
                "Ngô Nhật Nam", "Dương Phúc Phong", "Võ Quang Quân", "Trịnh Quốc Sơn", "Đoàn Thái Tâm",
                "Lý Thành Thắng", "Mai Tiến Triết", "Chu Trung Tuấn", "Phan Văn Vũ", "Tạ Xuân Yên",
                "Lương Gia Bảo", "Cao Minh Cường", "Trương Duy Đạt", "Đinh Hữu Hiếu", "Vương Khắc Kiên"
            };

            for (int i = 0; i < 25; i++)
            {
                var email = $"student{i + 1}@yopmail.com";
                var code = $"SE17{i + 1001:D4}";
                var user = await GetOrCreateUserAsync(email, studentNames[i], false, true, school.Id, cancellationToken, code, true, "https://s3.cloudfly.vn/rhymo-bucket/general/e188d675-d952-422f-a536-7bac6a1edc22.jpg");
                studentUsers.Add(user);
            }

            // 9. Gán Vai trò Sự kiện (EventRoles) cho Ban tổ chức / Chuyên gia
            var ecRole = new EventRole { UserId = ecUser.Id, EventId = fullEvent.Id, RoleName = EventRoleType.EventCoordinator, ExpiredAt = fullEvent.EndDate, Notes = "Trưởng ban điều phối sự kiện" };
            var judge1Role = new EventRole { UserId = judge1.Id, EventId = fullEvent.Id, TrackId = track1.Id, RoleName = EventRoleType.Judge, ExpiredAt = fullEvent.EndDate, Notes = "Giám khảo chính Track AI" };
            var judge2Role = new EventRole { UserId = judge2.Id, EventId = fullEvent.Id, TrackId = track2.Id, RoleName = EventRoleType.Judge, ExpiredAt = fullEvent.EndDate, Notes = "Giám khảo chính Track Web/App" };
            var mentor1Role = new EventRole { UserId = mentor1.Id, EventId = fullEvent.Id, TrackId = track1.Id, RoleName = EventRoleType.Mentor, ExpiredAt = fullEvent.EndDate, Notes = "Cố vấn kỹ thuật AI" };
            var mentor2Role = new EventRole { UserId = mentor2.Id, EventId = fullEvent.Id, TrackId = track2.Id, RoleName = EventRoleType.Mentor, ExpiredAt = fullEvent.EndDate, Notes = "Cố vấn phát triển sản phẩm" };

            await _unitOfWork.GetRepository<EventRole>().AddRangeAsync(new[] { ecRole, judge1Role, judge2Role, mentor1Role, mentor2Role });

            // 10. Tạo 5 Đội thi (Teams)
            var team1 = new Team { EventId = fullEvent.Id, TrackId = track1.Id, Name = "AI Titans", Description = "Dự án Phân loại rác thông minh tự động bằng AI", Status = TeamStatus.Registered };
            var team2 = new Team { EventId = fullEvent.Id, TrackId = track1.Id, Name = "Cyber Knights", Description = "Mô hình Dự báo và Cảnh báo Cháy rừng sớm", Status = TeamStatus.Registered };
            var team3 = new Team { EventId = fullEvent.Id, TrackId = track2.Id, Name = "Green Tech", Description = "Ứng dụng Chia sẻ phương tiện di chuyển xanh Eco-Transport", Status = TeamStatus.Registered };
            var team4 = new Team { EventId = fullEvent.Id, TrackId = track2.Id, Name = "Quantum Leap", Description = "Nền tảng Tối ưu năng lượng thông minh cho Tòa nhà", Status = TeamStatus.Registered };
            var team5 = new Team { EventId = fullEvent.Id, TrackId = track2.Id, Name = "Future Builders", Description = "Hệ thống IoT Quan trắc và Cảnh báo ô nhiễm nguồn nước", Status = TeamStatus.Registered };

            await _unitOfWork.GetRepository<Team>().AddRangeAsync(new[] { team1, team2, team3, team4, team5 });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 11. Gán Leader & Thành viên cho 5 Đội
            var teamStudentRoles = new List<EventRole>();
            var teams = new[] { team1, team2, team3, team4, team5 };

            for (int t = 0; t < 5; t++)
            {
                var curTeam = teams[t];
                // Leader: sinh viên đầu tiên của nhóm
                teamStudentRoles.Add(new EventRole
                {
                    UserId = studentUsers[t * 5].Id,
                    EventId = fullEvent.Id,
                    TeamId = curTeam.Id,
                    RoleName = EventRoleType.TeamLeader,
                    ExpiredAt = fullEvent.EndDate
                });

                // 4 Members còn lại
                for (int m = 1; m < 5; m++)
                {
                    teamStudentRoles.Add(new EventRole
                    {
                        UserId = studentUsers[t * 5 + m].Id,
                        EventId = fullEvent.Id,
                        TeamId = curTeam.Id,
                        RoleName = EventRoleType.TeamMember,
                        ExpiredAt = fullEvent.EndDate
                    });
                }
            }
            await _unitOfWork.GetRepository<EventRole>().AddRangeAsync(teamStudentRoles);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 12. Tạo Bài Nộp (SubmitResults) Vòng Sơ Loại cho cả 5 Đội
            var submit1 = new SubmitResult
            {
                TeamId = team1.Id,
                TrackId = track1.Id,
                RoundId = round1.Id,
                SubmissionUrl = "https://github.com/ai-titans/smart-recycling-ai",
                RepoUrl = "https://github.com/ai-titans/smart-recycling-ai",
                DemoUrl = "https://ai-titans.vercel.app",
                SlideUrl = "https://docs.google.com/presentation/d/ai-titans-pitch",
                Description = "Hệ thống phân loại rác thông minh tự động sử dụng Computer Vision (YOLOv8) kết hợp Edge AI.",
                IsActive = true,
                CreatedBy = studentUsers[0].Id,
                CreatedTime = targetDate.AddDays(-10)
            };
            var submit2 = new SubmitResult
            {
                TeamId = team2.Id,
                TrackId = track1.Id,
                RoundId = round1.Id,
                SubmissionUrl = "https://github.com/cyber-knights/wildfire-detection-ai",
                RepoUrl = "https://github.com/cyber-knights/wildfire-detection-ai",
                DemoUrl = "https://wildfire-ai.dev",
                SlideUrl = "https://docs.google.com/presentation/d/wildfire-pitch",
                Description = "Mô hình dự báo nguy cơ cháy rừng sớm qua ảnh vệ tinh đa phổ và mạng cảm biến IoT.",
                IsActive = true,
                CreatedBy = studentUsers[5].Id,
                CreatedTime = targetDate.AddDays(-9)
            };
            var submit3 = new SubmitResult
            {
                TeamId = team3.Id,
                TrackId = track2.Id,
                RoundId = round1.Id,
                SubmissionUrl = "https://github.com/green-tech/eco-smart-transport",
                RepoUrl = "https://github.com/green-tech/eco-smart-transport",
                DemoUrl = "https://greentech-fpt.web.app",
                SlideUrl = "https://docs.google.com/presentation/d/greentech-pitch",
                Description = "Ứng dụng chia sẻ phương tiện di chuyển xanh trong khuôn viên trường học và khu đô thị thông minh.",
                IsActive = true,
                CreatedBy = studentUsers[10].Id,
                CreatedTime = targetDate.AddDays(-8)
            };
            var submit4 = new SubmitResult
            {
                TeamId = team4.Id,
                TrackId = track2.Id,
                RoundId = round1.Id,
                SubmissionUrl = "https://github.com/quantum-leap/energy-monitor-hub",
                RepoUrl = "https://github.com/quantum-leap/energy-monitor-hub",
                DemoUrl = "https://energyhub.io",
                SlideUrl = "https://docs.google.com/presentation/d/quantum-pitch",
                Description = "Nền tảng IoT & Web Dashboard giám sát và tối ưu hóa mức tiêu thụ năng lượng theo thời gian thực.",
                IsActive = true,
                CreatedBy = studentUsers[15].Id,
                CreatedTime = targetDate.AddDays(-7)
            };
            var submit5 = new SubmitResult
            {
                TeamId = team5.Id,
                TrackId = track2.Id,
                RoundId = round1.Id,
                SubmissionUrl = "https://github.com/future-builders/clean-water-iot",
                RepoUrl = "https://github.com/future-builders/clean-water-iot",
                DemoUrl = "https://cleanwater-iot.net",
                SlideUrl = "https://docs.google.com/presentation/d/cleanwater-pitch",
                Description = "Hệ thống quan trắc và cảnh báo sớm mức độ ô nhiễm nguồn nước ngầm phục vụ nông nghiệp công nghệ cao.",
                IsActive = true,
                CreatedBy = studentUsers[20].Id,
                CreatedTime = targetDate.AddDays(-6)
            };

            await _unitOfWork.GetRepository<SubmitResult>().AddRangeAsync(new[] { submit1, submit2, submit3, submit4, submit5 });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 13. Nhận xét Cố Vấn (MentorFeedback)
            var fb1 = new MentorFeedback { SubmitResultId = submit1.Id, MentorId = mentor1.Id, Comment = "Ý tưởng phân loại rác rất thiết thực, mô hình inference nhanh. Cần tối ưu thêm độ chính xác khi thiếu sáng." };
            var fb2 = new MentorFeedback { SubmitResultId = submit2.Id, MentorId = mentor1.Id, Comment = "Độ phủ dữ liệu vệ tinh tốt, đề xuất tích hợp thêm dữ liệu gió và độ ẩm không khí thời gian thực." };
            var fb3 = new MentorFeedback { SubmitResultId = submit3.Id, MentorId = mentor2.Id, Comment = "UX ứng dụng mượt mà, tính năng đặt xe trực quan. Cần bổ sung thuật toán gom chuyến thông minh." };
            var fb4 = new MentorFeedback { SubmitResultId = submit4.Id, MentorId = mentor2.Id, Comment = "Dashboard dữ liệu đẹp và chi tiết, nên bổ sung dự báo AI cho các khung giờ cao điểm." };
            var fb5 = new MentorFeedback { SubmitResultId = submit5.Id, MentorId = mentor2.Id, Comment = "Phần cứng IoT hoạt động ổn định, cần bổ sung cơ chế cảnh báo khẩn cấp qua SMS/Email." };
            await _unitOfWork.GetRepository<MentorFeedback>().AddRangeAsync(new[] { fb1, fb2, fb3, fb4, fb5 });

            // 14. Chấm điểm Giám khảo (Scores & ScoreDetails)
            var score1 = new Score { EventRoleId = judge1Role.Id, SubmitResultId = submit1.Id, TotalScore = 9.25m, Comment = "Dự án xuất sắc toàn diện! AI chạy tốt, demo thực tế rất ấn tượng.", IsSubmitted = true };
            var score2 = new Score { EventRoleId = judge1Role.Id, SubmitResultId = submit2.Id, TotalScore = 8.50m, Comment = "Giải pháp có ý nghĩa bảo vệ môi trường cao, kiến trúc kỹ thuật tương đối hoàn thiện.", IsSubmitted = true };
            var score3 = new Score { EventRoleId = judge2Role.Id, SubmitResultId = submit3.Id, TotalScore = 9.00m, Comment = "Sản phẩm hoàn thiện cao, giao diện đẹp và tính khả thi áp dụng thực tế rất lớn.", IsSubmitted = true };
            var score4 = new Score { EventRoleId = judge2Role.Id, SubmitResultId = submit4.Id, TotalScore = 8.25m, Comment = "Hệ thống vận hành tốt, giải quyết tốt bài toán năng lượng.", IsSubmitted = true };
            // DEMO LIVE: CHỪA submit5 (đội Future Builders) CHƯA chấm — để giám khảo judge2_full chấm TRỰC TIẾP trước mặt thầy.

            await _unitOfWork.GetRepository<Score>().AddRangeAsync(new[] { score1, score2, score3, score4 });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Tạo chi tiết điểm (ScoreDetails) theo 4 tiêu chí
            var allScores = new[]
            {
                (score1, new[] { 9.5m, 9.0m, 9.5m, 9.0m }),
                (score2, new[] { 8.5m, 8.5m, 8.5m, 8.5m }),
                (score3, new[] { 9.0m, 9.0m, 9.0m, 9.0m }),
                (score4, new[] { 8.5m, 8.0m, 8.5m, 8.0m })
            };

            var scoreDetailsList = new List<ScoreDetail>();
            foreach (var (score, values) in allScores)
            {
                for (int i = 0; i < allCriteria.Count && i < values.Length; i++)
                {
                    scoreDetailsList.Add(new ScoreDetail
                    {
                        ScoreId = score.Id,
                        TemplateId = template.Id,
                        CriteriaId = allCriteria[i].CriteriaId,
                        Value = values[i]
                    });
                }
            }
            await _unitOfWork.GetRepository<ScoreDetail>().AddRangeAsync(scoreDetailsList);

            // 15. DEMO LIVE: KHÔNG tạo sẵn FinalResults. Lý do bắt buộc: nếu vòng đã có FinalResult,
            //     SaveScore sẽ KHÓA chấm điểm (roundPublished) -> giám khảo không chấm live được.
            //     Để EC tự bấm "TÍNH ĐIỂM TỰ ĐỘNG" + "CÔNG BỐ KẾT QUẢ" trước mặt thầy (sau khi đóng vòng).

            // 16. Đơn Phúc khảo (Appeal)
            var appeal = new Appeal
            {
                TeamId = team4.Id,
                SubmitResultId = submit4.Id,
                Reason = "Đội Quantum Leap xin phúc khảo tiêu chí Kiến trúc Kỹ thuật do có phần kiểm thử hiệu năng benchmark đã nộp nhưng chưa kịp trình diễn trong lúc demo.",
                Status = AppealStatus.Approved,
                AssignedJudgeId = judge2.Id,
                Response = "Ban Giám khảo đã hội ý, rà soát lại phần benchmark hiệu năng và thống nhất cộng 0.25 điểm cho tiêu chí Kỹ thuật. Ghi nhận tinh thần cầu thị của đội!"
            };
            await _unitOfWork.GetRepository<Appeal>().AddAsync(appeal);

            // 17. Thông báo hệ thống (AppNotification)
            var notifications = new List<AppNotification>
            {
                new AppNotification { UserId = ecUser.Id, Title = "Sự kiện đã hoàn tất chấm điểm", Message = "Vòng Sơ loại đã hoàn tất nhập điểm và xếp hạng cho 5 đội thi.", Type = "event", IsRead = false },
                new AppNotification { UserId = studentUsers[0].Id, Title = "Chúc mừng Đội AI Titans!", Message = "Đội bạn đã xuất sắc đạt Hạng 1 Vòng Sơ loại với điểm số 9.25 và giành quyền vào Vòng Chung kết.", Type = "result", IsRead = false },
                new AppNotification { UserId = studentUsers[15].Id, Title = "Đơn phúc khảo đã được xử lý", Message = "Đơn phúc khảo của đội Quantum Leap đã được Ban Giám khảo phản hồi chấp thuận.", Type = "appeal", IsRead = true }
            };
            await _unitOfWork.GetRepository<AppNotification>().AddRangeAsync(notifications);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var summary = new
            {
                Event = new { fullEvent.Id, fullEvent.EventName, fullEvent.Season, fullEvent.Year, fullEvent.StartDate, fullEvent.EndDate },
                Prizes = new[] { prize1.PrizeName, prize2.PrizeName, prize3.PrizeName, prize4.PrizeName },
                Rounds = new[] { round1.RoundName, round2.RoundName },
                Tracks = new[] { track1.TrackName, track2.TrackName },
                Accounts = new
                {
                    EventCoordinator = "ec_full@yopmail.com (Pass: 123456)",
                    Judges = new[] { "judge1_full@yopmail.com (Pass: 123456)", "judge2_full@yopmail.com (Pass: 123456)" },
                    Mentors = new[] { "mentor1_full@yopmail.com (Pass: 123456)", "mentor2_full@yopmail.com (Pass: 123456)" },
                    Students = "student1@yopmail.com đến student25@yopmail.com (Pass: 123456)"
                },
                Teams = new[] { "AI Titans (Hạng 1 - 9.25)", "Green Tech (Hạng 2 - 9.00)", "Cyber Knights (Hạng 3 - 8.50)", "Quantum Leap (Hạng 4 - 8.25)", "Future Builders (Hạng 5 - 7.80)" },
                Appeals = new[] { "Đơn phúc khảo của đội Quantum Leap (Đã xử lý Approved)" }
            };

            return BaseResponse<object>.OkResponse(summary, "Đã khởi tạo thành công Full 1 Sự kiện Hackathon chuẩn chỉnh từ A-Z với đầy đủ tất cả thực thể và dữ liệu liên quan.");
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
                if (!string.IsNullOrEmpty(studentCode)) user.StudentCode = studentCode;
                _unitOfWork.GetRepository<User>().Update(user);
            }
            await _unitOfWork.SaveChangesAsync(ct);
            return user;
        }
    }
}
