using SEAL_Domain.Base;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.DeleteSubmitResult
{
    public class DeleteSubmitResultCommand : IRequest<Result<bool>>
    {
        public string Id { get; set; }

        public DeleteSubmitResultCommand(string id)
        {
            Id = id;
        }
    }
}

