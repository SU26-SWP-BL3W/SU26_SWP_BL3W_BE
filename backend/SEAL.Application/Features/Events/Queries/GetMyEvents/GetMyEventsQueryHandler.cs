using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.Events.Queries.GetMyEvents.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Events.Queries.GetMyEvents
{
    public class GetMyEventsQueryHandler : IRequestHandler<GetMyEventsQuery, Result<List<MyEventResponseModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMyEventsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<MyEventResponseModel>>> Handle(GetMyEventsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Người dùng chưa đăng nhập hoặc token không hợp lệ.");
            }

            var myEvents = await _unitOfWork.GetRepository<EventRole>().Entities
                .Include(er => er.Event)
                .Where(er => er.UserId == currentUserId && (er.ExpiredAt == null || er.ExpiredAt > DateTime.UtcNow))
                .Select(er => new MyEventResponseModel
                {
                    Id = er.Event.Id,
                    EventName = er.Event.EventName,
                    Season = er.Event.Season,
                    Year = er.Event.Year,
                    StartDate = er.Event.StartDate,
                    EndDate = er.Event.EndDate,
                    RegistrationStartDate = er.Event.RegistrationStartDate,
                    RegistrationEndDate = er.Event.RegistrationEndDate,
                    Description = er.Event.Description,
                    Status = er.Event.Status,
                    PhotoEventUrl = er.Event.PhotoEventUrl,
                    CreatedTime = er.Event.CreatedTime,
                    LastUpdatedTime = er.Event.LastUpdatedTime,
                    Role = er.RoleName.ToString(),
                    TeamId = er.TeamId
                })
                .ToListAsync(cancellationToken);

            return myEvents;
        }
    }
}




