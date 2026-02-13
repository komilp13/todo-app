# Skill: check-architecture

Validates that code adheres to Clean Architecture + Vertical Slice Architecture principles.

## Invocation

```
/check-architecture [--strict] [--fix] [--report]
```

## Examples

```
/check-architecture
/check-architecture --strict
/check-architecture --fix
/check-architecture --report architecture-violations.json
```

## Parameters

- **--strict** (optional): Treat warnings as errors (fail on any violation)
- **--fix** (optional): Auto-fix common issues (files will be modified)
- **--report** (optional): Output detailed JSON report to file

## What It Checks

### Domain Layer Purity
✓ Domain entities have NO EF Core attributes
```csharp
// ✗ Wrong
public class Task
{
    [Table("Tasks")]
    public Guid Id { get; set; }
}

// ✓ Correct
public class Task
{
    public Guid Id { get; private set; }
}
```

✓ Domain entities use only private setters
```csharp
// ✗ Wrong
public string Name { get; set; }

// ✓ Correct
public string Name { get; private set; }
```

✓ Domain has no infrastructure dependencies
```csharp
// ✗ Wrong
using TodoApp.Infrastructure;
public class Task : IEntity { }

// ✓ Correct
public class Task { } // No external dependencies
```

### Vertical Slice Organization
✓ All command/query handlers are in `/Features/{Feature}/{UseCaseName}/`
✓ No cross-slice dependencies
```csharp
// ✗ Wrong
using TodoApp.Features.Tasks.CreateTask;
namespace TodoApp.Features.Projects;

public class CreateProjectHandler
{
    // Depends on Tasks.CreateTask - violation!
}

// ✓ Correct
using TodoApp.Core.Interfaces;
public class CreateProjectHandler
{
    // Depends on interface, not concrete slice
}
```

✓ Handlers don't directly use concrete repositories from other slices

### File Naming Conventions
✓ Commands named: `{UseCaseName}Command.cs`
✓ Queries named: `{UseCaseName}Query.cs`
✓ Handlers named: `{UseCaseName}Handler.cs`
✓ Validators named: `{UseCaseName}CommandValidator.cs` or `{UseCaseName}QueryValidator.cs`
✓ Responses named: `{UseCaseName}Response.cs`

### Controller Thinness
✓ Controllers contain no business logic
```csharp
// ✗ Wrong - Business logic in controller
[HttpPost]
public async Task<IActionResult> CreateTask(CreateTaskCommand command)
{
    if (string.IsNullOrEmpty(command.Name))
        return BadRequest("Name required");

    var task = new TodoTask { Name = command.Name };
    // ... more logic
    return Ok(task);
}

// ✓ Correct - Delegates to handler
[HttpPost]
public async Task<IActionResult> CreateTask(CreateTaskCommand command)
{
    var result = await _handler.Handle(command, CancellationToken.None);
    return Created($"/api/tasks/{result.Id}", result);
}
```

✓ Controllers don't contain data access logic
✓ Controllers don't duplicate validation

### Dependency Inversion
✓ Handlers depend on interfaces, not concrete implementations
```csharp
// ✗ Wrong
public class CreateTaskHandler
{
    private readonly TaskRepository _repo; // Concrete dependency
}

// ✓ Correct
public class CreateTaskHandler
{
    private readonly ITaskRepository _repo; // Interface dependency
}
```

✓ No service locator pattern
```csharp
// ✗ Wrong
public class Handler
{
    public async Task Handle(Command cmd)
    {
        var repo = ServiceLocator.GetRepository<ITaskRepository>();
    }
}

// ✓ Correct
public class Handler
{
    public Handler(ITaskRepository repo) { }
}
```

### Infrastructure Isolation
✓ EF Core DbContext only in Infrastructure layer
✓ Repository implementations only in Infrastructure
✓ No EF Core imports in Domain or Application layers
```csharp
// ✗ Wrong
namespace TodoApp.Features.Tasks;
using Microsoft.EntityFrameworkCore;

// ✓ Correct
namespace TodoApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
```

### CQRS Separation
✓ Commands not used for reads
✓ Queries not used for writes
```csharp
// ✗ Wrong - Query modifying state
public class GetTasksQuery
{
    public async Task<List<Task>> Handle()
    {
        var tasks = await _repo.GetAll();
        tasks.ForEach(t => t.Views++); // Mutation!
        return tasks;
    }
}

// ✓ Correct
public class GetTasksQuery
{
    public async Task<List<TaskResponse>> Handle()
    {
        return await _repo.GetAll().ToListAsync();
    }
}
```

## Output Examples

### Successful Check (No Violations)
```
Architecture Check Report
========================
Checking 45 files...

✓ Domain Layer Purity
  - 8 domain entities: all valid
  - 0 EF Core attributes detected
  - 0 external dependencies

✓ Vertical Slices
  - 35 handlers properly organized
  - 0 cross-slice dependencies detected
  - 0 forbidden imports detected

✓ File Naming
  - 35/35 command/query files properly named
  - 35/35 handlers properly named
  - 35/35 validators properly named
  - 35/35 responses properly named

✓ Controllers
  - 12 controllers: all thin
  - 0 business logic detected
  - 0 data access logic detected

✓ Dependency Inversion
  - 45 classes checked
  - 45 using interfaces (0 concrete dependencies)
  - 0 service locator usage

✓ Infrastructure Isolation
  - DbContext confined to Infrastructure
  - 0 EF imports in Domain
  - 0 EF imports in Application

✓ CQRS Separation
  - 20 commands (write-only)
  - 15 queries (read-only)
  - 0 mutations in queries

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summary: ✓ PASS (0 issues)
Architecture is clean and compliant
Recommendation: Ready to commit
```

### With Warnings
```
Architecture Check Report
========================
Checking 45 files...

✓ Domain Layer Purity (8/8 valid)
✓ Vertical Slices (0 cross-slice violations)
✗ File Naming (1 issue)
✗ Controllers (2 issues)
✓ Dependency Inversion (45/45 valid)
✓ Infrastructure Isolation
✓ CQRS Separation

Issues Found: 3

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[WARNING] Features/Tasks/CreateTask/CreateTaskResponse.cs:1
  File naming: Should be named 'CreateTaskResponse.cs' (currently correct)
  ℹ Actually valid, skipping

[WARNING] API/Controllers/TasksController.cs:45
  Business logic detected in controller
    Line 45: if (string.IsNullOrEmpty(command.Name)) return BadRequest(...);
  Fix: Move validation to handler/validator
  Suggestion: Remove manual validation, rely on validator middleware

[WARNING] API/Controllers/TasksController.cs:52
  Data access in controller
    Line 52: var task = await _dbContext.Tasks.FirstOrDefault(...);
  Fix: Use repository interface instead of DbContext
  Suggestion: Inject ITaskRepository and use repo methods

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summary: ✓ PASS with 2 warnings
Recommendation: Fix warnings before merging
Fixed by --fix flag: Yes, 2/2 issues auto-fixable
```

### Strict Mode (Errors)
```
Architecture Check Report (STRICT MODE)
=======================================

✗ CRITICAL: Domain entity has EF Core attributes
  File: Core/Domain/Entities/Task.cs:3
  Issue: [Table("Tasks")] attribute found
  Fix: Remove all EF Core attributes from domain entities

✗ CRITICAL: Cross-slice dependency detected
  File: Features/Projects/CreateProject/CreateProjectHandler.cs:8
  Issue: Using TodoApp.Features.Tasks.CreateTask namespace
  Fix: Depend on interfaces, not concrete slices

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Summary: ✗ FAIL (2 critical errors)
Strict mode: Treating all warnings as errors
Recommendation: Fix before any commits
```

## Auto-Fix Capability

With `--fix` flag, automatically resolves:

1. **Remove EF Core attributes from domain**
   ```csharp
   // Before
   [Table("Tasks")]
   public class Task { }

   // After
   public class Task { }
   ```

2. **Make properties private setters**
   ```csharp
   // Before
   public string Name { get; set; }

   // After
   public string Name { get; private set; }
   ```

3. **Update DI registrations** if inconsistent

4. **Fix file naming** (rename files if needed)

5. **Remove forbidden imports**
   ```csharp
   // Before
   using Microsoft.EntityFrameworkCore;
   using TodoApp.Infrastructure;

   // After
   // (only clean imports remain)
   ```

## Reporting

Output detailed JSON report with `--report`:

```json
{
  "timestamp": "2026-02-13T10:30:00Z",
  "status": "PASS_WITH_WARNINGS",
  "statistics": {
    "total_files_checked": 45,
    "domain_entities": 8,
    "handlers": 35,
    "controllers": 12,
    "violations": 2,
    "warnings": 2,
    "critical_errors": 0
  },
  "violations": [
    {
      "severity": "WARNING",
      "file": "API/Controllers/TasksController.cs",
      "line": 45,
      "issue": "Business logic in controller",
      "suggestion": "Move to handler/validator",
      "auto_fixable": false
    }
  ]
}
```

## Integration with CI/CD

### Pre-Commit Hook
```bash
#!/bin/bash
/check-architecture --strict || exit 1
```

### GitHub Actions
```yaml
- name: Check Architecture
  run: /check-architecture --strict --report violations.json

- name: Upload Report
  if: always()
  uses: actions/upload-artifact@v2
  with:
    name: architecture-report
    path: violations.json
```

## When to Run

✓ **Before committing** — Ensure no violations slip in
✓ **Before creating PR** — Maintain code quality
✓ **During code review** — Automated compliance check
✓ **Regularly** — Catch drift from architecture

## Common Fixes

### Fix 1: Remove EF Core from Domain Entity
```csharp
// ✗ Wrong
using Microsoft.EntityFrameworkCore;
[Table("Tasks")]
public class Task
{
    [Key]
    public Guid Id { get; set; }
}

// ✓ Correct
public class Task
{
    public Guid Id { get; private set; }
}
```

### Fix 2: Move Business Logic from Controller to Handler
Move from controller to handler.

### Fix 3: Use Repository Interface Instead of DbContext
```csharp
// ✗ Wrong
public TasksController(ApplicationDbContext db)
{
    var task = await db.Tasks.FindAsync(id);
}

// ✓ Correct
public TasksController(ITaskRepository repo)
{
    var task = await repo.GetByIdAsync(id);
}
```

## Next Steps

1. Run architecture check: `/check-architecture`
2. Review any warnings
3. Fix violations manually or use `--fix` flag
4. Re-run to verify
5. Commit with confidence
6. Run `/next-story` to mark current story complete and get next recommendation

## Notes

- Run before every commit
- Use `--strict` in CI/CD pipelines
- Architecture violations compound over time; fix immediately
- Clean architecture is an investment in long-term maintainability
