using MediatR;
using SEAL_Application.Features.Scores.Queries.GetTrackCalibration.Models;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Scores.Queries.GetTrackCalibration
{
    public class GetTrackCalibrationQuery : IRequest<Result<TrackCalibrationModel>>
    {
        public string TrackId { get; set; } = string.Empty;

        public GetTrackCalibrationQuery(string trackId)
        {
            TrackId = trackId;
        }
    }
}
