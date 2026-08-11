using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SEAL.Tests.Application.Helpers;
using SEAL_Application.Features.Users.Commands.UpdateStudentProfile;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL.Tests.Application.Features.Users.Commands
{
    /// <summary>
    /// Kiểm thử Business Rule (bước "1b" của UpdateStudentProfileCommandHandler):
    /// nếu hồ sơ đã bị từ chối >= 2 lần thì KHÔNG được cập nhật hồ sơ nữa.
    /// </summary>
    public class UpdateStudentProfileCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;
        private readonly Mock<IGenericRepository<UserRejection>> _mockUserRejectionRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly UpdateStudentProfileCommandHandler _handler;

        private const string TestUserId = "user-001";

        public UpdateStudentProfileCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();
            _mockUserRejectionRepository = new Mock<IGenericRepository<UserRejection>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<UserRejection>()).Returns(_mockUserRejectionRepository.Object);

            _mockCurrentUserService.Setup(c => c.UserId).Returns(TestUserId);

            _handler = new UpdateStudentProfileCommandHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object);
        }

        /// <summary>
        /// Giả lập 1 user hợp lệ + số lần đã bị từ chối (rejectionCount) trong bảng UserRejection.
        /// CountAsync(r => r.UserId == userId) trên mock queryable sẽ trả về đúng rejectionCount.
        /// </summary>
        private void SetupUserWithRejections(int rejectionCount)
        {
            _mockUserRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<object>()))
                .ReturnsAsync(new User { Id = TestUserId, Email = "test@example.com", FullName = "Nguyen Van Test" });

            var rejections = Enumerable.Range(0, rejectionCount)
                .Select(i => new UserRejection { Id = $"rej-{i}", UserId = TestUserId, IsActive = true })
                .ToList();

            _mockUserRejectionRepository
                .Setup(r => r.Entities)
                .Returns(MockQueryableHelper.GetMockIQueryable(rejections).Object);
        }

        [Fact]
        public async Task Handle_WhenRejectedExactlyTwice_ShouldThrowForbiddenAndNotWriteToDb()
        {
            // Arrange: hồ sơ đã bị từ chối đúng 2 lần
            SetupUserWithRejections(2);
            var command = new UpdateStudentProfileCommand { IsFpt = false };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert: bị chặn, trả về Result thất bại
            result.IsFailure.Should().BeTrue();
            result.Error!.ErrorMessage!.ToString().Should().Contain("2 lần");

            // Bị chặn ngay từ đầu nên KHÔNG được ghi gì xuống DB
            _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRejectedMoreThanTwice_ShouldAlsoThrowForbidden()
        {
            // Arrange: đã bị từ chối 3 lần -> vẫn phải chặn (điều kiện là >= 2, không phải == 2)
            SetupUserWithRejections(3);
            var command = new UpdateStudentProfileCommand { IsFpt = false };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRejectedOnce_ShouldNotBeBlockedByTwoTimeRule()
        {
            // Arrange: mới bị từ chối 1 lần -> KHÔNG bị chặn bởi luật 2 lần.
            // Dùng hồ sơ ngoài FPT + thiếu ảnh thẻ để handler đi QUA block rồi mới trả về lỗi
            // "Ảnh thẻ..." ở bước validate kế tiếp, chứ không phải lỗi 2 lần từ chối.
            SetupUserWithRejections(1);
            var command = new UpdateStudentProfileCommand
            {
                IsFpt = false,
                PhotoStudentCardUrl = string.Empty // thiếu ảnh thẻ -> lỗi ở bước 2
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert: lỗi là "Ảnh thẻ...", KHÔNG phải lỗi của luật 2 lần.
            result.IsFailure.Should().BeTrue();
            var message = result.Error!.ErrorMessage!.ToString();
            message.Should().Contain("Ảnh thẻ");
            message.Should().NotContain("2 lần");
        }
    }
}
