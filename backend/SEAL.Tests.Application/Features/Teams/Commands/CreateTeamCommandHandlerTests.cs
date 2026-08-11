using FluentAssertions;
using Moq;
using SEAL.Tests.Application.Helpers;
using SEAL_Application.Features.Teams.Commands.CreateTeam;
using SEAL_Application.Features.Teams.Commands.CreateTeam.Models;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SEAL.Tests.Application.Features.Teams.Commands
{
    public class CreateTeamCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<Team>> _mockTeamRepository;
        private readonly Mock<IGenericRepository<Event>> _mockEventRepository;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;
        private readonly Mock<IGenericRepository<EventRole>> _mockEventRoleRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly CreateTeamCommandHandler _handler;

        // Người dùng đang đăng nhập, dùng chung cho các test
        private const string CurrentUserId = "user-001";

        public CreateTeamCommandHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTeamRepository = new Mock<IGenericRepository<Team>>();
            _mockEventRepository = new Mock<IGenericRepository<Event>>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();
            _mockEventRoleRepository = new Mock<IGenericRepository<EventRole>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.Setup(u => u.GetRepository<Team>()).Returns(_mockTeamRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Event>()).Returns(_mockEventRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<EventRole>()).Returns(_mockEventRoleRepository.Object);

            // Đã đăng nhập
            _mockCurrentUserService.Setup(s => s.UserId).Returns(CurrentUserId);

            // Người tạo là tài khoản đã được ban tổ chức phê duyệt
            _mockUserRepository
                .Setup(r => r.GetByIdAsync(CurrentUserId))
                .ReturnsAsync(new User { Id = CurrentUserId, IsApproved = true });

            // Mặc định: user không giữ vai trò tổ chức và chưa tham gia đội nào khác trong sự kiện
            _mockEventRoleRepository
                .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<EventRole, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Handler dùng GetQueryable().CountAsync(...) để kiểm tra giới hạn MaxTeams;
            // mặc định danh sách rỗng (0 đội hiện có) để không chạm giới hạn trong các test không liên quan.
            _mockTeamRepository
                .Setup(r => r.GetQueryable())
                .Returns(MockQueryableHelper.GetMockIQueryable(new List<Team>()).Object);

            _handler = new CreateTeamCommandHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_WithValidInput_ShouldCreateTeamSuccessfully()
        {
            // Arrange
            var eventId = "event-001";
            var command = new CreateTeamCommand
            {
                Model = new CreateTeamRequestModel
                {
                    Name = "Development Team",
                    Description = "Team for development",
                    EventId = eventId,
                    LeaderId = "leader-001"
                }
            };

            // Event tồn tại (không đặt mốc thời gian đăng ký -> luôn trong thời gian cho phép)
            _mockEventRepository
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(new Event { Id = eventId, MaxTeams = 100 });

            // Tên nhóm chưa tồn tại
            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockTeamRepository
                .Setup(r => r.AddAsync(It.IsAny<Team>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Name.Should().Be(command.Model.Name);
            result.Value.Description.Should().Be(command.Model.Description);
            result.Value.IsActive.Should().BeTrue();

            _mockTeamRepository.Verify(r => r.AddAsync(It.IsAny<Team>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentEvent_ShouldThrowException()
        {
            // Arrange
            var command = new CreateTeamCommand
            {
                Model = new CreateTeamRequestModel
                {
                    Name = "Development Team",
                    Description = "Team for development",
                    EventId = "non-existent-event",
                    LeaderId = "leader-001"
                }
            };

            // Event không tồn tại
            _mockEventRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<object>()))
                .ReturnsAsync((Event?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.AddAsync(It.IsAny<Team>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithDuplicateTeamName_ShouldThrowException()
        {
            // Arrange
            var eventId = "event-001";
            var command = new CreateTeamCommand
            {
                Model = new CreateTeamRequestModel
                {
                    Name = "Development Team",
                    Description = "Team for development",
                    EventId = eventId,
                    LeaderId = "leader-001"
                }
            };

            // Event tồn tại
            _mockEventRepository
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(new Event { Id = eventId, MaxTeams = 100 });

            // Tên nhóm đã tồn tại
            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            _mockTeamRepository.Verify(r => r.AddAsync(It.IsAny<Team>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithEmptyDescription_ShouldCreateTeamSuccessfully()
        {
            // Arrange
            var eventId = "event-001";
            var command = new CreateTeamCommand
            {
                Model = new CreateTeamRequestModel
                {
                    Name = "Development Team",
                    Description = string.Empty,
                    EventId = eventId,
                    LeaderId = "leader-001"
                }
            };

            _mockEventRepository
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(new Event { Id = eventId, MaxTeams = 100 });

            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockTeamRepository
                .Setup(r => r.AddAsync(It.IsAny<Team>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Description.Should().Be(string.Empty);

            _mockTeamRepository.Verify(r => r.AddAsync(It.IsAny<Team>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCallSaveChangesOnce()
        {
            // Arrange
            var eventId = "event-001";
            var command = new CreateTeamCommand
            {
                Model = new CreateTeamRequestModel
                {
                    Name = "Development Team",
                    Description = "Team for development",
                    EventId = eventId,
                    LeaderId = "leader-001"
                }
            };

            _mockEventRepository
                .Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(new Event { Id = eventId, MaxTeams = 100 });

            _mockTeamRepository
                .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Team, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockTeamRepository
                .Setup(r => r.AddAsync(It.IsAny<Team>()))
                .Returns(Task.CompletedTask);

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
