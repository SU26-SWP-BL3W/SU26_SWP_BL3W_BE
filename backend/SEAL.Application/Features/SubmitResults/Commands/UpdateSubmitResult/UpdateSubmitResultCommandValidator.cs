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
                .When(x => !string.IsNullOrWhiteSpace(x.Model.SubmissionUrl)); // rỗng = giữ URL cũ (handler bỏ qua)

            RuleFor(x => x.Model.Description)
                .MaximumLength(1000).WithMessage("Mô tả bài nộp không vượt quá 1000 ký tự.");
        }


    }
}
