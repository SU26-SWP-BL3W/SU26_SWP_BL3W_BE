using FluentAssertions;
using Moq;
using SEAL_Application.Features.EventRoles;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL.Tests.Application.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SEAL.Tests.Application.Features.EventRoles
{
    public class EventRoleValidationHelperTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<EventRole>> _mockEventRoleRepo;

        public EventRoleValidationHelperTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEventRoleRepo = new Mock<IGenericRepository<EventRole>>();
            _mockUnitOfWork.Setup(u => u.GetRepository<EventRole>()).Returns(_mockEventRoleRepo.Object);
        }

        [Fact]
        public async Task CheckRoleConflict_WhenNoExistingRoles_ShouldReturnNull()
        {
            // Arrange
            var rolesList = new List<EventRole>();
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(rolesList);
            _mockEventRoleRepo.Setup(r => r.GetQueryable()).Returns(mockQueryable.Object);

            // Act
            var result = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _mockUnitOfWork.Object, "user1", "event1", EventRoleType.Judge, "track1", null, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CheckRoleConflict_WhenUserAlreadyHasSameRoleOnSameTrack_ShouldReturnConflict()
        {
            // Arrange
            var rolesList = new List<EventRole>
            {
                new EventRole { UserId = "user1", EventId = "event1", TrackId = "track1", RoleName = EventRoleType.Judge }
            };
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(rolesList);
            _mockEventRoleRepo.Setup(r => r.GetQueryable()).Returns(mockQueryable.Object);

            // Act
            var result = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _mockUnitOfWork.Object, "user1", "event1", EventRoleType.Judge, "track1", null, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("đã có vai trò");
        }

        [Fact]
        public async Task CheckRoleConflict_WhenStudentTriesToBecomeJudge_ShouldReturnConflict()
        {
            // Arrange
            var rolesList = new List<EventRole>
            {
                new EventRole { UserId = "user1", EventId = "event1", TeamId = "team1", RoleName = EventRoleType.TeamMember }
            };
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(rolesList);
            _mockEventRoleRepo.Setup(r => r.GetQueryable()).Returns(mockQueryable.Object);

            // Act
            var result = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _mockUnitOfWork.Object, "user1", "event1", EventRoleType.Judge, "track1", null, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("Thí sinh");
        }

        [Fact]
        public async Task CheckRoleConflict_WhenJudgeTriesToBecomeMentorOnSameTrack_ShouldReturnConflict()
        {
            // Arrange
            var rolesList = new List<EventRole>
            {
                new EventRole { UserId = "user1", EventId = "event1", TrackId = "track1", RoleName = EventRoleType.Judge }
            };
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(rolesList);
            _mockEventRoleRepo.Setup(r => r.GetQueryable()).Returns(mockQueryable.Object);

            // Act
            var result = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _mockUnitOfWork.Object, "user1", "event1", EventRoleType.Mentor, "track1", null, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("không thể vừa làm");
        }

        [Fact]
        public async Task CheckRoleConflict_WhenJudgeOnTrack1BecomesMentorOnTrack2_ShouldAllow()
        {
            // Arrange
            var rolesList = new List<EventRole>
            {
                new EventRole { UserId = "user1", EventId = "event1", TrackId = "track1", RoleName = EventRoleType.Judge }
            };
            var mockQueryable = MockQueryableHelper.GetMockIQueryable(rolesList);
            _mockEventRoleRepo.Setup(r => r.GetQueryable()).Returns(mockQueryable.Object);

            // Act
            var result = await EventRoleValidationHelper.CheckRoleConflictAsync(
                _mockUnitOfWork.Object, "user1", "event1", EventRoleType.Mentor, "track2", null, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
