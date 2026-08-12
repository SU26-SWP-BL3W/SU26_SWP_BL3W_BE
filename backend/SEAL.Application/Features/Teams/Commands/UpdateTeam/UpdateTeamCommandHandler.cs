// [FLOW3-DOITHI][UpdateTeam] Truong nhom cap nhat ten/mo ta doi.

using MediatR;
using SEAL_Application.Features.Teams.Commands.UpdateTeam.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.UpdateTeam
{
    public class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand, Result<UpdateTeamResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public UpdateTeamCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<UpdateTeamResponseModel>> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
        {
            var teamRepository = _unitOfWork.GetRepository<Team>();

            // Truy vấn trực tiếp bằng string Id (sử dụng request.Id thay vì request.Model.Id)
            var team = await teamRepository.GetByIdAsync(request.Id);
            if (team == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Nhóm không tồn tại trong hệ thống.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            bool isLeader = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId 
                      && er.TeamId == team.Id 
                      && er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                team.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isLeader && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền cập nhật nhóm này.");
            }

            // Đội đã ĐĂNG KÝ CHÍNH THỨC (khóa danh sách) thì chỉ EC/Admin được sửa thông tin —
            // tránh leader đổi tên đội giữa cuộc thi làm lệch bảng kết quả.
            if (team.Status != SEAL_Domain.Entity.Enums.TeamStatus.Forming && !isCoordinator)
            {
                return new BaseException.ForbiddenException(
                    "Đội đã đăng ký chính thức nên chỉ Event Coordinator được cập nhật thông tin đội.");
            }

            // IsActive: null = giữ nguyên; chỉ EC/Admin được thay đổi (tránh leader tự tắt đội / quên field bị tắt ngầm).
            if (request.Model.IsActive.HasValue && request.Model.IsActive.Value != team.IsActive && !isCoordinator)
            {
                return new BaseException.ForbiddenException(
                    "Chỉ Event Coordinator được thay đổi trạng thái hiệu lực (IsActive) của đội.");
            }

            var isNameExist = await teamRepository.AnyAsync(
                t => t.Name.ToLower() == request.Model.Name.ToLower() && t.Id != request.Id && t.EventId == team.EventId,
                cancellationToken);

            if (isNameExist)
            {
                return BaseException.BadRequestDupplicationResponse($"Tên nhóm '{request.Model.Name}' đã tồn tại.");
            }

            team.Name = request.Model.Name;
            team.Description = request.Model.Description;
            if (request.Model.IsActive.HasValue)
            {
                team.IsActive = request.Model.IsActive.Value;
            }
            team.LastUpdatedTime = CoreHelper.SystemTimeNow; // Sử dụng LastUpdatedTime khớp cấu trúc BaseEntity

            teamRepository.Update(team);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateTeamResponseModel
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                IsActive = team.IsActive,
                LastUpdatedTime = team.LastUpdatedTime
            };
        }
    }
}


