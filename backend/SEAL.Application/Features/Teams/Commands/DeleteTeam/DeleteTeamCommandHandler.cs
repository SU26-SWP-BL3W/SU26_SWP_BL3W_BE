// [FLOW3-DOITHI][DeleteTeam] Xoa doi chua chot dang ky.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEAL_Application.Features.Teams.Commands.DeleteTeam
{
    public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteTeamCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
        {
            var teamRepository = _unitOfWork.GetRepository<Team>();

            // Nhận diện bằng string
            var team = await teamRepository.GetByIdAsync(request.Id);
            if (team == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Nhóm không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

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

            if (!isAdmin && !isLeader && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa nhóm này.");
            }

            // Đội đã CHỐT DANH SÁCH (PendingApproval) hoặc ĐĂNG KÝ CHÍNH THỨC (Registered) thì leader
            // không tự xóa được; chỉ EC/Admin xử lý — nhất quán với Leave/Add/Remove đều khóa sau Forming.
            if (team.Status != SEAL_Domain.Entity.Enums.TeamStatus.Forming && !isAdmin && !isCoordinator)
            {
                return new BaseException.ForbiddenException(
                    "Đội đã chốt danh sách nên chỉ Event Coordinator/Admin được xóa đội.");
            }

            // Nhận diện TeamId bằng string
            var hasSubmissions = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                sr => sr.TeamId == request.Id,
                cancellationToken);

            if (hasSubmissions)
            {
                return BaseException.BadRequestInvaildInputResponse("Không thể xóa nhóm đã tham gia nộp bài giải.");
            }

            // Gỡ toàn bộ EventRole (leader + thành viên) của đội TRƯỚC khi xóa đội.
            // FK EventRoles.TeamId là NO ACTION nên DB không tự xóa theo Team → nếu không gỡ trước sẽ vướng
            // ràng buộc khóa ngoại và ném lỗi 500. (Bài nộp đã bị chặn 400 ở trên, nên không lo Scores/SubmitResults.)
            var teamRoles = await _unitOfWork.GetRepository<EventRole>().Entities
                .Where(er => er.TeamId == team.Id)
                .ToListAsync(cancellationToken);
            if (teamRoles.Count > 0)
            {
                await _unitOfWork.GetRepository<EventRole>().DeleteRangeAsync(teamRoles);
            }

            teamRepository.Delete(team);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

