// [FLOW3-NOPBAI][GetMySubmissions] Sinh vien xem lai cac bai nop ma doi minh da gui qua tung vong thi.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Commons;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Application.Features.SubmitResults.Queries.GetSubmitResultsList.Models;
using SEAL_Domain.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SEAL_Domain.Base;

namespace SEAL_Application.Features.Teams.Queries.GetMySubmissions
{
    public class GetMySubmissionsQueryHandler : IRequestHandler<GetMySubmissionsQuery, Result<PagedResult<SubmitResultListItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMySubmissionsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<SubmitResultListItemModel>>> Handle(GetMySubmissionsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Người dùng chưa đăng nhập.");
            }

            // 1. Tìm tất cả các TeamId mà người dùng này tham gia (thông qua EventRole)
            var teamIds = await _unitOfWork.GetRepository<EventRole>().GetQueryable()
                .AsNoTracking()
                .Where(er => er.UserId == userId && er.TeamId != null)
                .Where(er => er.ExpiredAt == null || er.ExpiredAt > System.DateTime.UtcNow)
                .Select(er => er.TeamId!)
                .Distinct()
                .ToListAsync(cancellationToken);

            // 2. Lấy tất cả SubmitResult thuộc về các TeamId đó
            var query = _unitOfWork.GetRepository<SubmitResult>().GetQueryable()
                .AsNoTracking()
                .Include(sr => sr.Team)
                .Where(sr => teamIds.Contains(sr.TeamId));

            // 3. Phân trang và trả về kết quả — đủ 3 URL để FE xem lại bài đã nộp.
            var pagedResults = await query
                .ToPagedResultAsync(request, sr => new SubmitResultListItemModel
                {
                    Id = sr.Id,
                    TeamId = sr.TeamId,
                    TrackId = sr.TrackId,
                    TeamName = sr.Team != null ? sr.Team.Name : null,
                    SubmissionUrl = sr.SubmissionUrl,
                    RepoUrl = sr.RepoUrl,
                    DemoUrl = sr.DemoUrl,
                    SlideUrl = sr.SlideUrl,
                    IsActive = sr.IsActive,
                    CreatedTime = sr.CreatedTime
                }, cancellationToken);

            return pagedResults;
        }
    }
}




