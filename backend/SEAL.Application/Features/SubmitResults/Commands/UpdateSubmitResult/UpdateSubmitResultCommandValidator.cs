using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult
{
    public class UpdateSubmitResultCommandValidator : AbstractValidator<UpdateSubmitResultCommand>
    {
        public UpdateSubmitResultCommandValidator()
        {
            RuleFor(x => x.Model.Id)
                .NotEmpty().WithMessage("Mã bài nộp (Id) không được để trống.");

            RuleFor(x => x.Model.SubmissionUrl)
                .MaximumLength(2000).WithMessage("Đường dẫn bài nộp không vượt quá 2000 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Model.SubmissionUrl));

            RuleFor(x => x.Model.RepoUrl)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.Model.RepoUrl));
            RuleFor(x => x.Model.DemoUrl)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.Model.DemoUrl));
            RuleFor(x => x.Model.SlideUrl)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.Model.SlideUrl));

            RuleFor(x => x.Model.Description)
                .MaximumLength(1000).WithMessage("Mô tả bài nộp không vượt quá 1000 ký tự.");
        }


    }
}
