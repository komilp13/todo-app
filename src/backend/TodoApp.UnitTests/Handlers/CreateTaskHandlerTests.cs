using Moq;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.Domain.Interfaces;
using Xunit;

namespace TodoApp.UnitTests.Handlers;

/// <summary>
/// Unit tests for CreateTaskHandler.
/// </summary>
public class CreateTaskHandlerTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly CreateTaskHandler _handler;

    public CreateTaskHandlerTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockProjectRepository = new Mock<IProjectRepository>();
        _handler = new CreateTaskHandler(_mockTaskRepository.Object, _mockProjectRepository.Object);
    }

    private CreateTaskCommand CreateValidCommand(Guid? userId = null)
    {
        return new CreateTaskCommand
        {
            Name = "Test Task",
            Description = "Test Description",
            Priority = Priority.P2,
            SystemList = SystemList.Inbox,
            UserId = userId ?? Guid.NewGuid(),
            ProjectId = null,
            DueDate = null
        };
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAndReturnsTask()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = CreateValidCommand(userId);

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(userId, SystemList.Inbox.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(command.Name, response.Name);
        Assert.Equal(command.Description, response.Description);
        Assert.Equal(command.Priority, response.Priority);
        Assert.Equal(command.SystemList, response.SystemList);
        Assert.Equal(Domain.Enums.TaskStatus.Open, response.Status);
        Assert.False(response.IsArchived);
        Assert.Null(response.CompletedAt);
        Assert.NotEqual(Guid.Empty, response.Id);
    }

    [Fact]
    public async Task Handle_WithoutProjectId_CreatesTaskSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = CreateValidCommand(userId);
        command.ProjectId = null;

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(userId, SystemList.Inbox.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Null(response.ProjectId);
        _mockProjectRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidProjectId_ValidatesProjectOwnership()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var command = CreateValidCommand(userId);
        command.ProjectId = projectId;

        var project = Project.Create(userId, "Test Project", null, null);

        _mockProjectRepository
            .Setup(r => r.GetByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(userId, SystemList.Inbox.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(projectId, response.ProjectId);
        _mockProjectRepository.Verify(
            r => r.GetByIdAsync(projectId, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidProjectId_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var command = CreateValidCommand(userId);
        command.ProjectId = projectId;

        _mockProjectRepository
            .Setup(r => r.GetByIdAsync(projectId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task Handle_WithDifferentSystemLists_CalculatesSortOrderCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = CreateValidCommand(userId);
        command.SystemList = SystemList.Next;

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(userId, SystemList.Next.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(SystemList.Next, response.SystemList);

        _mockTaskRepository.Verify(
            r => r.GetMaxSortOrderAsync(userId, SystemList.Next.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDueDate_SetsDueDateOnTask()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(5).Date;
        var command = CreateValidCommand(userId);
        command.DueDate = dueDate;

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(userId, SystemList.Inbox.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(dueDate, response.DueDate);
    }

    [Fact]
    public async Task Handle_CallsRepositoryAddAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = CreateValidCommand(userId);

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(userId, SystemList.Inbox.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockTaskRepository.Verify(
            r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ResponseIncludesTimestamps()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = CreateValidCommand(userId);
        var beforeHandle = DateTime.UtcNow;

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(userId, SystemList.Inbox.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);
        var afterHandle = DateTime.UtcNow;

        // Assert
        Assert.NotNull(response);
        Assert.True(response.CreatedAt >= beforeHandle);
        Assert.True(response.CreatedAt <= afterHandle);
        Assert.True(response.UpdatedAt >= beforeHandle);
        Assert.True(response.UpdatedAt <= afterHandle);
    }

    [Fact]
    public async Task Handle_WithMultiplePriorities_CreatesTaskWithCorrectPriority()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var priorities = new[] { Priority.P1, Priority.P2, Priority.P3, Priority.P4 };

        _mockTaskRepository
            .Setup(r => r.GetMaxSortOrderAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockTaskRepository
            .Setup(r => r.AddAsync(It.IsAny<TodoTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        foreach (var priority in priorities)
        {
            var command = CreateValidCommand(userId);
            command.Priority = priority;

            var response = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(priority, response.Priority);
        }
    }
}
