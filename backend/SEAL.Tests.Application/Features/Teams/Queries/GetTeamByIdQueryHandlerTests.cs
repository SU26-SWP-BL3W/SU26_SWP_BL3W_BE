using FluentAssertions;
using Moq;
using SEAL_Application.Features.Teams.Queries.GetTeamById;
using SEAL_Application.Features.Teams.Queries.GetTeamById.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL.Tests.Application.Helpers;
using System.Collections.Generic;
using Xunit;

namespace SEAL.Tests.Application.Features.Teams.Queries
{
    public class GetTeamByIdQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Team>> _mockTeamRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IEventRoleChecker> _mockEventRoleChecker;
        private readonly GetTeamByIdQueryHandler _handler;

        public GetTeamByIdQueryHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTeamRepository = new Mock<IGenericRepository<Team>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockEventRoleChecker = new Mock<IEventRoleChecker>();

            _mockUnitOfWork
                .Setup(u => u.GetRepository<Team>())
                .Returns(_mockTeamRepository.Object);

            _handler = new GetTeamByIdQueryHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockEventRoleChecker.Object);
        }

        // Handler dùng _unitOfWork.GetRepository<Team>().Entities.Include(...).FirstOrDefaultAsync(...)
        // nên phải mock .Entities bằng IQueryable hỗ trợ async (MockQueryableHelper) thay vì GetByIdAsync.
        private void SetupTeams(params Team[] teams)
        {
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(new List<Team>(teams));
            _mockTeamRepository.Setup(r => r.Entities).Returns(mockQueryable.Object);
        }

        [Fact]
        public async Task Handle_WithValidTeamId_ShouldReturnTeamWithAllProperties()
        {
            // Arrange
            var teamId = "abc123def456ghi789";
            var eventId = "event-001";
            var createdTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
            var createdBy = "admin-user";

            var team = new Team
            {
                Id = teamId,
                EventId = eventId,
                Name = "Development Team",
                Description = "Team responsible for core development",
                IsActive = true,
                CreatedTime = createdTime,
                CreatedBy = createdBy
            };

            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(team);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Id.Should().Be(teamId);
            result.Value.Id.Should().NotBeEmpty();
            result.Value.Name.Should().Be("Development Team");
            result.Value.Name.Should().NotBeNullOrEmpty();
            result.Value.Description.Should().Be("Team responsible for core development");
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedTime.Should().Be(createdTime);

            _mockTeamRepository.Verify(r => r.Entities, Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_WithNonExistentTeamId_ShouldThrowBaseException()
        {
            // Arrange
            var teamId = "non-existent-id-12345";
            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(); // không có đội nào -> FirstOrDefault trả null

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error!.ErrorMessage!.ToString().Should().Contain("Nhóm không tồn tại");

            _mockTeamRepository.Verify(r => r.Entities, Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_WithNullTeamIdResponse_ShouldThrowException()
        {
            // Arrange
            var teamId = "team-null-test";
            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(); // không có đội khớp -> null -> ném lỗi

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.Entities, Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_WithInactiveTeam_ShouldReturnInactiveStatus()
        {
            // Arrange
            var teamId = "inactive-team-id";
            var createdTime = new DateTimeOffset(2023, 6, 15, 14, 30, 0, TimeSpan.Zero);

            var team = new Team
            {
                Id = teamId,
                EventId = "event-002",
                Name = "Legacy QA Team",
                Description = "Inactive quality assurance team",
                IsActive = false,
                CreatedTime = createdTime
            };

            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(team);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.IsActive.Should().BeFalse();
            result.Value.Id.Should().Be(teamId);
            result.Value.Name.Should().Be("Legacy QA Team");
            result.Value.CreatedTime.Should().Be(createdTime);
        }

        [Fact]
        public async Task Handle_WithNullDescription_ShouldMapNullableFieldCorrectly()
        {
            // Arrange
            var teamId = "team-no-description";
            var createdTime = new DateTimeOffset(2024, 2, 20, 9, 15, 0, TimeSpan.Zero);

            var team = new Team
            {
                Id = teamId,
                EventId = "event-003",
                Name = "Minimal Team",
                Description = null,
                IsActive = true,
                CreatedTime = createdTime
            };

            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(team);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Id.Should().Be(teamId);
            result.Value.Name.Should().Be("Minimal Team");
            // Handler map Description null -> string.Empty (Description ?? string.Empty)
            result.Value.Description.Should().BeEmpty();
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedTime.Should().Be(createdTime);
        }

        [Fact]
        public async Task Handle_ShouldMapAllPropertiesAccurately()
        {
            // Arrange
            var teamId = "complete-team-id";
            var eventId = "complete-event-id";
            var name = "Complete Mapping Team";
            var description = "Testing complete property mapping";
            var isActive = true;
            var createdTime = new DateTimeOffset(2024, 3, 10, 11, 45, 30, TimeSpan.Zero);

            var team = new Team
            {
                Id = teamId,
                EventId = eventId,
                Name = name,
                Description = description,
                IsActive = isActive,
                CreatedTime = createdTime
            };

            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(team);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Id.Should().Be(teamId);
            result.Value.Name.Should().Be(name);
            result.Value.Description.Should().Be(description);
            result.Value.IsActive.Should().Be(isActive);
            result.Value.CreatedTime.Should().Be(createdTime);
            result.Value.EventId.Should().Be(eventId);

            result.Value.Id.Should().NotBeEmpty();
            result.Value.Name.Should().NotBeNullOrEmpty();

            _mockTeamRepository.Verify(r => r.Entities, Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_WithEmptyStringTeamId_ShouldAttemptToRetrieveAndHandleGracefully()
        {
            // Arrange
            var teamId = string.Empty;
            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(); // không có đội khớp id rỗng -> null -> ném lỗi

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();

            _mockTeamRepository.Verify(r => r.Entities, Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_ResponseModel_ShouldHaveCorrectTypes()
        {
            // Arrange
            var teamId = "type-check-team";
            var createdTime = new DateTimeOffset(2024, 4, 5, 8, 30, 0, TimeSpan.Zero);

            var team = new Team
            {
                Id = teamId,
                EventId = "event-type-check",
                Name = "Type Check Team",
                Description = "Verifying response model types",
                IsActive = false,
                CreatedTime = createdTime
            };

            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(team);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Should().BeOfType<TeamDetailResponseModel>();
            result.Value.Id.Should().BeOfType<string>();
            result.Value.Name.Should().BeOfType<string>();
            result.Value.Description.Should().BeOfType<string>();
        }

        [Fact]
        public async Task Handle_RepositoryCallCount_ShouldQueryEntitiesOnce()
        {
            // Arrange
            var teamId = "call-count-team";
            var createdTime = DateTimeOffset.UtcNow;

            var team = new Team
            {
                Id = teamId,
                EventId = "event-call-count",
                Name = "Call Count Team",
                Description = "Testing repository call count",
                IsActive = true,
                CreatedTime = createdTime
            };

            var query = new GetTeamByIdQuery(teamId);
            SetupTeams(team);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _mockTeamRepository.Verify(r => r.Entities, Times.Once);
        }
    }
}
