using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Tracks.Commands.CreateTrack.Models;

namespace SEAL_Application.Features.Tracks.Commands.CreateTrack
{
    public class CreateTrackCommand : IRequest<Result<CreateTrackResponseModel>>
    {
        public CreateTrackRequestModel Model { get; set; } = new CreateTrackRequestModel();
    }
}

