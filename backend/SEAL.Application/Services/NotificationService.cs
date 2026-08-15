using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task NotifyAsync(
            string userId,
            string title,
            string message,
            string type = "info",
            string? linkUrl = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;
            await _unitOfWork.GetRepository<AppNotification>().AddAsync(new AppNotification
            {
                UserId = userId,
                Title = title.Length > 200 ? title[..200] : title,
                Message = message.Length > 1000 ? message[..1000] : message,
                Type = string.IsNullOrWhiteSpace(type) ? "info" : type,
                LinkUrl = linkUrl
            });
        }

        public async Task NotifyManyAsync(
            IEnumerable<string> userIds,
            string title,
            string message,
            string type = "info",
            string? linkUrl = null,
            CancellationToken cancellationToken = default)
        {
            foreach (var id in userIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                await NotifyAsync(id, title, message, type, linkUrl, cancellationToken);
            }
        }
    }
}
