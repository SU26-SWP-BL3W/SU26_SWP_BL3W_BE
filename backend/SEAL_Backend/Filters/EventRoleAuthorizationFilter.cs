using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SEAL_Application.Interfaces;
using SEAL_Domain.Entity.Enums;
using SEAL_Infrastructure.Services;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEAL_Backend.Filters
{
    public class EventRoleAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly EventRoleType[] _allowedRoles;
        private readonly IEventRoleChecker _eventRoleChecker;
        private readonly IEventMetadataResolver _metadataResolver;

        public EventRoleAuthorizationFilter(
            EventRoleType[] allowedRoles, 
            IEventRoleChecker eventRoleChecker,
            IEventMetadataResolver metadataResolver)
        {
            _allowedRoles = allowedRoles;
            _eventRoleChecker = eventRoleChecker;
            _metadataResolver = metadataResolver;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            
            // 1. Get UserId
            var userId = Token.GetUserIdFromHttpContext(httpContext);
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 2. Extract EventId & TrackId from Route -> Query -> Header -> Body
            string? eventId = null;
            string? trackId = null;
            string? teamId = null;

            // 2.1 Route values
            if (context.RouteData.Values.TryGetValue("eventId", out var routeEventId)) eventId = routeEventId?.ToString();
            if (context.RouteData.Values.TryGetValue("trackId", out var routeTrackId)) trackId = routeTrackId?.ToString();
            if (context.RouteData.Values.TryGetValue("teamId", out var routeTeamId)) teamId = routeTeamId?.ToString();

            // 2.2 Query
            if (string.IsNullOrEmpty(eventId) && httpContext.Request.Query.TryGetValue("eventId", out var queryEventId)) eventId = queryEventId.ToString();
            if (string.IsNullOrEmpty(trackId) && httpContext.Request.Query.TryGetValue("trackId", out var queryTrackId)) trackId = queryTrackId.ToString();
            if (string.IsNullOrEmpty(teamId) && httpContext.Request.Query.TryGetValue("teamId", out var queryTeamId)) teamId = queryTeamId.ToString();

            // 2.3 Headers
            if (string.IsNullOrEmpty(eventId) && httpContext.Request.Headers.TryGetValue("X-Event-Id", out var headerEventId)) eventId = headerEventId.ToString();
            if (string.IsNullOrEmpty(trackId) && httpContext.Request.Headers.TryGetValue("X-Track-Id", out var headerTrackId)) trackId = headerTrackId.ToString();
            if (string.IsNullOrEmpty(teamId) && httpContext.Request.Headers.TryGetValue("X-Team-Id", out var headerTeamId)) teamId = headerTeamId.ToString();

            // 2.4 Body (JSON)
            if (string.IsNullOrEmpty(eventId) && httpContext.Request.HasJsonContentType())
            {
                var bodyMetadata = await ExtractMetadataFromBody(httpContext);
                if (string.IsNullOrEmpty(eventId)) eventId = bodyMetadata.EventId;
                if (string.IsNullOrEmpty(trackId)) trackId = bodyMetadata.TrackId;
                if (string.IsNullOrEmpty(teamId)) teamId = bodyMetadata.TeamId;

                // Auto Resolve for Scores Creation
                if (controllerName == "Scores" && string.IsNullOrEmpty(eventId) && !string.IsNullOrEmpty(bodyMetadata.SubmitResultId))
                {
                    var resolved = await _metadataResolver.ResolveFromEntityAsync("SubmitResults", bodyMetadata.SubmitResultId);
                    eventId = resolved.EventId;
                    trackId = resolved.TrackId;
                }
                // Auto Resolve for ScoreDetails Creation
                else if (controllerName == "ScoreDetails" && string.IsNullOrEmpty(eventId) && !string.IsNullOrEmpty(bodyMetadata.ScoreId))
                {
                    var resolved = await _metadataResolver.ResolveFromEntityAsync("Scores", bodyMetadata.ScoreId);
                    eventId = resolved.EventId;
                    trackId = resolved.TrackId;
                }
                // Auto Resolve for SubmitResults Creation (from trackId)
                else if (controllerName == "SubmitResults" && string.IsNullOrEmpty(eventId) && !string.IsNullOrEmpty(trackId))
                {
                    var resolved = await _metadataResolver.ResolveFromEntityAsync("Tracks", trackId);
                    eventId = resolved.EventId;
                }
            }

            // 2.5 Auto Resolution from Database based on Controller and Route "id" or other parameters
            if (string.IsNullOrEmpty(eventId))
            {
                if (context.RouteData.Values.TryGetValue("id", out var routeIdObj) && routeIdObj != null)
                {
                    var resolved = await _metadataResolver.ResolveFromEntityAsync(controllerName ?? "", routeIdObj.ToString()!);
                    eventId = resolved.EventId;
                    trackId = resolved.TrackId;

                    // Bài nộp: lấy thêm TeamId để vai trò đội (TeamLeader/TeamMember — gắn TeamId,
                    // không gắn TrackId) khớp được tầng kiểm tra theo Team trong EventRoleChecker.
                    // Thiếu bước này, TeamLeader luôn bị 403 khi PUT/DELETE /SubmitResults/{id}.
                    if (controllerName == "SubmitResults" && string.IsNullOrEmpty(teamId))
                    {
                        teamId = await _metadataResolver.ResolveTeamIdFromSubmitResultAsync(routeIdObj.ToString()!);
                    }

                    // Đội: route id CHÍNH LÀ TeamId -> truyền vào checker, nếu không TeamLeader
                    // (vai trò gắn TeamId) không khớp tầng nào và luôn bị 403 khi PUT/DELETE /Teams/{id}.
                    if (controllerName == "Teams" && string.IsNullOrEmpty(teamId))
                    {
                        teamId = routeIdObj.ToString();
                    }
                }
                else if (controllerName == "Scores" && context.RouteData.Values.TryGetValue("eventRoleId", out var erIdObj) && erIdObj != null)
                {
                    var resolved = await _metadataResolver.ResolveFromEntityAsync("EventRoles", erIdObj.ToString()!);
                    eventId = resolved.EventId; 
                    trackId = resolved.TrackId;
                }
                else if (controllerName == "ScoreDetails" && context.RouteData.Values.TryGetValue("scoreId", out var sIdObj) && sIdObj != null)
                {
                    var resolved = await _metadataResolver.ResolveFromEntityAsync("Scores", sIdObj.ToString()!);
                    eventId = resolved.EventId; 
                    trackId = resolved.TrackId;
                }
                else if (context.RouteData.Values.TryGetValue("roundId", out var rIdObj) && rIdObj != null)
                {
                    eventId = await _metadataResolver.ResolveEventIdFromRoundAsync(rIdObj.ToString()!);
                }
                else if (context.RouteData.Values.TryGetValue("teamId", out var tIdObj) && tIdObj != null)
                {
                    var tId = tIdObj.ToString()!;
                    var resolved = await _metadataResolver.ResolveFromEntityAsync("Teams", tId);
                    eventId = resolved.EventId;
                    if (string.IsNullOrEmpty(teamId)) teamId = tId;
                }
                // teamId da duoc lay tu query string o buoc 2.2 (vd GET /SubmitResults?teamId=...)
                // nhung nhanh tren chi doc RouteData nen bo sot -> luon 400 "EventId is required".
                else if (!string.IsNullOrEmpty(teamId))
                {
                    var resolved = await _metadataResolver.ResolveFromEntityAsync("Teams", teamId);
                    eventId = resolved.EventId;
                }
            }

            // 3. If EventId is still missing, fail
            if (string.IsNullOrEmpty(eventId))
            {
                context.Result = new BadRequestObjectResult(new { message = "EventId is required for this operation." });
                return;
            }

            // 4. Check permissions
            bool hasRole = await _eventRoleChecker.HasRoleAsync(userId, eventId, _allowedRoles, trackId: trackId, teamId: teamId);
            if (!hasRole)
            {
                context.Result = new ObjectResult(new { message = "Bạn không có quyền thực hiện thao tác này trong sự kiện." }) 
                { 
                    StatusCode = 403 
                };
            }
        }

        private async Task<(string? EventId, string? TrackId, string? SubmitResultId, string? ScoreId, string? TeamId)> ExtractMetadataFromBody(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            httpContext.Request.EnableBuffering();
            if (httpContext.Request.Body.CanSeek) httpContext.Request.Body.Position = 0;

            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, true, 1024, true);
            var body = await reader.ReadToEndAsync();
            httpContext.Request.Body.Position = 0;

            try
            {
                using var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    string? eId = GetStringProperty(jsonDoc.RootElement, "eventId") ?? GetStringProperty(jsonDoc.RootElement, "EventId");
                    string? tId = GetStringProperty(jsonDoc.RootElement, "trackId") ?? GetStringProperty(jsonDoc.RootElement, "TrackId");
                    string? srId = GetStringProperty(jsonDoc.RootElement, "submitResultId") ?? GetStringProperty(jsonDoc.RootElement, "SubmitResultId");
                    string? sId = GetStringProperty(jsonDoc.RootElement, "scoreId") ?? GetStringProperty(jsonDoc.RootElement, "ScoreId");
                    string? teamId = GetStringProperty(jsonDoc.RootElement, "teamId") ?? GetStringProperty(jsonDoc.RootElement, "TeamId");

                    return (eId, tId, srId, sId, teamId);
                }
            }
            catch { }
            return (null, null, null, null, null);
        }

        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
            return null;
        }
    }
}
