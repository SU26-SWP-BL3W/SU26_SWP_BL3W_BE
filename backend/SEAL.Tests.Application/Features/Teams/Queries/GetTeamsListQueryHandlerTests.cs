using FluentAssertions;
using Moq;
using SEAL_Application.Commons;
using SEAL_Application.Features.Teams.Queries.GetTeamsList;
using SEAL_Application.Features.Teams.Queries.GetTeamsList.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL.Tests.Application.Helpers;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL.Tests.Application.Features.Teams.Queries
{
    public class GetTeamsListQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Team>> _mockTeamRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IGenericRepository<EventRole>> _mockEventRoleRepository;
        private readonly GetTeamsListQueryHandler _handler;

        public GetTeamsListQueryHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTeamRepository = new Mock<IGenericRepository<Team>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockEventRoleRepository = new Mock<IGenericRepository<EventRole>>();

            _mockUnitOfWork
                .Setup(u => u.GetRepository<Team>())
                .Returns(_mockTeamRepository.Object);

            _mockUnitOfWork
                .Setup(u => u.GetRepository<EventRole>())
                .Returns(_mockEventRoleRepository.Object);

            _handler = new GetTeamsListQueryHandler(_mockUnitOfWork.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_WithMultipleTeams_ShouldReturnAllTeams()
        {
            // Arrange
            var teams = new List<Team>
            {
                new Team
                {
                    Id = "team-001",
                    Name = "Development Team",
                    Description = "Team for development",
                    IsActive = true,
                    CreatedTime = DateTime.UtcNow
                },
                new Team
                {
                    Id = "team-002",
                    Name = "QA Team",
                    Description = "Team for QA",
                    IsActive = true,
                    CreatedTime = DateTime.UtcNow
                },
                new Team
                {
                    Id = "team-003",
                    Name = "DevOps Team",
                    Description = "Team for DevOps",
                    IsActive = false,
                    CreatedTime = DateTime.UtcNow
                }
            };

            var query = new GetTeamsListQuery();
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Data.Should().HaveCount(3);
            result.Value.Data.Should().AllSatisfy(t => t.Should().BeOfType<TeamListItemModel>());
            result.Value.TotalItems.Should().Be(3);

            _mockTeamRepository.Verify(r => r.Entities, Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_WithEmptyTeamList_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetTeamsListQuery();
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(new List<Team>());

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Data.Should().BeEmpty();
            result.Value.TotalItems.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ShouldMapTeamPropertiesCorrectly()
        {
            // Arrange
            var createdTime = new DateTime(2024, 1, 1, 10, 0, 0);
            var teams = new List<Team>
            {
                new Team
                {
                    Id = "team-001",
                    Name = "Test Team",
                    Description = "Test Description",
                    IsActive = true,
                    CreatedTime = createdTime
                }
            };

            var query = new GetTeamsListQuery();
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dataList = result.Value.Data.ToList();
            dataList.Should().HaveCount(1);
            dataList[0].Id.Should().Be("team-001");
            dataList[0].Name.Should().Be("Test Team");
            dataList[0].Description.Should().Be("Test Description");
            dataList[0].IsActive.Should().BeTrue();
            dataList[0].CreatedTime.Should().Be(createdTime);
        }

        [Fact]
        public async Task Handle_WithInactiveTeams_ShouldIncludeInResult()
        {
            // Arrange
            var teams = new List<Team>
            {
                new Team
                {
                    Id = "team-001",
                    Name = "Active Team",
                    Description = "Active",
                    IsActive = true
                },
                new Team
                {
                    Id = "team-002",
                    Name = "Inactive Team",
                    Description = "Inactive",
                    IsActive = false
                }
            };

            var query = new GetTeamsListQuery();
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Data.Should().HaveCount(2);
            result.Value.Data.Should().Contain(t => !t.IsActive);
        }

        [Fact]
        public async Task Handle_ShouldPreserveTeamOrder()
        {
            // Arrange
            var teams = new List<Team>
            {
                new Team { Id = "team-001", Name = "First Team", Description = "First" },
                new Team { Id = "team-002", Name = "Second Team", Description = "Second" },
                new Team { Id = "team-003", Name = "Third Team", Description = "Third" }
            };

            var query = new GetTeamsListQuery
            {
                SortBy = "Name",
                IsAscending = true
            };
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dataList = result.Value.Data.ToList();
            dataList[0].Name.Should().Be("First Team");
            dataList[1].Name.Should().Be("Second Team");
            dataList[2].Name.Should().Be("Third Team");
        }

        [Fact]
        public async Task Handle_WithSingleTeam_ShouldReturnList()
        {
            // Arrange
            var teams = new List<Team>
            {
                new Team
                {
                    Id = "team-001",
                    Name = "Only Team",
                    Description = "The only team",
                    IsActive = true
                }
            };

            var query = new GetTeamsListQuery();
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dataList = result.Value.Data.ToList();
            dataList.Should().HaveCount(1);
            dataList[0].Name.Should().Be("Only Team");
        }

        [Fact]
        public async Task Handle_ShouldReturnPagedResultOfTeamListItemModel()
        {
            // Arrange
            var teams = new List<Team>
            {
                new Team { Id = "team-001", Name = "Team", Description = "Description" }
            };

            var query = new GetTeamsListQuery();
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().BeOfType<PagedResult<TeamListItemModel>>();
        }

        [Fact]
        public async Task Handle_WithEventIdFilter_ShouldReturnOnlyTeamsOfThatEvent()
        {
            // Arrange
            var teams = new List<Team>
            {
                new Team { Id = "team-001", Name = "Team A", Description = "A", EventId = "event-001" },
                new Team { Id = "team-002", Name = "Team B", Description = "B", EventId = "event-002" },
                new Team { Id = "team-003", Name = "Team C", Description = "C", EventId = "event-001" }
            };

            var query = new GetTeamsListQuery { EventId = "event-001" };
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Data.Should().HaveCount(2);
            result.Value.Data.Should().OnlyContain(t => t.EventId == "event-001");
            result.Value.TotalItems.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithNonExistentEventId_ShouldReturnEmptyList()
        {
            // Arrange
            var teams = new List<Team>
            {
                new Team { Id = "team-001", Name = "Team A", Description = "A", EventId = "event-001" },
                new Team { Id = "team-002", Name = "Team B", Description = "B", EventId = "event-002" }
            };

            var query = new GetTeamsListQuery { EventId = "event-khong-ton-tai" };
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(teams);

            _mockTeamRepository
                .Setup(r => r.Entities)
                .Returns(mockQueryable.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value.Data.Should().BeEmpty();
            result.Value.TotalItems.Should().Be(0);
        }
    }
}
