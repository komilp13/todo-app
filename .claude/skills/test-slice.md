# Skill: test-slice

Runs unit and integration tests for a specific vertical slice.

## Invocation

```
/test-slice <UseCaseName> [--unit-only] [--integration-only] [--verbose] [--coverage]
```

## Examples

```
/test-slice CreateTask
/test-slice GetTasks --integration-only
/test-slice CompleteTask --unit-only --verbose
/test-slice UpdateProject --coverage
```

## Parameters

- **UseCaseName** (required): Feature name to test (CreateTask, GetTasks, etc.)
- **--unit-only** (optional): Run only unit tests
- **--integration-only** (optional): Run only integration tests
- **--verbose** (optional): Show detailed output including individual test names
- **--coverage** (optional): Include code coverage report

## What It Does

### Default Behavior (Unit + Integration)

```bash
# Unit tests
dotnet test /src/backend/TodoApp.UnitTests \
  --filter "FullyQualifiedName~CreateTask" \
  --logger "console;verbosity=minimal"

# Integration tests
dotnet test /src/backend/TodoApp.IntegrationTests \
  --filter "FullyQualifiedName~CreateTask" \
  --logger "console;verbosity=minimal"
```

### Unit Tests Only
```bash
dotnet test /src/backend/TodoApp.UnitTests \
  --filter "FullyQualifiedName~CreateTask"
```

### Integration Tests Only
```bash
dotnet test /src/backend/TodoApp.IntegrationTests \
  --filter "FullyQualifiedName~CreateTask"
```

### With Coverage
```bash
dotnet test /src/backend/TodoApp.UnitTests \
  --filter "FullyQualifiedName~CreateTask" \
  /p:CollectCoverageMetricUnderSourceParent=true \
  /p:CoverageThreshold=80
```

## Output Format

### Successful Run
```
Testing CreateTask...

✓ Unit Tests (3 passed)
  ✓ Handle_ValidCommand_CreatesTask (234ms)
  ✓ Handle_InvalidName_Returns400 (45ms)
  ✓ Handle_EmptyName_Validation (32ms)

✓ Integration Tests (2 passed)
  ✓ CreateTask_WithValidRequest_Returns201 (456ms)
  ✓ CreateTask_WithInvalidProject_Returns400 (123ms)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summary: 5 passed, 0 failed (791ms)
Coverage: 94% of handler code
```

### Failed Tests
```
Testing CreateTask...

✗ Unit Tests (2 passed, 1 failed)
  ✓ Handle_ValidCommand_CreatesTask (234ms)
  ✗ Handle_InvalidName_Returns400 (45ms)
    Error: Expected exception but none was thrown
  ✓ Handle_EmptyName_Validation (32ms)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summary: 4 passed, 1 failed (789ms)

Recommendation: Fix validation logic in CreateTaskCommandValidator
```

## Test Structure Expectations

### Unit Test File Location
```
Tests/Unit/Features/{Feature}/{UseCaseName}HandlerTests.cs
```

### Example Unit Test
```csharp
namespace TodoApp.Tests.Unit.Features.Tasks;

public class CreateTaskHandlerTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly CreateTaskHandler _handler;

    public CreateTaskHandlerTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _handler = new CreateTaskHandler(_mockTaskRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTask()
    {
        // Arrange
        var command = new CreateTaskCommand
        {
            Name = "Buy milk",
            SystemList = SystemList.Inbox,
            Priority = Priority.P4
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Id);
        _mockTaskRepository.Verify(r => r.AddAsync(It.IsAny<TodoTask>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidName_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateTaskCommand { Name = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
```

### Integration Test File Location
```
Tests/Integration/Endpoints/{Feature}/{UseCaseName}Tests.cs
```

### Example Integration Test
```csharp
namespace TodoApp.Tests.Integration.Endpoints.Tasks;

public class CreateTaskTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public CreateTaskTests()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        // Seed test data, set auth headers, etc.
    }

    [Fact]
    public async Task CreateTask_WithValidRequest_Returns201AndTask()
    {
        // Arrange
        var request = new CreateTaskCommand
        {
            Name = "Buy milk",
            SystemList = SystemList.Inbox
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadAsAsync<CreateTaskResponse>();
        task.Id.Should().NotBeEmpty();
        task.Name.Should().Be("Buy milk");
    }

    [Fact]
    public async Task CreateTask_WithoutAuthentication_Returns401()
    {
        // Arrange
        var request = new CreateTaskCommand { Name = "Test" };
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }
}
```

## Testing Patterns

### Testing Domain Logic
```csharp
[Fact]
public void TodoTask_Complete_CannotCompleteIfAlreadyDone()
{
    // Arrange
    var task = TodoTask.Create(Guid.NewGuid(), "Test", null);
    task.Complete();

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => task.Complete());
}
```

### Testing Validation
```csharp
[Fact]
public void CreateTaskCommandValidator_InvalidName_HasError()
{
    // Arrange
    var validator = new CreateTaskCommandValidator();
    var command = new CreateTaskCommand { Name = "" };

    // Act
    var result = validator.Validate(command);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "Name");
}
```

### Testing Authorization
```csharp
[Fact]
public async Task GetTask_WithDifferentUser_Returns404()
{
    // Arrange - Create task as alice
    var aliceClient = await CreateAuthenticatedClient("alice@example.com");
    var createResponse = await aliceClient.PostAsJsonAsync(
        "/api/tasks", new CreateTaskCommand { Name = "Alice's task" });
    var taskId = /* extract from response */;

    // Arrange - Authenticate as bob
    var bobClient = await CreateAuthenticatedClient("bob@example.com");

    // Act
    var response = await bobClient.GetAsync($"/api/tasks/{taskId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

## Debugging Failed Tests

### Run with Maximum Verbosity
```
/test-slice CreateTask --verbose
```

Shows:
- Individual test execution time
- Detailed assertion messages
- Stack traces
- Variable values at failure point

### Run Single Test Method
```bash
dotnet test /src/backend/TodoApp.UnitTests \
  --filter "FullyQualifiedName~CreateTaskHandlerTests.Handle_ValidCommand_CreatesTask"
```

### Run with Debug Output
```bash
dotnet test /src/backend/TodoApp.UnitTests \
  --filter "FullyQualifiedName~CreateTask" \
  -v diag
```

## Test Data Fixtures

### Reusable Factory
```csharp
public static class TestDataFactory
{
    public static CreateTaskCommand ValidCreateTaskCommand() => new()
    {
        Name = "Test Task",
        SystemList = SystemList.Inbox,
        Priority = Priority.P2
    };

    public static TodoTask ValidTodoTask() =>
        TodoTask.Create(Guid.NewGuid(), "Test", null);
}

// Usage in tests
[Fact]
public async Task Handle_ValidCommand_CreatesTask()
{
    var command = TestDataFactory.ValidCreateTaskCommand();
    // ... rest of test
}
```

## Coverage Goals

Target 80%+ coverage for:
- Domain entities and rules
- Handler business logic
- Validation rules
- Error scenarios

Lower coverage acceptable for:
- Auto-generated EF migrations
- Thin controller routing

## Continuous Testing During Development

```bash
# Watch mode - reruns tests on file changes
dotnet watch test /src/backend/TodoApp.UnitTests
```

## Common Test Issues

### Test Isolation
- Use fresh repository mocks for each test
- Database tests: rollback transactions between tests
- Clear mock invocations between tests

### Async Issues
```csharp
// ❌ Wrong
public void TestAsync() { }

// ✓ Correct
public async Task TestAsync() { }
```

### Collection Assertions
```csharp
// Using FluentAssertions
tasks.Should()
    .HaveCount(3)
    .And.AllSatisfy(t => t.UserId == userId)
    .And.ContainSingle(t => t.Id == expectedId);
```

## Next Steps

1. Create tests with `/scaffold-slice` (creates template)
2. Run `/test-slice FeatureName` frequently during development
3. Aim for 80%+ coverage
4. Use `--coverage` flag before commits
5. Fix failures immediately; don't commit failing tests

## Tips

- Run tests after every major change
- Use `--verbose` to understand failures
- Integration tests are slower; run unit tests first
- Mock external dependencies in unit tests
- Use real database for integration tests
- Test both happy path and error cases
