using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Notifications.Queries.GetMyNotifications
{
    public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, Result<List<NotificationItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMyNotificationsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<NotificationItemModel>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var items = await _unitOfWork.GetRepository<AppNotification>().Entities
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedTime)
                .Take(50)
                .Select(n => new NotificationItemModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    LinkUrl = n.LinkUrl,
                    CreatedAt = n.CreatedTime
                })
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
