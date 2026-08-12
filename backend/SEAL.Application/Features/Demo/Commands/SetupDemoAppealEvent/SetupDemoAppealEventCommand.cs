using MediatR;
using SEAL_Domain.Base;
using System;

namespace SEAL_Application.Features.Demo.Commands.SetupDemoAppealEvent
{
    public class SetupDemoAppealEventCommand : IRequest<Result<BaseResponse<bool>>>
    {
        public DateTime TargetDate { get; set; }
    }
}

