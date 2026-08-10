// [FLOW3-DOITHI][GetTeamsList] Danh sach tat ca doi trong 1 su kien, phan trang, khong lo thong tin ca nhan thanh vien.

using SEAL_Domain.Base;
using MediatR;
using SEAL_Application.Commons;
using SEAL_Application.Features.Teams.Queries.GetTeamsList.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Teams.Queries.GetTeamsList
{
    public class GetTeamsListQueryHandler : IRequestHandler<GetTeamsListQuery, Result<PagedResult<TeamListItemModel>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;

        public GetTeamsListQueryHandler(IUnitOfWork unitOfWork, SEAL_Application.Interfaces.ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<TeamListItemModel>>> Handle(GetTeamsListQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.GetRepository<Team>().Entities;

            if (request.MyTeamsOnly == true)
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new SEAL_Domain.Base.BaseException.UnauthorizedException("Không thể xác thực người dùng.");
                }

                var userTeamIds = _unitOfWork.GetRepository<EventRole>().Entities
                    .Where(er => er.UserId == userId && er.TeamId != null)
                    .Select(er => er.TeamId)
                    .Distinct();

                query = query.Where(t => userTeamIds.Contains(t.Id));
            }

            if (!string.IsNullOrWhiteSpace(request.EventId))
            {
                query = query.Where(t => t.EventId == request.EventId);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchName))
            {
                query = query.Where(t => t.Name.ToLower().Contains(request.SearchName.ToLower()));
            }

            return await query.ToPagedResultAsync(
                request,
                t => new TeamListItemModel
                {
                    Id = t.Id,
                    EventId = t.EventId,
                    Name = t.Name,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    CreatedTime = t.CreatedTime
                },
                cancellationToken
            );
        }
    }
}


