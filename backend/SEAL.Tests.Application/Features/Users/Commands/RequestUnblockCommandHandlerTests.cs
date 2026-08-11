using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using SEAL.Tests.Application.Helpers;
using SEAL_Application.Features.Users.Commands.RequestUnblock;
using SEAL_Application.Features.Users.Commands.RequestUnblock.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL.Tests.Application.Features.Users.Commands
{
    /// <summary>
    /// Kiểm thử luồng "User tự gửi yêu cầu gỡ khóa" (RequestUnblockCommandHandler):
    /// chỉ gửi mail khi tài khoản thực sự bị khóa (>= 2 lần từ chối), chống dò email, chống spam 24h.
    /// </summary>
    public class RequestUnblockCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;
        private readonly Mock<IGenericRepository<UserRejection>> _mockUserRejectionRepository;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly IMemoryCache _memoryCache;
        private readonly RequestUnblockCommandHandler _handler;

        private const string TestEmail = "blocked@example.com";
        private const string SupportEmail = "btc@seal.test";

        public RequestUnblockCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();
            _mockUserRejectionRepository = new Mock<IGenericRepository<UserRejection>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<UserRejection>()).Returns(_mockUserRejectionRepository.Object);
            _mockConfiguration.Setup(c => c["SupportEmail"]).Returns(SupportEmail);
            _mockConfiguration.Setup(c => c["FrontendUrl"]).Returns("https://fe.test");
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _handler = new RequestUnblockCommandHandler(
                _mockUnitOfWork.Object,
                _mockEmailService.Object,
                _mockConfiguration.Object,
                _memoryCache);
        }

        private void SetupData(User? user, int rejectionCount)
        {
            var users = user == null ? new List<User>() : new List<User> { user };
            _mockUserRepository.Setup(r => r.Entities)
                .Returns(MockQueryableHelper.GetMockIQueryable(users).Object);

            var rejections = Enumerable.Range(0, rejectionCount)
                .Select(i => new UserRejection
                {
                    Id = $"rej-{i}",
                    UserId = user?.Id ?? "no-user",
                    Reason = $"Ly do tu choi {i}",
                    CreatedTime = DateTimeOffset.UtcNow.AddMinutes(i)
                })
                .ToList();
            _mockUserRejectionRepository.Setup(r => r.Entities)
                .Returns(MockQueryableHelper.GetMockIQueryable(rejections).Object);
        }

        private static RequestUnblockCommand BuildCommand(string email) => new RequestUnblockCommand
        {
            Model = new RequestUnblockRequestModel { Email = email }
        };

        [Fact]
        public async Task Handle_WhenAccountIsLocked_ShouldSendEmailToOrganizerAndUser()
        {
            // Arrange: tài khoản bị khóa (2 lần từ chối)
            var user = new User { Id = "user-1", Email = TestEmail, FullName = "Blocked User" };
            SetupData(user, rejectionCount: 2);

            // Act
            var result = await _handler.Handle(BuildCommand(TestEmail), CancellationToken.None);

            // Assert: trả thông báo + gửi 2 mail (BTC + xác nhận cho user)
            result.Value.Message.Should().NotBeNullOrEmpty();
            _mockEmailService.Verify(e => e.SendEmailAsync(SupportEmail, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockEmailService.Verify(e => e.SendEmailAsync(TestEmail, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenEmailNotFound_ShouldReturnGenericMessageWithoutSendingEmail()
        {
            // Arrange: email không tồn tại
            SetupData(null, rejectionCount: 0);

            // Act
            var result = await _handler.Handle(BuildCommand("ghost@example.com"), CancellationToken.None);

            // Assert: vẫn trả thông báo chung (chống dò email), không gửi mail
            result.Value.Message.Should().NotBeNullOrEmpty();
            _mockEmailService.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenNotLockedYet_ShouldNotSendEmail()
        {
            // Arrange: mới bị từ chối 1 lần -> chưa bị khóa
            var user = new User { Id = "user-1", Email = TestEmail, FullName = "One Reject" };
            SetupData(user, rejectionCount: 1);

            // Act
            await _handler.Handle(BuildCommand(TestEmail), CancellationToken.None);

            // Assert
            _mockEmailService.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenCalledTwiceWithin24h_ShouldThrottleSecondRequest()
        {
            // Arrange: tài khoản bị khóa
            var user = new User { Id = "user-1", Email = TestEmail, FullName = "Blocked User" };
            SetupData(user, rejectionCount: 2);

            // Act: gọi 2 lần liên tiếp
            await _handler.Handle(BuildCommand(TestEmail), CancellationToken.None);
            await _handler.Handle(BuildCommand(TestEmail), CancellationToken.None);

            // Assert: chỉ gửi cho BTC đúng 1 lần (lần 2 bị chặn bởi rate-limit)
            _mockEmailService.Verify(e => e.SendEmailAsync(SupportEmail, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
