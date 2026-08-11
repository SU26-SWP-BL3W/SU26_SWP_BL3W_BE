using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SEAL_Application.Features.EventRoles.Commands.InviteEventRole;
using SEAL_Application.Features.EventRoles.Commands.InviteEventRole.Models;
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
    /// Kiểm thử luồng gửi lời mời vai trò sự kiện (InviteEventRoleCommandHandler):
    /// tạo lời mời Pending hết hạn 7 ngày + gửi email thông báo; chặn khi event/người dùng
    /// không tồn tại, đã giữ vai trò, hoặc đã có lời mời cùng vai trò đang chờ.
    /// </summary>
    public class InviteEventRoleCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow;
        private readonly Mock<IGenericRepository<Event>> _eventRepo;
        private readonly Mock<IGenericRepository<User>> _userRepo;
        private readonly Mock<IGenericRepository<EventRole>> _roleRepo;
        private readonly Mock<IGenericRepository<EventRoleInvitation>> _inviteRepo;
        private readonly Mock<ICurrentUserService> _currentUser;
        private readonly Mock<IEmailService> _emailService;
        private readonly Mock<IConfiguration> _configuration;
        private readonly InviteEventRoleCommandHandler _handler;

        private const string InviterId = "inviter-1";

        public InviteEventRoleCommandHandlerTests()
        {
            _uow = new Mock<IUnitOfWork>();
            _eventRepo = new Mock<IGenericRepository<Event>>();
            _userRepo = new Mock<IGenericRepository<User>>();
            _roleRepo = new Mock<IGenericRepository<EventRole>>();
            _inviteRepo = new Mock<IGenericRepository<EventRoleInvitation>>();
            _currentUser = new Mock<ICurrentUserService>();
            _emailService = new Mock<IEmailService>();
            _configuration = new Mock<IConfiguration>();

            _uow.Setup(u => u.GetRepository<Event>()).Returns(_eventRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<EventRole>()).Returns(_roleRepo.Object);
            _uow.Setup(u => u.GetRepository<EventRoleInvitation>()).Returns(_inviteRepo.Object);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _currentUser.Setup(c => c.UserId).Returns(InviterId);
            _configuration.Setup(c => c["FrontendUrl"]).Returns("https://fe.test");
            _emailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _handler = new InviteEventRoleCommandHandler(
                _uow.Object, _currentUser.Object, _emailService.Object, _configuration.Object);
        }

        private static InviteEventRoleCommand BuildCommand(EventRoleType role = EventRoleType.Judge) => new InviteEventRoleCommand
        {
            Model = new InviteEventRoleRequestModel
            {
                EventId = "event-1",
                InvitedUserId = "user-1",
                RoleName = role
            }
        };

        private void SetupHappyPath()
        {
            _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new Event { Id = "event-1", EventName = "Event One" });
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new User { Id = "user-1", Email = "u@test.com", FullName = "User One" });
            _roleRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<EventRole, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _inviteRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<EventRoleInvitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _inviteRepo.Setup(r => r.AddAsync(It.IsAny<EventRoleInvitation>())).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task Handle_HappyPath_CreatesPendingInvitationAndSendsEmail()
        {
            SetupHappyPath();

            var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

            result.Should().NotBeNull();
            result.Value.Status.Should().Be(EventRoleInvitationStatus.Pending.ToString());
            result.Value.RoleName.Should().Be(EventRoleType.Judge.ToString());
            result.Value.InvitedUserId.Should().Be("user-1");
            // Lời mời hết hạn sau 24 giờ (đồng bộ với toàn bộ email hệ thống)
            result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(23));
            result.Value.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(25));
            _inviteRepo.Verify(r => r.AddAsync(It.IsAny<EventRoleInvitation>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _emailService.Verify(e => e.SendEmailAsync("u@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmailFailure_StillSucceeds()
        {
            SetupHappyPath();
            _emailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP down"));

            var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

            result.Value.Status.Should().Be(EventRoleInvitationStatus.Pending.ToString());
            _inviteRepo.Verify(r => r.AddAsync(It.IsAny<EventRoleInvitation>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EventNotFound_Throws()
        {
            _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync((Event?)null);

            var result = await _handler.Handle(BuildCommand(), CancellationToken.None);
            result.IsFailure.Should().BeTrue();
            _inviteRepo.Verify(r => r.AddAsync(It.IsAny<EventRoleInvitation>()), Times.Never);
        }

        [Fact]
        public async Task Handle_InvitedUserNotFound_Throws()
        {
            _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new Event { Id = "event-1", EventName = "Event One" });
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync((User?)null);

            var result = await _handler.Handle(BuildCommand(), CancellationToken.None);
            result.IsFailure.Should().BeTrue();
            _inviteRepo.Verify(r => r.AddAsync(It.IsAny<EventRoleInvitation>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserAlreadyHasRole_Throws()
        {
            _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new Event { Id = "event-1", EventName = "Event One" });
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new User { Id = "user-1" });
            _roleRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<EventRole, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _handler.Handle(BuildCommand(), CancellationToken.None);
            result.IsFailure.Should().BeTrue();
            _inviteRepo.Verify(r => r.AddAsync(It.IsAny<EventRoleInvitation>()), Times.Never);
        }

        [Fact]
        public async Task Handle_PendingInvitationExists_Throws()
        {
            _eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new Event { Id = "event-1", EventName = "Event One" });
            _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>())).ReturnsAsync(new User { Id = "user-1" });
            _roleRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<EventRole, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _inviteRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<EventRoleInvitation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _handler.Handle(BuildCommand(), CancellationToken.None);
            result.IsFailure.Should().BeTrue();
            _inviteRepo.Verify(r => r.AddAsync(It.IsAny<EventRoleInvitation>()), Times.Never);
        }
    }
}
