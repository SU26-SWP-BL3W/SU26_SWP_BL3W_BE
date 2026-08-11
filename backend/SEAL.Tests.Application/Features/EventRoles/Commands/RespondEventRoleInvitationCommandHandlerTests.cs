using FluentAssertions;
using Moq;
using SEAL_Application.Features.EventRoles.Commands.RespondEventRoleInvitation;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL.Tests.Application.Features.EventRoles.Commands
{
    /// <summary>
    /// Kiểm thử luồng phản hồi lời mời vai trò sự kiện (RespondEventRoleInvitationCommandHandler):
    /// accept tạo EventRole + xóa cache phân quyền; reject đánh dấu Rejected; chặn lời mời
    /// không tồn tại / không phải người được mời / đã hết hạn.
    /// </summary>
    public class RespondEventRoleInvitationCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<IGenericRepository<EventRoleInvitation>> _inviteRepo;
        private readonly Mock<IGenericRepository<Event>> _eventRepo;
        private readonly Mock<IGenericRepository<User>> _userRepo;
        private readonly Mock<IGenericRepository<EventRole>> _roleRepo;
        private readonly Mock<ICurrentUserService> _currentUser;
        private readonly Mock<IEventRoleChecker> _roleChecker;
        private readonly RespondEventRoleInvitationCommandHandler _handler;

        private const string UserId = "user-1";

        public RespondEventRoleInvitationCommandHandlerTests()
        {
            _uow = new Mock<IUnitOfWork>();
            _inviteRepo = new Mock<IGenericRepository<EventRoleInvitation>>();
            _eventRepo = new Mock<IGenericRepository<Event>>();
            _userRepo = new Mock<IGenericRepository<User>>();
            _roleRepo = new Mock<IGenericRepository<EventRole>>();
            _currentUser = new Mock<ICurrentUserService>();
            _roleChecker = new Mock<IEventRoleChecker>();

            _uow.Setup(u => u.GetRepository<EventRoleInvitation>()).Returns(_inviteRepo.Object);
            _uow.Setup(u => u.GetRepository<Event>()).Returns(_eventRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<EventRole>()).Returns(_roleRepo.Object);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _currentUser.Setup(c => c.UserId).Returns(UserId);

            _handler = new RespondEventRoleInvitationCommandHandler(_uow.Object, _currentUser.Object, _roleChecker.Object);
        }

        private static EventRoleInvitation BuildPendingInvitation() => new EventRoleInvitation
        {
            EventId = "event-1",
            InvitedUserId = UserId,
            InvitedByUserId = "inviter-1",
            RoleName = EventRoleType.Judge,
            Status = EventRoleInvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(3)
        };

        [Fact]
        public async Task Handle_Accept_CreatesEventRoleAndInvalidatesCache()
        {
            var invitation = BuildPendingInvitation();
            _inviteRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(invitation);
            // Handler dùng GetByIdAsync (không phải AnyAsync) để tái xác thực Event khi ACCEPT.
            _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new Event { Id = invitation.EventId });
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new User { Id = UserId, IsApproved = true });
            _roleRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<EventRole, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _roleRepo.Setup(r => r.AddAsync(It.IsAny<EventRole>())).Returns(Task.CompletedTask);
            _inviteRepo.Setup(r => r.UpdateAsync(It.IsAny<EventRoleInvitation>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(new RespondEventRoleInvitationCommand(invitation.Id, true), CancellationToken.None);

            result.Value.Status.Should().Be(EventRoleInvitationStatus.Accepted.ToString());
            result.Value.EventRoleId.Should().NotBeNullOrEmpty();
            invitation.Status.Should().Be(EventRoleInvitationStatus.Accepted);
            invitation.RespondedAt.Should().NotBeNull();
            _roleRepo.Verify(r => r.AddAsync(It.IsAny<EventRole>()), Times.Once);
            _roleChecker.Verify(c => c.InvalidateCache(UserId, "event-1"), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Reject_MarksRejectedAndCreatesNoRole()
        {
            var invitation = BuildPendingInvitation();
            _inviteRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(invitation);
            _inviteRepo.Setup(r => r.UpdateAsync(It.IsAny<EventRoleInvitation>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(new RespondEventRoleInvitationCommand(invitation.Id, false), CancellationToken.None);

            result.Value.Status.Should().Be(EventRoleInvitationStatus.Rejected.ToString());
            result.Value.EventRoleId.Should().BeNull();
            invitation.Status.Should().Be(EventRoleInvitationStatus.Rejected);
            _roleRepo.Verify(r => r.AddAsync(It.IsAny<EventRole>()), Times.Never);
        }

        [Fact]
        public async Task Handle_InvitationNotFound_Throws()
        {
            _inviteRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync((EventRoleInvitation?)null);

            var result = await _handler.Handle(new RespondEventRoleInvitationCommand("missing", true), CancellationToken.None);
            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_NotTheInvitedUser_ThrowsForbidden()
        {
            var invitation = BuildPendingInvitation();
            invitation.InvitedUserId = "someone-else";
            _inviteRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(invitation);

            var result = await _handler.Handle(new RespondEventRoleInvitationCommand(invitation.Id, true), CancellationToken.None);
            result.IsFailure.Should().BeTrue();
            _roleRepo.Verify(r => r.AddAsync(It.IsAny<EventRole>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ExpiredInvitation_ThrowsAndMarksExpired()
        {
            var invitation = BuildPendingInvitation();
            invitation.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            _inviteRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(invitation);
            _inviteRepo.Setup(r => r.UpdateAsync(It.IsAny<EventRoleInvitation>())).Returns(Task.CompletedTask);

            var result = await _handler.Handle(new RespondEventRoleInvitationCommand(invitation.Id, true), CancellationToken.None);
            result.IsFailure.Should().BeTrue();
            invitation.Status.Should().Be(EventRoleInvitationStatus.Expired);
            _roleRepo.Verify(r => r.AddAsync(It.IsAny<EventRole>()), Times.Never);
        }
    }
}
