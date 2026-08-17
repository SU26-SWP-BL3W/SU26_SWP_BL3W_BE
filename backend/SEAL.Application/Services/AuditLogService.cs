using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public AuditLogService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task AppendAsync(
            string action,
            string entityType,
            string entityId,
            string? eventId,
            string? summary,
            object? payload = null,
            CancellationToken cancellationToken = default)
        {
            string? payloadJson = null;
            if (payload != null)
            {
                payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
                if (payloadJson.Length > 4000)
                {
                    payloadJson = payloadJson[..4000];
                }
            }

            await _unitOfWork.GetRepository<AuditLog>().AddAsync(new AuditLog
            {
                EventId = eventId,
                ActorUserId = _currentUserService.UserId ?? string.Empty,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Summary = summary != null && summary.Length > 500 ? summary[..500] : summary,
                PayloadJson = payloadJson
            });
        }
    }
}
