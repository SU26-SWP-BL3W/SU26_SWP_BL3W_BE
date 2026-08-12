using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.UpdateTeam
{
    public class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
    {
        public UpdateTeamCommandValidator()
        {
            RuleFor(x => x.Model.Id)
                .NotEmpty().WithMessage("Mã nhóm (Id) không được để trống.");

            RuleFor(x => x.Model.Name)
                .NotEmpty().WithMessage("Tên nhóm không được để trống.")
                .MaximumLength(150).WithMessage("Tên nhóm không vượt quá 150 ký tự.");

            RuleFor(x => x.Model.Description)
                .MaximumLength(500).WithMessage("Mô tả không vượt quá 500 ký tự.");
        }
    }
}
