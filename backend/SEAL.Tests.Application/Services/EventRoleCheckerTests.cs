using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SEAL_Application.Interfaces;
using SEAL_Application.Services;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using SEAL.Tests.Application.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SEAL.Tests.Application.Services
{
    public class EventRoleCheckerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<User>> _mockUserRepo;
        private readonly Mock<IGenericRepository<EventRole>> _mockEventRoleRepo;
        private readonly IMemoryCache _memoryCache;
        private readonly EventRoleChecker _checker;

        public EventRoleCheckerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepo = new Mock<IGenericRepository<User>>();
            _mockEventRoleRepo = new Mock<IGenericRepository<EventRole>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<EventRole>()).Returns(_mockEventRoleRepo.Object);

            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _checker = new EventRoleChecker(_mockUnitOfWork.Object, _memoryCache);
        }

        [Fact]
        public async Task HasRoleAsync_WhenUserIsAdmin_ShouldAlwaysReturnTrue()
        {
            // Arrange
            var adminUser = new User { Id = "admin1", IsAdmin = true };
            var usersList = new List<User> { adminUser };
            _mockUserRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(usersList).Object);

            // Act
            var result = await _checker.HasRoleAsync("admin1", "event1", new[] { EventRoleType.EventCoordinator });

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasRoleAsync_WhenUserHasEventLevelRole_ShouldReturnTrue()
        {
            // Arrange
            var normalUser = new User { Id = "user1", IsAdmin = false };
            var usersList = new List<User> { normalUser };
            _mockUserRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(usersList).Object);

            var rolesList = new List<EventRole>
            {
                new EventRole { UserId = "user1", EventId = "event1", RoleName = EventRoleType.EventCoordinator }
            };
            _mockEventRoleRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(rolesList).Object);

            // Act
            var result = await _checker.HasRoleAsync("user1", "event1", new[] { EventRoleType.EventCoordinator });

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasRoleAsync_WhenUserHasTrackLevelRole_ShouldReturnTrueForThatTrack()
        {
            // Arrange
            var normalUser = new User { Id = "user1", IsAdmin = false };
            var usersList = new List<User> { normalUser };
            _mockUserRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(usersList).Object);

            var rolesList = new List<EventRole>
            {
                new EventRole { UserId = "user1", EventId = "event1", TrackId = "track1", RoleName = EventRoleType.Judge }
            };
            _mockEventRoleRepo.Setup(r => r.GetQueryable()).Returns(MockQueryableHelper.GetMockIQueryable(rolesList).Object);

            // Act
            var resultTrack1 = await _checker.HasRoleAsync("user1", "event1", new[] { EventRoleType.Judge }, trackId: "track1");
            var resultTrack2 = await _checker.HasRoleAsync("user1", "event1", new[] { EventRoleType.Judge }, trackId: "track2");

            // Assert
            resultTrack1.Should().BeTrue();
            resultTrack2.Should().BeFalse();
        }

        [Fact]
        public void InvalidateCache_ShouldClearCachedRoles()
        {
            // Arrange
            var cacheKey = "EventRole_user1_event1";
            _memoryCache.Set(cacheKey, new[] { new UserEventRoleDto { RoleName = EventRoleType.Judge } });

            // Act
            _checker.InvalidateCache("user1", "event1");

            // Assert
            _memoryCache.TryGetValue(cacheKey, out _).Should().BeFalse();
        }
    }
}
