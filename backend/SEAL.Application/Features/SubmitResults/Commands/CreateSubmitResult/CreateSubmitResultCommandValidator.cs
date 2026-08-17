using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult
{
    public class CreateSubmitResultCommandValidator : AbstractValidator<CreateSubmitResultCommand>
    {
        public CreateSubmitResultCommandValidator()
        {
            RuleFor(x => x.Model.TeamId)
                .NotEmpty().WithMessage("Mã nhóm nộp bài (TeamId) không được để trống.");

            RuleFor(x => x.Model.TrackId)
                .NotEmpty().WithMessage("Mã Track (TrackId) không được để trống.");

            RuleFor(x => x.Model.RoundId)
                .NotEmpty().WithMessage("Mã vòng thi (RoundId) không được để trống.");

            RuleFor(x => x.Model)
                .Must(m => !string.IsNullOrWhiteSpace(m.RepoUrl) || !string.IsNullOrWhiteSpace(m.SubmissionUrl))
                .WithMessage("Cần đường dẫn GitHub/GitLab (RepoUrl).")
                .OverridePropertyName("Model.RepoUrl");

            RuleFor(x => x.Model.DemoUrl)
                .NotEmpty().WithMessage("Cần đường dẫn demo (DemoUrl).")
                .MaximumLength(2000);

            RuleFor(x => x.Model.SlideUrl)
                .NotEmpty().WithMessage("Cần đường dẫn slide (SlideUrl).")
                .MaximumLength(2000);

            RuleFor(x => x.Model.SubmissionUrl)
                .MaximumLength(2000).WithMessage("Đường dẫn bài nộp không vượt quá 2000 ký tự.");

            RuleFor(x => x.Model.Description)
                .MaximumLength(1000).WithMessage("Mô tả bài nộp không vượt quá 1000 ký tự.");
        }

    }
}
