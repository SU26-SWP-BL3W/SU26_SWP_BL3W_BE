using MediatR;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Notifications.Commands.MarkNotificationRead
{
    public class MarkNotificationReadCommand : IRequest<Result<bool>>
    {
        public string NotificationId { get; set; } = string.Empty;
    }
}
