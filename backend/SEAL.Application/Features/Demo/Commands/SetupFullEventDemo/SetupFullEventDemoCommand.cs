using MediatR;
using SEAL_Domain.Base;
using System;

namespace SEAL_Application.Features.Demo.Commands.SetupFullEventDemo
{
    public class SetupFullEventDemoCommand : IRequest<Result<BaseResponse<object>>>
    {
        public DateTime TargetDate { get; set; } = DateTime.UtcNow;
    }
}
