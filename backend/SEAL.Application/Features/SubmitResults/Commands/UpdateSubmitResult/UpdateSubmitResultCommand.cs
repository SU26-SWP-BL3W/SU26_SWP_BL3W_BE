using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult
{
    public class UpdateSubmitResultCommand : IRequest<Result<UpdateSubmitResultResponseModel>>
    {
        public string Id { get; set; } = string.Empty; 
        public UpdateSubmitResultRequestModel Model { get; set; } = new UpdateSubmitResultRequestModel();
    }
}
