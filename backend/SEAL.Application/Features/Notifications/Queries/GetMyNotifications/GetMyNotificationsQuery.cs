using MediatR;
using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Application.Features.Notifications.Queries.GetMyNotifications
{
    public class GetMyNotificationsQuery : IRequest<Result<List<NotificationItemModel>>>
    {
    }

    public class NotificationItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info";
        public bool IsRead { get; set; }
        public string? LinkUrl { get; set; }
        public System.DateTimeOffset CreatedAt { get; set; }
    }
}
