using FluentAssertions;
using Moq;
using SEAL.Tests.Application.Helpers;
using SEAL_Application.Features.Teams.Commands.DeleteTeam;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Collections.Generic;
using Xunit;

namespace SEAL.Tests.Application.Features.Teams.Commands
{
    public class DeleteTeamCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Team>> _mockTeamRepository;
        private readonly Mock<IGenericRepository<SubmitResult>> _mockSubmitResultRepository;
        private readonly Mock<IGenericRepository<EventRole>> _mockEventRoleRepository;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IEventRoleChecker> _mockEventRoleChecker;
        private readonly DeleteTeamCommandHandler _handler;

        public DeleteTeamCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTeamRepository = new Mock<IGenericRepository<Team>>();
            _mockSubmitResultRepository = new Mock<IGenericRepository<SubmitResult>>();
            _mockEventRoleRepository = new Mock<IGenericRepository<EventRole>>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockEventRoleChecker = new Mock<IEventRoleChecker>();

            _mockUnitOfWork
                .Setup(u => u.GetRepository<Team>())
                .Returns(_mockTeamRepository.Object);

            _mockUnitOfWork
                .Setup(u => u.GetRepository<SubmitResult>())
                .Returns(_mockSubmitResultRepository.Object);

            _mockUnitOfWork
                .Setup(u => u.GetRepository<EventRole>())
                .Returns(_mockEventRoleRepository.Object);

            // Handler đọc GetRepository<User>().GetByIdAsync(currentUserId) để kiểm tra IsAdmin
            _mockUnitOfWork
                .Setup(u => u.GetRepository<User>())
                .Returns(_mockUserRepository.Object);

            // Mock CurrentUserService UserId
            _mockCurrentUserService
                .Setup(s => s.UserId)
                .Returns("user-001");

            // Người dùng hiện tại là tài khoản thường (không phải Admin); quyền xóa dựa vào vai trò TeamLeader bên dưới
            _mockUserRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<object>()))
                .ReturnsAsync(new User { Id = "user-001", IsAdmin = false });

            // Mock EventRole repository check leader mặc định trả về true
            _mockEventRoleRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EventRole, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockEventRoleChecker
                .Setup(c => c.HasRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EventRoleType[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Handler calls Entities.Where(...).ToListAsync(...) to gỡ EventRole của team trước khi xóa;
            // mock để hỗ trợ EF InMemory async provider (mặc định không có role nào).
            _mockEventRoleRepository
                .Setup(r => r.Entities)
                .Returns(MockQueryableHelper.GetMockIQueryable(new List<EventRole>()).Object);

            _handler = new DeleteTeamCommandHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockEventRoleChecker.Object);
        }

        [Fact]
        public async Task Handle_WithValidTeam_ShouldDeleteSuccessfully()
        {
            // Arrange
            var teamId = "team-001";
            var team = new Team
            {
                Id = teamId,
                Name = "Team to Delete",
                Description = "Description",
                IsActive = true
            };

            var command = new DeleteTeamCommand(teamId);

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);

            _mockSubmitResultRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SubmitResult, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Value.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.Delete(team), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentTeam_ShouldThrowException()
        {
            // Arrange
            var teamId = "non-existent-team";
            var command = new DeleteTeamCommand(teamId);

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync((Team)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.Delete(It.IsAny<Team>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithTeamHavingSubmissions_ShouldThrowException()
        {
            // Arrange
            var teamId = "team-001";
            var team = new Team
            {
                Id = teamId,
                Name = "Team with Submissions",
                Description = "Description",
                IsActive = true
            };

            var command = new DeleteTeamCommand(teamId);

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);

            _mockSubmitResultRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SubmitResult, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.Delete(It.IsAny<Team>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithMultipleSubmissions_ShouldPreventDeletion()
        {
            // Arrange
            var teamId = "team-001";
            var team = new Team
            {
                Id = teamId,
                Name = "Team with Multiple Submissions",
                Description = "Description",
                IsActive = true
            };

            var command = new DeleteTeamCommand(teamId);

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);

            // Simulate multiple submissions
            _mockSubmitResultRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SubmitResult, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldVerifySubmitResultsBeforeDeletion()
        {
            // Arrange
            var teamId = "team-001";
            var team = new Team
            {
                Id = teamId,
                Name = "Team",
                Description = "Description",
                IsActive = true
            };

            var command = new DeleteTeamCommand(teamId);

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);

            _mockSubmitResultRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SubmitResult, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockSubmitResultRepository.Verify(
                r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SubmitResult, bool>>>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCallSaveChangesOnce()
        {
            // Arrange
            var teamId = "team-001";
            var team = new Team
            {
                Id = teamId,
                Name = "Team",
                Description = "Description",
                IsActive = true
            };

            var command = new DeleteTeamCommand(teamId);

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(team);

            _mockSubmitResultRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SubmitResult, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
