using MediatR;
using SEAL_Domain.Base;
using System;

namespace SEAL_Application.Features.Demo.Commands.SetupDemoEvents
{
    public class SetupDemoEventsCommand : IRequest<Result<BaseResponse<bool>>>
    {
        public DateTime TargetDate { get; set; }
    }
}

