// [FLOW3-NOPBAI][GetSubmitResultsList] Danh sach tat ca bai nop cua 1 Hang muc, dung cho Giam khao/EC xem tong quan de cham diem.

using SEAL_Domain.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Commons;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList
{
    public class GetSubmitResultsListQueryHandler : IRequestHandler<GetSubmitResultsListQuery, Result<PagedResult<SubmitResultListItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public GetSubmitResultsListQueryHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<SubmitResultListItemModel>>> Handle(GetSubmitResultsListQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<SubmitResult>().Entities;

            // LỌC THEO SỰ KIỆN (suy qua Track -> Round): thiếu điều kiện này, EC/giám khảo sự kiện A
            // sẽ thấy lẫn bài nộp của mọi sự kiện khác.
            if (!string.IsNullOrEmpty(request.EventId))
            {
                query = query.Where(sr => sr.Track!.EventId == request.EventId);
            }

            if (!string.IsNullOrEmpty(request.TeamId))
            {
                query = query.Where(sr => sr.TeamId == request.TeamId);
            }

            if (!string.IsNullOrEmpty(request.RoundId))
            {
                query = query.Where(sr => sr.RoundId == request.RoundId);
            }

            if (!string.IsNullOrEmpty(request.TrackId))
            {
                query = query.Where(sr => sr.TrackId == request.TrackId);
            }

            // GIỚI HẠN PHẠM VI XEM: thành viên đội (không phải BTC) chỉ thấy bài nộp của đội MÌNH,
            // bất kể truyền bộ lọc gì. Admin xem tất cả; EC/Judge/Mentor trong đúng sự kiện xem như cũ.
            var currentUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
                bool isAdmin = currentUser != null && currentUser.IsAdmin;
                if (!isAdmin)
                {
                    // Suy ra sự kiện của truy vấn (nếu được) để xét vai trò tổ chức ĐÚNG phạm vi sự kiện đó.
                    string? scopeEventId = null;
                    if (!string.IsNullOrEmpty(request.EventId))
                    {
                        scopeEventId = request.EventId;
                    }
                    else if (!string.IsNullOrEmpty(request.TeamId))
                    {
                        var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.TeamId);
                        scopeEventId = team?.EventId;
                    }
                    else if (!string.IsNullOrEmpty(request.TrackId))
                    {
                        var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.TrackId);
                        scopeEventId = track?.EventId;
                    }
                    else if (!string.IsNullOrEmpty(request.RoundId))
                    {
                        var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.RoundId);
                        scopeEventId = round?.EventId;
                    }

                    // Lấy vai trò của user trong phạm vi sự kiện (hoặc mọi sự kiện nếu không suy ra được)
                    var roles = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                        .AsNoTracking()
                        .Where(er => er.UserId == currentUserId)
                        .Where(er => scopeEventId == null || er.EventId == scopeEventId)
                        .Select(er => new { er.RoleName, er.TrackId })
                        .ToListAsync(cancellationToken);

                    bool isEventCoordinator = roles.Any(r => r.RoleName == EventRoleType.EventCoordinator);
                    var judgeMentorRoles = roles
                        .Where(r => r.RoleName == EventRoleType.Judge || r.RoleName == EventRoleType.Mentor)
                        .ToList();
                    bool hasEventWideJudgeRole = judgeMentorRoles.Any(r => string.IsNullOrEmpty(r.TrackId));

                    if (isEventCoordinator || hasEventWideJudgeRole)
                    {
                        // EC (hoặc Judge/Mentor cấp sự kiện, không gắn track) -> xem theo bộ lọc như cũ
                    }
                    else if (judgeMentorRoles.Count > 0)
                    {
                        // Giám khảo/Mentor gắn track: CHỈ thấy bài thuộc các track mình được phân công
                        var assignedTrackIds = judgeMentorRoles
                            .Where(r => !string.IsNullOrEmpty(r.TrackId))
                            .Select(r => r.TrackId!)
                            .Distinct()
                            .ToList();
                        query = query.Where(sr => assignedTrackIds.Contains(sr.TrackId));
                    }
                    else
                    {
                        // Chỉ có vai trò đội (hoặc không vai trò) -> chỉ thấy bài nộp của đội MÌNH
                        query = query.Where(sr => sr.Team!.EventRoles.Any(er =>
                            er.UserId == currentUserId &&
                            (er.RoleName == EventRoleType.TeamLeader || er.RoleName == EventRoleType.TeamMember)));
                    }
                }
            }

            return await query.ToPagedResultAsync(
                request,
                sr => new SubmitResultListItemModel
                {
                    Id = sr.Id,
                    TeamId = sr.TeamId,
                    TrackId = sr.TrackId ?? string.Empty,
                    SubmissionUrl = sr.SubmissionUrl,
                    IsActive = sr.IsActive,
                    CreatedTime = sr.CreatedTime
                },
                cancellationToken
            );
        }
    }
}


