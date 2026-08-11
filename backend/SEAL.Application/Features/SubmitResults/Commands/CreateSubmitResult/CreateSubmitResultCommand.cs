using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult
{
    public class CreateSubmitResultCommand : IRequest<Result<CreateSubmitResultResponseModel>>
    {
        public CreateSubmitResultRequestModel Model { get; set; } = new CreateSubmitResultRequestModel();
    }
}

