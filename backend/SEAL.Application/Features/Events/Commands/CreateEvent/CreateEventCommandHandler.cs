using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Features.Events.Commands.CreateEvent.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Events.Commands.CreateEvent
{
    public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<CreateEventResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CreateEventCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CreateEventResponseModel>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            // 0. Kiểm tra quyền khởi tạo: Chỉ Admin mới có quyền tạo mới Sự kiện
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            if (currentUser == null)
            {
                return new BaseException.UnauthorizedException("Tài khoản người dùng không tồn tại.");
            }

            var isDuplicate = await _unitOfWork.GetRepository<Event>().Entities
                .AnyAsync(e => e.EventName.ToLower() == request.Model.EventName.ToLower() && e.Year == request.Model.Year, cancellationToken);
            if (isDuplicate)
            {
                return BaseException.BadRequestDupplicationResponse($"Sự kiện '{request.Model.EventName}' cho năm {request.Model.Year} đã tồn tại trong hệ thống.");
            }

            _unitOfWork.BeginTransaction();
            try
            {
                // 1. Kiểm tra Template Weight
                var templateIds = request.Model.Rounds
                    .SelectMany(r => r.Tracks)
                    .Select(t => t.TemplateId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                foreach (var tId in templateIds)
                {
                    var template = await _unitOfWork.GetRepository<Template>().GetByIdAsync(tId);
                    if (template == null)
                        return BaseException.BadRequestNotFoundResponse($"Mẫu tiêu chí có ID '{tId}' không tồn tại.");

                    var totalWeight = await _unitOfWork.GetRepository<TemplateCriteria>().Entities
                        .Where(tc => tc.TemplateId == tId)
                        .SumAsync(tc => (decimal?)tc.Weight, cancellationToken) ?? 0m;

                    if (totalWeight != 100m)
                    {
                        return BaseException.BadRequestResponse($"Template '{template.TemplateName}' hiện có tổng trọng số là {totalWeight}%. Vui lòng cấu hình đủ 100% trước khi đưa vào sử dụng.");
                    }
                }


                // 2. Tạo mới thực thể Event
                var eventEntity = new Event
                {
                    EventName = request.Model.EventName,
                    Season = request.Model.Season,
                    Year = request.Model.Year,
                    StartDate = request.Model.StartDate.ToUniversalTime(),
                    EndDate = request.Model.EndDate.ToUniversalTime(),
                    RegistrationStartDate = request.Model.RegistrationStartDate?.ToUniversalTime(),
                    RegistrationEndDate = request.Model.RegistrationEndDate?.ToUniversalTime(),
                    Description = request.Model.Description,
                    Status = request.Model.Status,
                    PhotoEventUrl = request.Model.PhotoEventUrl,
                    MaxTeams = request.Model.MaxTeams
                };

                await _unitOfWork.GetRepository<Event>().AddAsync(eventEntity);
                // SaveChanges lần 1 để EventEntity sinh Id
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var roundsToInsert = new List<Round>();
                var tracksToInsert = new List<Track>();
                var eventRolesToInsert = new List<EventRole>();

                // Tự động gán vai trò EventCoordinator cho người tạo Event
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    eventRolesToInsert.Add(new EventRole
                    {
                        UserId = currentUserId,
                        EventId = eventEntity.Id,
                        RoleName = EventRoleType.EventCoordinator,
                        AssignedAt = DateTime.UtcNow,
                        ExpiredAt = eventEntity.EndDate,
                        Notes = "Auto-assigned EventCoordinator khi tạo Event"
                    });
                }

                var roundResponses = new List<RoundResponseDto>();

                // 3. Xử lý các Rounds, Tracks, Judges, Mentors lồng nhau
                foreach (var roundDto in request.Model.Rounds)
                {
                    var roundId = Guid.NewGuid().ToString();
                    var roundEntity = new Round
                    {
                        Id = roundId,
                        EventId = eventEntity.Id,
                        RoundName = roundDto.RoundName,
                        RoundNumber = roundDto.RoundNumber,
                        StartDate = roundDto.StartDate.ToUniversalTime(),
                        EndDate = roundDto.EndDate.ToUniversalTime(),
                        AdvancementRule = roundDto.AdvancementRule,
                        ScoringStartDate = roundDto.ScoringStartDate?.ToUniversalTime(),
                        ScoringEndDate = roundDto.ScoringEndDate?.ToUniversalTime()
                    };
                    roundsToInsert.Add(roundEntity);

                    var trackResponses = new List<TrackResponseDto>();

                    foreach (var trackDto in roundDto.Tracks)
                    {
                        var trackId = Guid.NewGuid().ToString();
                        var trackEntity = new Track
                        {
                            Id = trackId,
                            EventId = eventEntity.Id,
                            TrackName = trackDto.TrackName,
                            Description = trackDto.Description,
                            TemplateId = trackDto.TemplateId,
                            SubmissionRuleDescription = trackDto.SubmissionRuleDescription,
                            StartDate = trackDto.StartDate?.ToUniversalTime(),
                            EndDate = trackDto.EndDate?.ToUniversalTime(),
                            ScoringStartDate = trackDto.ScoringStartDate?.ToUniversalTime(),
                            ScoringEndDate = trackDto.ScoringEndDate?.ToUniversalTime()
                        };
                        tracksToInsert.Add(trackEntity);



                        trackResponses.Add(new TrackResponseDto
                        {
                            Id = trackId,
                            TrackName = trackEntity.TrackName,
                            Description = trackEntity.Description,
                            TemplateId = trackEntity.TemplateId,
                            SubmissionRuleDescription = trackEntity.SubmissionRuleDescription,
                            StartDate = trackEntity.StartDate,
                            EndDate = trackEntity.EndDate,
                            ScoringStartDate = trackEntity.ScoringStartDate,
                            ScoringEndDate = trackEntity.ScoringEndDate
                        });
                    }

                    roundResponses.Add(new RoundResponseDto
                    {
                        Id = roundId,
                        RoundName = roundEntity.RoundName,
                        RoundNumber = roundEntity.RoundNumber,
                        StartDate = roundEntity.StartDate,
                        EndDate = roundEntity.EndDate,
                        AdvancementRule = roundEntity.AdvancementRule,
                        ScoringStartDate = roundEntity.ScoringStartDate,
                        ScoringEndDate = roundEntity.ScoringEndDate,
                        Tracks = trackResponses
                    });
                }

                // Batch insert cho các thực thể phụ
                if (roundsToInsert.Any())
                {
                    await _unitOfWork.GetRepository<Round>().AddRangeAsync(roundsToInsert);
                }
                if (tracksToInsert.Any())
                {
                    await _unitOfWork.GetRepository<Track>().AddRangeAsync(tracksToInsert);
                }
                if (eventRolesToInsert.Any())
                {
                    await _unitOfWork.GetRepository<EventRole>().AddRangeAsync(eventRolesToInsert);
                }

                // SaveChanges lần 2 để hoàn tất lưu toàn bộ cấu trúc
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _unitOfWork.CommitTransaction();

                return new CreateEventResponseModel
                {
                    Id = eventEntity.Id,
                    EventName = eventEntity.EventName,
                    Season = eventEntity.Season,
                    Year = eventEntity.Year,
                    StartDate = eventEntity.StartDate,
                    EndDate = eventEntity.EndDate,
                    RegistrationStartDate = eventEntity.RegistrationStartDate,
                    RegistrationEndDate = eventEntity.RegistrationEndDate,
                    Description = eventEntity.Description,
                    Status = eventEntity.Status,
                    PhotoEventUrl = eventEntity.PhotoEventUrl,
                    CreatedTime = eventEntity.CreatedTime,
                    Rounds = roundResponses
                };
            }
            catch (Exception)
            {
                _unitOfWork.RollBack();
                throw;
            }
        }
    }
}

