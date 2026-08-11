using FluentAssertions;
using Moq;
using SEAL_Application.Features.Teams.Commands.UpdateTeam;
using SEAL_Application.Features.Teams.Commands.UpdateTeam.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using Xunit;

namespace SEAL.Tests.Application.Features.Teams.Commands
{
    public class UpdateTeamCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Team>> _mockTeamRepository;
        private readonly Mock<IGenericRepository<EventRole>> _mockEventRoleRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IEventRoleChecker> _mockEventRoleChecker;
        private readonly UpdateTeamCommandHandler _handler;

        public UpdateTeamCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTeamRepository = new Mock<IGenericRepository<Team>>();
            _mockEventRoleRepository = new Mock<IGenericRepository<EventRole>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockEventRoleChecker = new Mock<IEventRoleChecker>();

            _mockUnitOfWork
                .Setup(u => u.GetRepository<Team>())
                .Returns(_mockTeamRepository.Object);

            _mockUnitOfWork
                .Setup(u => u.GetRepository<EventRole>())
                .Returns(_mockEventRoleRepository.Object);

            // Mock CurrentUserService UserId
            _mockCurrentUserService
                .Setup(s => s.UserId)
                .Returns("user-001");

            // Mock EventRole repository check leader mặc định trả về true (người dùng là TeamLeader)
            _mockEventRoleRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EventRole, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Mặc định KHÔNG phải Event Coordinator
            _mockEventRoleChecker
                .Setup(c => c.HasRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EventRoleType[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _handler = new UpdateTeamCommandHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockEventRoleChecker.Object);
        }

        [Fact]
        public async Task Handle_WithValidInput_ShouldUpdateTeamSuccessfully()
        {
            // Arrange
            var teamId = "team-001";
            var existingTeam = new Team
            {
                Id = teamId,
                Name = "Old Team Name",
                Description = "Old Description",
                IsActive = true
            };

            // Handler truy vấn bằng request.Id nên command.Id phải được set (không chỉ Model.Id)
            var command = new UpdateTeamCommand
            {
                Id = teamId,
                Model = new UpdateTeamRequestModel
                {
                    Id = teamId,
                    Name = "New Team Name",
                    Description = "New Description",
                    IsActive = true
                }
            };

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(existingTeam);

            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Name.Should().Be(command.Model.Name);
            result.Value.Description.Should().Be(command.Model.Description);

            _mockTeamRepository.Verify(r => r.Update(It.IsAny<Team>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentTeam_ShouldThrowException()
        {
            // Arrange
            var teamId = "non-existent-team";
            var command = new UpdateTeamCommand
            {
                Id = teamId,
                Model = new UpdateTeamRequestModel
                {
                    Id = teamId,
                    Name = "New Team Name",
                    Description = "New Description",
                    IsActive = true
                }
            };

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync((Team?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.Update(It.IsAny<Team>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithDuplicateTeamName_ShouldThrowException()
        {
            // Arrange
            var teamId = "team-001";
            var existingTeam = new Team
            {
                Id = teamId,
                Name = "Old Team Name",
                Description = "Old Description",
                IsActive = true
            };

            var command = new UpdateTeamCommand
            {
                Id = teamId,
                Model = new UpdateTeamRequestModel
                {
                    Id = teamId,
                    Name = "Existing Team Name",
                    Description = "New Description",
                    IsActive = true
                }
            };

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(existingTeam);

            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.Update(It.IsAny<Team>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithSameNameAsCurrentTeam_ShouldUpdateSuccessfully()
        {
            // Arrange
            var teamId = "team-001";
            var existingTeam = new Team
            {
                Id = teamId,
                Name = "Team Name",
                Description = "Old Description",
                IsActive = true
            };

            var command = new UpdateTeamCommand
            {
                Id = teamId,
                Model = new UpdateTeamRequestModel
                {
                    Id = teamId,
                    Name = "Team Name",
                    Description = "New Description",
                    IsActive = true
                }
            };

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(existingTeam);

            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Name.Should().Be(command.Model.Name);
        }

        [Fact]
        public async Task Handle_ShouldUpdateAllFields()
        {
            // Arrange
            var teamId = "team-001";
            var existingTeam = new Team
            {
                Id = teamId,
                Name = "Old Team",
                Description = "Old Desc",
                IsActive = true
            };

            // Đổi IsActive (true -> false): theo nghiệp vụ chỉ Event Coordinator/Admin được phép,
            // nên test này đóng vai Event Coordinator.
            var command = new UpdateTeamCommand
            {
                Id = teamId,
                Model = new UpdateTeamRequestModel
                {
                    Id = teamId,
                    Name = "New Team",
                    Description = "New Desc",
                    IsActive = false
                }
            };

            _mockEventRoleChecker
                .Setup(c => c.HasRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EventRoleType[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockTeamRepository
                .Setup(r => r.GetByIdAsync(teamId))
                .ReturnsAsync(existingTeam);

            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Value.IsActive.Should().BeFalse();
        }
    }
}
