using FluentAssertions;
using Moq;
using SEAL_Application.Features.Events.Commands.CreateEvent;
using SEAL_Application.Features.Events.Commands.CreateEvent.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL.Tests.Application.Helpers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SEAL.Tests.Application.Features.Events
{
    public class CreateEventCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IGenericRepository<User>> _mockUserRepo;
        private readonly Mock<IGenericRepository<Event>> _mockEventRepo;
        private readonly CreateEventCommandHandler _handler;

        public CreateEventCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUserRepo = new Mock<IGenericRepository<User>>();
            _mockEventRepo = new Mock<IGenericRepository<Event>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Event>()).Returns(_mockEventRepo.Object);

            _handler = new CreateEventCommandHandler(_mockUnitOfWork.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.UserId).Returns(string.Empty);
            var command = new CreateEventCommand
            {
                Model = new CreateEventRequestModel { EventName = "Test Event", Year = 2026 }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Handle_WhenEventNameDuplicateInSameYear_ShouldReturnDuplicateError()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.UserId).Returns("user1");
            _mockUserRepo.Setup(r => r.GetByIdAsync("user1")).ReturnsAsync(new User { Id = "user1", IsAdmin = true });

            var existingEvents = new List<Event>
            {
                new Event { EventName = "Hackathon 2026", Year = 2026 }
            };
            _mockEventRepo.Setup(r => r.Entities).Returns(MockQueryableHelper.GetMockIQueryable(existingEvents).Object);

            var command = new CreateEventCommand
            {
                Model = new CreateEventRequestModel { EventName = "Hackathon 2026", Year = 2026 }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.StatusCode.Should().Be(400);
            result.Error?.ErrorCode.Should().Be(SEAL_Domain.Store.Constants.ResponseCodeConstants.DUPLICATE);
        }

        [Fact]
        public async Task Handle_WhenCreatingEventWithoutRounds_ShouldSucceed()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.UserId).Returns("user1");
            _mockUserRepo.Setup(r => r.GetByIdAsync("user1")).ReturnsAsync(new User { Id = "user1", IsAdmin = true });

            var mockEventRoleRepo = new Mock<IGenericRepository<EventRole>>();
            _mockUnitOfWork.Setup(u => u.GetRepository<EventRole>()).Returns(mockEventRoleRepo.Object);

            var existingEvents = new List<Event>();
            _mockEventRepo.Setup(r => r.Entities).Returns(MockQueryableHelper.GetMockIQueryable(existingEvents).Object);

            var command = new CreateEventCommand
            {
                Model = new CreateEventRequestModel
                {
                    EventName = "Event Without Rounds",
                    Year = 2026,
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(10),
                    Rounds = new List<RoundRequestDto>() // No rounds configured
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.EventName.Should().Be("Event Without Rounds");
            result.Value.Rounds.Should().BeEmpty();
            _mockEventRepo.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
        }
    }
}
