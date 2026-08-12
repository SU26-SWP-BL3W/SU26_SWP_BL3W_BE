using FluentValidation;

namespace SEAL_Application.Features.Events.Commands.UpdateEvent
{
    public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
    {
        public UpdateEventCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID sự kiện không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu cập nhật không được để trống");

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
                .Must(m => m.StartDate < m.EndDate).WithMessage("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.")
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
        }
    }
}
