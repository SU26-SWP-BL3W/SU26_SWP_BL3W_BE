using FluentAssertions;
using Moq;
using SEAL.Tests.Application.Helpers;
using SEAL_Application.Features.Users.Queries.GetMyInvitations;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL.Tests.Application.Features.Users.Queries
{
    /// <summary>
    /// Kiểm thử API tổng hợp lời mời (GetMyInvitationsQueryHandler):
    /// gộp TeamInvitation + EventRoleInvitation đang chờ của user hiện tại, map ra DTO chung,
    /// sắp xếp theo thời điểm hết hạn gần nhất.
    /// </summary>
    public class GetMyInvitationsQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<IGenericRepository<TeamInvitation>> _teamInviteRepo;
        private readonly Mock<IGenericRepository<EventRoleInvitation>> _eventInviteRepo;
        private readonly Mock<IGenericRepository<User>> _userRepo;
        private readonly Mock<ICurrentUserService> _currentUser;
        private readonly GetMyInvitationsQueryHandler _handler;

        private const string UserId = "user-1";

        public GetMyInvitationsQueryHandlerTests()
        {
            _uow = new Mock<IUnitOfWork>();
            _teamInviteRepo = new Mock<IGenericRepository<TeamInvitation>>();
            _eventInviteRepo = new Mock<IGenericRepository<EventRoleInvitation>>();
            _userRepo = new Mock<IGenericRepository<User>>();
            _currentUser = new Mock<ICurrentUserService>();

            _uow.Setup(u => u.GetRepository<TeamInvitation>()).Returns(_teamInviteRepo.Object);
            _uow.Setup(u => u.GetRepository<EventRoleInvitation>()).Returns(_eventInviteRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _currentUser.Setup(c => c.UserId).Returns(UserId);

            _handler = new GetMyInvitationsQueryHandler(_uow.Object, _currentUser.Object);
        }

        private void SetupRepos(List<TeamInvitation> teamInvites, List<EventRoleInvitation> eventInvites, List<User> users)
        {
            _teamInviteRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(teamInvites).Object);
            _eventInviteRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(eventInvites).Object);
            _userRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(users).Object);
        }

        [Fact]
        public async Task Handle_AggregatesTeamAndEventRoleInvitations_OrderedByExpiry()
        {
            var teamInvite = new TeamInvitation
            {
                TeamId = "team-1",
                InvitedUserId = UserId,
                InvitedByUserId = "leader-1",
                Status = TeamInvitationStatus.PendingAccept,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                Team = new Team { Id = "team-1", Name = "The Bug Slayers" }
            };
            var eventInvite = new EventRoleInvitation
            {
                EventId = "event-1",
                InvitedUserId = UserId,
                InvitedByUserId = "ec-1",
                RoleName = EventRoleType.Judge,
                Status = EventRoleInvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                Event = new Event { Id = "event-1", EventName = "FPT Hackathon 2026" },
                InvitedByUser = new User { Id = "ec-1", FullName = "Ban To Chuc" }
            };

            SetupRepos(
                new List<TeamInvitation> { teamInvite },
                new List<EventRoleInvitation> { eventInvite },
                new List<User> { new User { Id = "leader-1", FullName = "Team Leader" } });

            var result = await _handler.Handle(new GetMyInvitationsQuery(), CancellationToken.None);

            result.Value.TotalPending.Should().Be(2);
            result.Value.Invitations.Should().Contain(i =>
                i.Type == "TEAM" && i.TargetName == "The Bug Slayers" && i.InviterName == "Team Leader" && i.Role == "Thành viên");
            result.Value.Invitations.Should().Contain(i =>
                i.Type == "EVENT_ROLE" && i.TargetName == "FPT Hackathon 2026" && i.InviterName == "Ban To Chuc" && i.Role == "Judge");
            // Sắp theo ExpiresAt tăng dần: lời mời đội (1 ngày) đứng trước lời mời vai trò (5 ngày)
            result.Value.Invitations.First().Type.Should().Be("TEAM");
        }

        [Fact]
        public async Task Handle_NoPendingInvitations_ReturnsEmpty()
        {
            SetupRepos(new List<TeamInvitation>(), new List<EventRoleInvitation>(), new List<User>());

            var result = await _handler.Handle(new GetMyInvitationsQuery(), CancellationToken.None);

            result.Value.TotalPending.Should().Be(0);
            result.Value.Invitations.Should().BeEmpty();
        }
    }
}
