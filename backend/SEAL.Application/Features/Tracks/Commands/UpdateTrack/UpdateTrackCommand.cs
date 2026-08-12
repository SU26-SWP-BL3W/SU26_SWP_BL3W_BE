using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Features.Tracks.Commands.UpdateTrack.Models;

namespace SEAL_Application.Features.Tracks.Commands.UpdateTrack
{
    public class UpdateTrackCommand : IRequest<Result<UpdateTrackResponseModel>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateTrackRequestModel Model { get; set; } = new UpdateTrackRequestModel();
    }
}

