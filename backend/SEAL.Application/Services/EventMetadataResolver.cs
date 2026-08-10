using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading.Tasks;

namespace SEAL_Application.Services
{
    public class EventMetadataResolver : IEventMetadataResolver
    {
        private readonly IUnitOfWork _unitOfWork;

        public EventMetadataResolver(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(string? EventId, string? TrackId)> ResolveFromEntityAsync(string controllerName, string id)
        {
            string? eventId = null;
            string? trackId = null;

            if (string.IsNullOrEmpty(id)) return (null, null);

            switch (controllerName)
            {
                case "Events":
                    eventId = id;
                    break;

                case "Rounds":
                    var round = await _unitOfWork.GetRepository<Round>().GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == id);
                    eventId = round?.EventId;
                    break;

                case "Tracks":
                    var track = await _unitOfWork.GetRepository<Track>().GetQueryable()
                        .AsNoTracking()
                        .Include(t => t.Round)
                        .FirstOrDefaultAsync(t => t.Id == id);
                    if (track != null)
                    {
                        trackId = track.Id;
                        eventId = track.Round?.EventId;
                    }
                    break;

                case "EventRoles":
                    var eventRole = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(er => er.Id == id);
                    eventId = eventRole?.EventId;
                    trackId = eventRole?.TrackId;
                    break;

                case "Teams":
                    var team = await _unitOfWork.GetRepository<Team>().GetQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == id);
                    eventId = team?.EventId;
                    break;

                case "SubmitResults":
                    var submitResult = await _unitOfWork.GetRepository<SubmitResult>().GetQueryable()
                        .AsNoTracking()
                        .Include(sr => sr.Track)
                            .ThenInclude(t => t.Round)
                        .FirstOrDefaultAsync(sr => sr.Id == id);
                    if (submitResult != null)
                    {
                        trackId = submitResult.TrackId;
                        eventId = submitResult.Track?.Round?.EventId;
                    }
                    break;

                case "Scores":
                    var score = await _unitOfWork.GetRepository<Score>().GetQueryable()
                        .AsNoTracking()
                        .Include(s => s.EventRole)
                        .FirstOrDefaultAsync(s => s.Id == id);
                    if (score?.EventRole != null)
                    {
                        eventId = score.EventRole.EventId;
                        trackId = score.EventRole.TrackId;
                    }
                    break;

                case "ScoreDetails":
                    var scoreDetail = await _unitOfWork.GetRepository<ScoreDetail>().GetQueryable()
                        .AsNoTracking()
                        .Include(sd => sd.Score)
                            .ThenInclude(s => s.EventRole)
                        .FirstOrDefaultAsync(sd => sd.Id == id);
                    if (scoreDetail?.Score?.EventRole != null)
                    {
                        eventId = scoreDetail.Score.EventRole.EventId;
                        trackId = scoreDetail.Score.EventRole.TrackId;
                    }
                    break;

                case "FinalResults":
                    var finalResult = await _unitOfWork.GetRepository<FinalResult>().GetQueryable()
                        .AsNoTracking()
                        .Include(fr => fr.Round)
                        .FirstOrDefaultAsync(fr => fr.Id == id);
                    eventId = finalResult?.Round?.EventId;
                    break;
            }

            return (eventId, trackId);
        }

        public async Task<string?> ResolveEventIdFromRoundAsync(string roundId)
        {
            if (string.IsNullOrEmpty(roundId)) return null;
            var round = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roundId);
            return round?.EventId;
        }

        public async Task<string?> ResolveTeamIdFromSubmitResultAsync(string submitResultId)
        {
            if (string.IsNullOrEmpty(submitResultId)) return null;
            var submitResult = await _unitOfWork.GetRepository<SubmitResult>().GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.Id == submitResultId);
            return submitResult?.TeamId;
        }
    }
}
