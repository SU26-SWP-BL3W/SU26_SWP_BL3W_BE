using FluentValidation;
using SEAL_Application.Features.Events.Commands.CreateEvent.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Application.Interfaces;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateEventCommandValidator(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu sự kiện không được để trống");

            RuleFor(x => x.Model.EventName)
                .NotEmpty().WithMessage("Tên sự kiện không được để trống")
                .MaximumLength(255).WithMessage("Tên sự kiện không được vượt quá 255 ký tự")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Season)
                .MaximumLength(100).WithMessage("Season không được vượt quá 100 ký tự")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Season));

            RuleFor(x => x.Model.Year)
                .GreaterThan(2000).WithMessage("Năm tổ chức phải lớn hơn 2000")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Description));

            RuleFor(x => x.Model)
                .Must(m => m.StartDate < m.EndDate).WithMessage("Ngày bắt đầu sự kiện phải nhỏ hơn ngày kết thúc.")
                .When(x => x.Model != null);

            // Kiểm tra thời gian đăng ký (nếu có)
            RuleFor(x => x.Model)
                .Must(m => m.RegistrationStartDate < m.RegistrationEndDate)
                .WithMessage("Ngày bắt đầu đăng ký phải nhỏ hơn ngày kết thúc đăng ký.")
                .When(x => x.Model != null && x.Model.RegistrationStartDate.HasValue && x.Model.RegistrationEndDate.HasValue);

            RuleFor(x => x.Model)
                .Must(m => m.RegistrationEndDate <= m.EndDate)
                .WithMessage("Thời gian đăng ký phải kết thúc trước hoặc cùng lúc với thời gian kết thúc sự kiện.")
                .When(x => x.Model != null && x.Model.RegistrationEndDate.HasValue);

            RuleFor(x => x.Model)
                .Must(m => !m.RegistrationStartDate.HasValue || !m.RegistrationEndDate.HasValue || (m.RegistrationStartDate.HasValue && m.RegistrationEndDate.HasValue))
                .WithMessage("Vui lòng nhập đầy đủ cả thời gian bắt đầu và kết thúc đăng ký nếu bạn muốn mở đăng ký.");

            RuleFor(x => x.Model.MaxTeams)
                .GreaterThan(0).WithMessage("Số đội tối đa phải lớn hơn 0")
                .When(x => x.Model != null);

            // Validate ngày của Round so với Event ở mức Validator cha (tránh lỗi DI)
            RuleForEach(x => x.Model.Rounds)
                .Must((command, round) => round.StartDate < round.EndDate)
                .WithMessage((command, round) => $"Vòng '{round.RoundName}': Ngày bắt đầu phải nhỏ hơn ngày kết thúc.")
                .Must((command, round) => round.StartDate >= command.Model.StartDate)
                .WithMessage((command, round) => $"Vòng '{round.RoundName}': Ngày bắt đầu phải nằm trong thời gian diễn ra sự kiện (từ {command.Model.StartDate:dd/MM/yyyy}).")
                .Must((command, round) => round.EndDate <= command.Model.EndDate)
                .WithMessage((command, round) => $"Vòng '{round.RoundName}': Ngày kết thúc không được vượt quá ngày kết thúc sự kiện (đến {command.Model.EndDate:dd/MM/yyyy}).")
                .Must((command, round) => !round.ScoringStartDate.HasValue || round.ScoringStartDate.Value <= command.Model.EndDate)
                .WithMessage((command, round) => $"Vòng '{round.RoundName}': Thời gian bắt đầu chấm không được vượt quá ngày kết thúc sự kiện (đến {command.Model.EndDate:dd/MM/yyyy}).")
                .Must((command, round) => !round.ScoringEndDate.HasValue || round.ScoringEndDate.Value <= command.Model.EndDate)
                .WithMessage((command, round) => $"Vòng '{round.RoundName}': Hạn chấm không được vượt quá ngày kết thúc sự kiện (đến {command.Model.EndDate:dd/MM/yyyy}).")
                .When(x => x.Model != null && x.Model.Rounds != null && x.Model.Rounds.Any());

            // Validate Track dates against Event dates
            RuleFor(x => x.Model)
                .Custom((model, context) =>
                {
                    if (model == null || model.Rounds == null) return;
                    foreach (var round in model.Rounds)
                    {
                        if (round.Tracks == null) continue;
                        foreach (var track in round.Tracks)
                        {
                            if (track.StartDate.HasValue && track.EndDate.HasValue && track.StartDate.Value >= track.EndDate.Value)
                                context.AddFailure($"Vòng '{round.RoundName}', Hạng mục '{track.TrackName}': Ngày bắt đầu nộp bài phải trước hạn nộp bài.");
                            
                            if (track.StartDate.HasValue && track.StartDate.Value < model.StartDate)
                                context.AddFailure($"Vòng '{round.RoundName}', Hạng mục '{track.TrackName}': Ngày bắt đầu nộp bài không được trước ngày bắt đầu sự kiện.");
                            
                            if (track.EndDate.HasValue && track.EndDate.Value > model.EndDate)
                                context.AddFailure($"Vòng '{round.RoundName}', Hạng mục '{track.TrackName}': Hạn nộp bài không được vượt quá ngày kết thúc sự kiện.");
                            
                            if (track.ScoringStartDate.HasValue && track.EndDate.HasValue && track.ScoringStartDate.Value < track.EndDate.Value)
                                context.AddFailure($"Vòng '{round.RoundName}', Hạng mục '{track.TrackName}': Thời gian bắt đầu chấm phải sau hoặc cùng lúc với hạn nộp bài.");
                            
                            if (track.ScoringStartDate.HasValue && track.ScoringStartDate.Value > model.EndDate)
                                context.AddFailure($"Vòng '{round.RoundName}', Hạng mục '{track.TrackName}': Thời gian bắt đầu chấm không được vượt quá ngày kết thúc sự kiện.");

                            if (track.ScoringEndDate.HasValue && track.ScoringEndDate.Value > model.EndDate)
                                context.AddFailure($"Vòng '{round.RoundName}', Hạng mục '{track.TrackName}': Hạn chấm không được vượt quá ngày kết thúc sự kiện.");
                            
                            if (track.ScoringEndDate.HasValue && track.ScoringEndDate.Value <= (track.ScoringStartDate ?? track.EndDate ?? DateTime.MinValue))
                                context.AddFailure($"Vòng '{round.RoundName}', Hạng mục '{track.TrackName}': Hạn chót chấm phải sau thời điểm bắt đầu chấm.");
                        }
                    }
                })
                .When(x => x.Model != null);

            // Kiểm tra trùng tên sự kiện trong cùng 1 năm
            RuleFor(x => x.Model)
                .MustAsync(async (model, cancellationToken) =>
                {
                    if (model == null) return true;
                    var isDuplicate = await _unitOfWork.GetRepository<Event>().AnyAsync(
                        e => e.EventName.ToLower() == model.EventName.ToLower() && e.Year == model.Year,
                        cancellationToken);
                    return !isDuplicate;
                })
                .WithMessage(x => $"Sự kiện '{x.Model?.EventName}' trong năm {x.Model?.Year} đã tồn tại.")
                .When(x => x.Model != null);

            // Kiểm tra quyền của người dùng hiện tại (Admin hoặc EventCoordinator từ trước)
            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var currentUserId = _currentUserService.UserId;
                    if (string.IsNullOrEmpty(currentUserId)) return false;

                    var currentUser = await _unitOfWork.GetRepository<User>().GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

                    if (currentUser == null) return false;
                    if (currentUser.IsAdmin) return true;

                    var now = System.DateTime.UtcNow;
                    var isCoordinator = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                        .AsNoTracking()
                        .AnyAsync(er => er.UserId == currentUserId
                                     && er.RoleName == EventRoleType.EventCoordinator
                                     && (er.ExpiredAt == null || er.ExpiredAt > now),
                            cancellationToken);

                    return isCoordinator;
                })
                .WithMessage("Chỉ người dùng có vai trò Điều phối viên (Event Coordinator) từ trước hoặc Admin mới có quyền tạo sự kiện.");

            RuleForEach(x => x.Model.Rounds)
                .SetValidator(new RoundRequestDtoValidator(_unitOfWork))
                .When(x => x.Model != null && x.Model.Rounds != null && x.Model.Rounds.Any());
        }
    }

    public class RoundRequestDtoValidator : AbstractValidator<RoundRequestDto>
    {
        public RoundRequestDtoValidator(IUnitOfWork unitOfWork)
        {
            RuleFor(x => x.RoundName)
                .NotEmpty().WithMessage("Tên vòng thi không được để trống")
                .MaximumLength(255).WithMessage("Tên vòng thi không được vượt quá 255 ký tự");

            RuleFor(x => x.RoundNumber)
                .GreaterThan(0).WithMessage("Số thứ tự vòng thi phải lớn hơn 0");

            RuleFor(x => x.AdvancementRule)
                .MaximumLength(1000).WithMessage("Luật thăng vòng không được vượt quá 1000 ký tự")
                .Matches(@"^(top|percent|minScore)\s*:\s*\d+(\.\d+)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                .WithMessage("Luật thăng vòng phải có dạng 'top:N', 'percent:P' hoặc 'minScore:X' (ví dụ: top:3).")
                .When(x => !string.IsNullOrEmpty(x.AdvancementRule));

            // Validate scoring dates local constraints
            RuleFor(x => x)
                .Must(r => !r.ScoringStartDate.HasValue || r.ScoringStartDate.Value >= r.EndDate)
                .WithMessage(r => $"Vòng '{r.RoundName}': Thời gian bắt đầu chấm phải sau khi kết thúc nộp bài.")
                .Must(r => !r.ScoringEndDate.HasValue || r.ScoringEndDate.Value > (r.ScoringStartDate ?? r.EndDate))
                .WithMessage(r => $"Vòng '{r.RoundName}': Hạn chót chấm phải sau thời điểm bắt đầu chấm.");

            // Validate Tracks
            RuleFor(x => x.Tracks)
                .NotEmpty().WithMessage(r => $"Vòng '{r.RoundName}' phải cấu hình ít nhất một hạng mục (Track).");

            // Độc nhất tên Track trong cùng một Round
            RuleFor(x => x.Tracks)
                .Must(tracks =>
                {
                    if (tracks == null) return true;
                    var names = tracks.Select(t => t.TrackName.Trim().ToLower()).ToList();
                    return names.Distinct().Count() == names.Count;
                })
                .WithMessage(r => $"Vòng '{r.RoundName}' có các Hạng mục (Track) bị trùng tên nhau.");

            RuleForEach(x => x.Tracks)
                .SetValidator(new TrackRequestDtoValidator(unitOfWork));
        }
    }

    public class TrackRequestDtoValidator : AbstractValidator<TrackRequestDto>
    {
        public TrackRequestDtoValidator(IUnitOfWork unitOfWork)
        {
            RuleFor(x => x.TrackName)
                .NotEmpty().WithMessage("Tên hạng mục không được để trống")
                .MaximumLength(255).WithMessage("Tên hạng mục không được vượt quá 255 ký tự");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả hạng mục không được vượt quá 1000 ký tự");

            RuleFor(x => x.SubmissionRuleDescription)
                .MaximumLength(2000).WithMessage("Mô tả quy định nộp bài không được vượt quá 2000 ký tự");



            // Kiểm tra TemplateId có tồn tại trong CSDL hay không
            RuleFor(x => x.TemplateId)
                .MustAsync(async (templateId, cancellationToken) =>
                {
                    if (string.IsNullOrEmpty(templateId)) return true;
                    return await unitOfWork.GetRepository<Template>().AnyAsync(t => t.Id == templateId, cancellationToken);
                })
                .WithMessage(t => $"Mẫu tiêu chí chấm điểm (TemplateId: {t.TemplateId}) cho hạng mục '{t.TrackName}' không tồn tại trong hệ thống.");
        }
    }
}
