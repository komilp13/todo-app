# Agent: Test Strategy

**Subagent Type**: `general-purpose`

**Purpose**: Design comprehensive test approach for complex features including unit tests, integration tests, edge cases, and error scenarios.

## When to Use

- Designing test approach for complex features
- Features with multiple code paths
- Features with error scenarios
- Authorization/authentication features
- Business logic requiring extensive testing

## Example Prompts

### Example 1: Test Strategy for CompleteTask

```
Design comprehensive test strategy for CompleteTask feature:

Requirements:
1. Marks task as Done and archives (IsArchived = true)
2. Sets CompletedAt to current UTC time
3. Cannot complete if already done (idempotent)
4. Only owner can complete their tasks (authorization)
5. Archived tasks appear in separate view (GET /api/tasks?archived=true)
6. Completion updates sidebar counts
7. Triggers success toast in UI

Test Levels:
- Unit tests (handler logic)
- Integration tests (HTTP endpoint)
- Frontend component tests (optimistic UI)

For each level, provide:
1. Test case descriptions
2. Arrange-Act-Assert structure
3. Mock/real database approach
4. Expected assertions
5. Error scenarios

Provide:
1. List of all unit test cases
2. List of all integration test cases
3. Mock/fixture strategy
4. Data setup approach
5. Error case coverage
6. Example test code (using xUnit)
7. Expected coverage %

Reference framework: xUnit with Moq for mocking
```

### Example 2: Test Strategy for CreateTask with Validation

```
Design test approach for CreateTask with comprehensive validation:

Requirements:
1. Name required, max 500 chars
2. Description optional, max 4000 chars
3. DueDate optional, must be valid date
4. Priority must be P1-P4 (enum)
5. SystemList must be one of 4 values
6. ProjectId optional, must exist and belong to user
7. Validator runs before handler
8. Returns 400 with validation details on error
9. Returns 201 with full task on success
10. Default values: status=Open, systemList=Inbox, priority=P4

Test Cases Needed:
- [ ] Valid command creates task
- [ ] Empty name returns validation error
- [ ] Name > 500 chars returns error
- [ ] Invalid priority returns error
- [ ] Non-existent projectId returns error
- [ ] ProjectId from different user returns error
- [ ] DueDate in past should be accepted (?)
- [ ] Concurrent creates don't conflict
- [ ] New task appears in GetTasks

Provide:
1. Complete test matrix
2. Validator test cases separately
3. Handler test cases
4. Integration test cases
5. Example test code
6. Test data factory for reuse

Focus areas:
- Validation testing (is every rule tested?)
- Authorization testing (user can't create in others' projects)
- Happy path (everything works)
- Error paths (all validation failures)
- Edge cases (boundary values)
```

### Example 3: Test Strategy for GetUpcomingTasks

```
Design tests for complex GetUpcomingTasks query:

Requirements:
1. Return tasks with due date within 14 days (any system list)
2. Plus tasks with SystemList = Upcoming (any date)
3. Sort: overdue first (oldest first), then by due date
4. Include related: labels, project name
5. Only non-archived, open tasks
6. Only user's own tasks
7. No pagination (return all matching)

Complexity:
- Multiple filter conditions (date range OR explicit list)
- Sorting logic (overdue first)
- Relationship loading (N+1 prevention)
- Authorization boundary

Test Categories:

Unit Tests (Handler):
- [ ] Fetches tasks within 14 days
- [ ] Includes explicit Upcoming list tasks
- [ ] Sorts overdue first
- [ ] Excludes archived tasks
- [ ] Excludes Done status tasks
- [ ] Only includes user's tasks
- [ ] Includes labels and project in response
- [ ] Handles user with no tasks
- [ ] Handles user with no Upcoming tasks

Integration Tests:
- [ ] GetUpcomingTasks returns 200 with tasks
- [ ] GetUpcomingTasks without auth returns 401
- [ ] GetUpcomingTasks includes proper sorting
- [ ] GetUpcomingTasks performance (N+1 check)
- [ ] GetUpcomingTasks with multiple users (isolation)

Edge Cases:
- [ ] Task due exactly 14 days away (should include)
- [ ] Task due exactly 15 days away (should exclude)
- [ ] Task due today (should include)
- [ ] Overdue task (should come first)
- [ ] Multiple overdue tasks (oldest first)
- [ ] Tasks in all system lists (but date-driven)
- [ ] Completed/archived tasks (should exclude)

Provide:
1. Complete test suite code
2. Test data setup strategy
3. Performance considerations
4. Mock vs real database approach
5. N+1 query prevention verification
6. Assertion examples
```

## What to Expect from Agent

1. **Comprehensive Test Cases**
   - Happy path
   - Validation errors
   - Authorization failures
   - Edge cases
   - Concurrency issues

2. **Test Structure Examples**
   ```csharp
   [Fact]
   public async Task Handle_ValidCommand_CreatesTask()
   {
       // Arrange
       var command = new CreateTaskCommand { Name = "Test" };
       var handler = new CreateTaskHandler(_mockRepo);

       // Act
       var result = await handler.Handle(command, CancellationToken.None);

       // Assert
       Assert.NotNull(result.Id);
       _mockRepo.Verify(r => r.AddAsync(It.IsAny<TodoTask>()), Times.Once);
   }
   ```

3. **Test Data Strategies**
   ```csharp
   public static class TestDataFactory
   {
       public static CreateTaskCommand ValidCommand() => new()
       {
           Name = "Test",
           SystemList = SystemList.Inbox
       };
   }
   ```

4. **Mock Setup Examples**
   ```csharp
   var mockRepo = new Mock<ITaskRepository>();
   mockRepo
       .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
       .ReturnsAsync(new TodoTask(...));
   ```

5. **Assertion Strategies**
   - Assert properties
   - Verify mock calls
   - Check state changes
   - Validate error messages

6. **Coverage Goals**
   - All code paths
   - Error scenarios
   - Edge cases
   - Authorization checks

## Integration Points

After test design:

1. **Create test files**:
   - `Tests/Unit/Features/{Feature}/{UseCase}HandlerTests.cs`
   - `Tests/Integration/Endpoints/{Feature}/{UseCase}Tests.cs`

2. **Implement tests** based on provided examples

3. **Run `/test-slice {UseCase}`** to validate

## Testing Patterns

### Unit Test Pattern
```csharp
public class CreateTaskHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_Returns201()
    {
        // Arrange
        var mock = new Mock<ITaskRepository>();
        var handler = new CreateTaskHandler(mock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mock.Verify(m => m.AddAsync(It.IsAny<TodoTask>()), Times.Once);
    }
}
```

### Integration Test Pattern
```csharp
public class CreateTaskTests
{
    [Fact]
    public async Task CreateTask_WithValidRequest_Returns201()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new CreateTaskCommand { Name = "Test" };

        // Act
        var response = await client.PostAsJsonAsync("/api/tasks", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadAsAsync<TaskResponse>();
        task.Name.Should().Be("Test");
    }
}
```

## Follow-Up Questions

- "Should we test database persistence or mock it?"
- "How deep into related entities should tests go?"
- "Do we need performance/load tests?"
- "Should we test concurrent operations?"
- "How to handle time-dependent tests?"

## Coverage Goals

Target **80%+** coverage for:
- Domain entity logic
- Handler business logic
- Validator rules
- Error scenarios

Lower coverage acceptable for:
- Auto-generated code
- Simple routing
- Infrastructure setup

## Tips for Using This Agent

1. **Be thorough** — List all scenarios, even edge cases
2. **Provide context** — Share domain complexity
3. **Ask for examples** — Want concrete test code
4. **Request patterns** — "How to test time-based logic?"
5. **Test data** — "What test fixtures do I need?"

## Test Pyramid Strategy

```
        Unit Tests (many, fast)
       Integration Tests (some, moderate)
      E2E Tests (few, slow)
```

For this project:
- **Unit**: 60% (handler logic, validators, domain rules)
- **Integration**: 30% (API endpoints, database)
- **E2E**: 10% (critical user flows only)
