using System.Threading.Tasks;

namespace SEAL_Application.Interfaces
{
    public interface IEventMetadataResolver
    {
        /// <summary>
        /// Tìm EventId và TrackId dựa trên tên Controller và ID thực thể.
        /// </summary>
        Task<(string? EventId, string? TrackId)> ResolveFromEntityAsync(string controllerName, string id);

        /// <summary>
        /// Tìm EventId dựa trên RoundId.
        /// </summary>
        Task<string?> ResolveEventIdFromRoundAsync(string roundId);

        /// <summary>
        /// Tìm TeamId của một bài nộp (SubmitResult) — để filter phân quyền khớp được
        /// vai trò TeamLeader/TeamMember (vai trò đội gắn TeamId, không gắn TrackId).
        /// </summary>
        Task<string?> ResolveTeamIdFromSubmitResultAsync(string submitResultId);
    }
}
