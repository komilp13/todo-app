# Skill: scaffold-slice

Generates all boilerplate files for a new vertical slice (command/query + handler + validator + response).

## Invocation

```
/scaffold-slice <UseCaseName> <command|query> [--feature <FeatureName>]
```

## Examples

```
/scaffold-slice CreateTask command
/scaffold-slice GetTasks query
/scaffold-slice CompleteTask command --feature Tasks
/scaffold-slice UpdateProject command --feature Projects
```

## Parameters

- **UseCaseName** (required): PascalCase name (CreateTask, GetTasks, UpdateProject)
- **Type** (required): `command` for write operations, `query` for read operations
- **--feature** (optional): Feature area (Auth, Tasks, Projects, Labels). Auto-detected if omitted

## What It Generates

### For Commands (Write Operations)

**Files Created**:
1. `Features/{Feature}/{UseCaseName}/{UseCaseName}Command.cs` — Input DTO
2. `Features/{Feature}/{UseCaseName}/{UseCaseName}Handler.cs` — Command handler
3. `Features/{Feature}/{UseCaseName}/{UseCaseName}CommandValidator.cs` — FluentValidation validator
4. `Features/{Feature}/{UseCaseName}/{UseCaseName}Response.cs` — Response DTO
5. `Tests/Unit/Features/{Feature}/{UseCaseName}HandlerTests.cs` — Unit test template
6. `API/Controllers/{Feature}Controller.cs` — Updated or created (if needed)

**DI Registration**: Automatically added to `Program.cs`

### For Queries (Read Operations)

**Files Created**:
1. `Features/{Feature}/{UseCaseName}/{UseCaseName}Query.cs` — Query DTO
2. `Features/{Feature}/{UseCaseName}/{UseCaseName}Handler.cs` — Query handler
3. `Features/{Feature}/{UseCaseName}/{UseCaseName}QueryValidator.cs` — Validator
4. `Features/{Feature}/{UseCaseName}/{UseCaseName}Response.cs` — Response DTO
5. `Tests/Unit/Features/{Feature}/{UseCaseName}HandlerTests.cs` — Unit test template
6. `API/Controllers/{Feature}Controller.cs` — Updated or created (if needed)

**DI Registration**: Automatically added to `Program.cs`

## Generated File Templates

### Command Template
```csharp
namespace TodoApp.Features.{Feature}.{UseCaseName};

public class {UseCaseName}Command
{
    public required string Name { get; set; }
    // Add other properties as needed
}
```

### Query Template
```csharp
namespace TodoApp.Features.{Feature}.{UseCaseName};

public class {UseCaseName}Query
{
    public Guid? ProjectId { get; set; }
    // Add filter properties as needed
}
```

### Handler Template
```csharp
namespace TodoApp.Features.{Feature}.{UseCaseName};

public class {UseCaseName}Handler : ICommandHandler<{UseCaseName}Command, {UseCaseName}Response>
{
    private readonly ITaskRepository _taskRepository;

    public {UseCaseName}Handler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<{UseCaseName}Response> Handle(
        {UseCaseName}Command command,
        CancellationToken cancellationToken)
    {
        // TODO: Implement business logic
        throw new NotImplementedException();
    }
}
```

### Validator Template
```csharp
namespace TodoApp.Features.{Feature}.{UseCaseName};

public class {UseCaseName}CommandValidator : AbstractValidator<{UseCaseName}Command>
{
    public {UseCaseName}CommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(500).WithMessage("Name cannot exceed 500 characters");
    }
}
```

### Response Template
```csharp
namespace TodoApp.Features.{Feature}.{UseCaseName};

public class {UseCaseName}Response
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // Add response properties
}
```

### Unit Test Template
```csharp
namespace TodoApp.Tests.Unit.Features.{Feature};

public class {UseCaseName}HandlerTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly {UseCaseName}Handler _handler;

    public {UseCaseName}HandlerTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _handler = new {UseCaseName}Handler(_mockTaskRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsResponse()
    {
        // Arrange
        var command = new {UseCaseName}Command { Name = "Test" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockTaskRepository.Verify(r => r.AddAsync(It.IsAny<TodoTask>()), Times.Once);
    }
}
```

## Implementation Checklist

After generating, complete:

- [ ] Implement handler logic (remove `throw new NotImplementedException()`)
- [ ] Define all necessary properties in Command/Query DTO
- [ ] Add validation rules to validator
- [ ] Populate response DTO with all needed fields
- [ ] Add integration test in `Tests/Integration/`
- [ ] Implement controller action (routes to handler)
- [ ] Test with `dotnet test`

## Next Steps

1. Open the generated handler file
2. Review the TODO comment
3. Implement business logic
4. Run `/test-slice {UseCaseName}` to test
5. Run `/check-architecture` to validate design
6. Implement controller endpoint
7. Run `/next-story` when complete to mark story done and get next recommendation

## Notes

- All files follow Clean Architecture + Vertical Slice patterns
- Handlers use dependency injection (no service locator)
- Validators use FluentValidation
- Response DTOs are separate from domain entities
- DI registration is automatically updated
- File structure must follow conventions for proper organization
