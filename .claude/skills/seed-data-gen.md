# Skill: seed-data-gen

Generates realistic development seed data (test users, tasks, projects, labels).

## Invocation

```
/seed-data-gen [--minimal] [--heavy] [--output <path>]
```

## Examples

```
/seed-data-gen
/seed-data-gen --minimal
/seed-data-gen --heavy
/seed-data-gen --output Infrastructure/Data/Seeders/CustomSeeder.cs
```

## Parameters

- **--minimal** (optional): Generate only essential data (2 users, 10 tasks)
- **--heavy** (optional): Generate large dataset for stress testing (10 users, 500 tasks)
- **--output** (optional): Custom output file path

## What It Generates

### Default Dataset

**Users** (3 test accounts):
```
1. alice@example.com
   - Password: Alice@12345
   - Display Name: Alice Smith

2. bob@example.com
   - Password: Bob@12345
   - Display Name: Bob Johnson

3. carol@example.com
   - Password: Carol@12345
   - Display Name: Carol Davis
```

**Tasks** (30 total):
- 10 in Inbox
- 8 in Next
- 7 in Upcoming
- 5 in Someday
- 8 completed/archived

**Priorities**:
- 5 P1 (High)
- 8 P2 (Medium)
- 12 P3 (Normal)
- 5 P4 (Low)

**Projects** (4):
1. "Product Roadmap" (8 tasks)
2. "Q1 Planning" (6 tasks)
3. "Bug Fixes" (5 tasks)
4. "Personal Goals" (4 tasks)

**Labels** (8):
- Work (red)
- Personal (blue)
- Urgent (orange)
- Review (purple)
- Bug (red)
- Feature (green)
- Testing (cyan)
- Documentation (gray)

### Generated Seeder Class

Creates `Infrastructure/Data/Seeders/DevelopmentSeeder.cs`:

```csharp
namespace TodoApp.Infrastructure.Data.Seeders;

public class DevelopmentSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;

    public DevelopmentSeeder(
        ApplicationDbContext context,
        IPasswordHashingService passwordHashingService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
    }

    public async Task SeedAsync()
    {
        // Check if data already exists (idempotent)
        if (await _context.Users.AnyAsync())
            return;

        var users = CreateUsers();
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var projects = CreateProjects(users);
        await _context.Projects.AddRangeAsync(projects);
        await _context.SaveChangesAsync();

        var labels = CreateLabels(users);
        await _context.Labels.AddRangeAsync(labels);
        await _context.SaveChangesAsync();

        var tasks = CreateTasks(users, projects, labels);
        await _context.Tasks.AddRangeAsync(tasks);
        await _context.SaveChangesAsync();
    }

    private List<User> CreateUsers()
    {
        return new()
        {
            new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = "alice@example.com",
                DisplayName = "Alice Smith",
                PasswordHash = HashPassword("Alice@12345", out var salt1),
                PasswordSalt = salt1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Email = "bob@example.com",
                DisplayName = "Bob Johnson",
                PasswordHash = HashPassword("Bob@12345", out var salt2),
                PasswordSalt = salt2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Email = "carol@example.com",
                DisplayName = "Carol Davis",
                PasswordHash = HashPassword("Carol@12345", out var salt3),
                PasswordSalt = salt3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    private List<Project> CreateProjects(List<User> users)
    {
        var userId = users[0].Id;
        return new()
        {
            new Project
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Product Roadmap",
                Description = "Q1-Q3 product development",
                Status = ProjectStatus.Active,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // ... more projects
        };
    }

    private List<Label> CreateLabels(List<User> users)
    {
        var userId = users[0].Id;
        return new()
        {
            new Label
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Work",
                Color = "#FF4444",
                CreatedAt = DateTime.UtcNow
            },
            // ... more labels
        };
    }

    private List<TodoTask> CreateTasks(List<User> users, List<Project> projects, List<Label> labels)
    {
        var tasks = new List<TodoTask>();
        var userId = users[0].Id;

        // Inbox tasks
        tasks.Add(TodoTask.Create(userId, "Review PR #123", "Check code quality", SystemList.Inbox, Priority.P1));
        // ... more tasks

        return tasks;
    }

    private string HashPassword(string password, out string salt)
    {
        return _passwordHashingService.HashPassword(password);
        // Implementation depends on your PasswordHashingService
    }
}
```

### Integration in Startup

Updates `Program.cs`:

```csharp
// In configuration
builder.Services.AddScoped<DevelopmentSeeder>();

// In app initialization
var seeder = app.Services.CreateScope().ServiceProvider.GetRequiredService<DevelopmentSeeder>();
if (app.Environment.IsDevelopment())
{
    await seeder.SeedAsync();
}
```

## Data Variations

### Minimal Dataset (--minimal)

```
Users: 2 (alice, bob)
Tasks: 10 total
  - 4 Inbox, 3 Next, 2 Upcoming, 1 Someday
  - 0 completed
Projects: 1 ("My Project")
Labels: 3 (Work, Personal, Urgent)
Relationships: Basic coverage
```

### Heavy Dataset (--heavy)

```
Users: 10 (alice, bob, carol, david, eve, frank, grace, henry, iris, jack)
Tasks: 500 total
  - 150 Inbox, 120 Next, 100 Upcoming, 60 Someday
  - 70 completed/archived
Projects: 8 with various completion states
Labels: 15 detailed labels
  - Each label assigned to 15-30 tasks
  - Cross-user data
  - Multiple projects per user
Relationships: Complex, realistic data
```

## Test User Credentials

After seeding, login with:

```
Email: alice@example.com
Password: Alice@12345

Email: bob@example.com
Password: Bob@12345

Email: carol@example.com
Password: Carol@12345
```

Document these in README or `.env.example`:

```
# Development Test Users
#
# User 1:
# Email: alice@example.com
# Password: Alice@12345
#
# User 2:
# Email: bob@example.com
# Password: Bob@12345
```

## Sample Task Data Structure

Example of realistic task distribution:

**Inbox Tasks** (Capture bucket):
- "Review team feedback on dashboard"
- "Update API documentation"
- "Fix bug in task completion"
- "Respond to client emails"

**Next Tasks** (Actionable soon):
- "Implement user settings page"
- "Add dark mode toggle"
- "Write integration tests"
- "Deploy to staging"

**Upcoming Tasks** (Scheduled/Date-driven):
- "Quarterly planning meeting" (due: 2026-03-01)
- "Product launch" (due: 2026-02-28)
- "Security audit" (due: 2026-03-15)

**Someday Tasks** (Deferred):
- "Learn Rust"
- "Rebuild homepage"
- "Implement machine learning features"

**Completed Tasks** (Archived):
- "Design new logo" (completed: 2026-02-10)
- "Setup CI/CD pipeline" (completed: 2026-02-08)

## Project Organization

Example projects with tasks:

**Project: "Product Roadmap"**
- Status: Active
- Tasks: 8
  - 2 in Inbox (planning)
  - 3 in Next (in progress)
  - 2 in Upcoming (scheduled)
  - 1 Completed (done)

**Project: "Q1 Planning"**
- Status: Active
- Due Date: 2026-03-31
- Tasks: 6
- Progress: 33% complete

**Project: "Bug Fixes"**
- Status: Active
- Tasks: 5 (all high priority)
- Completion: 1/5

## Running Seeder

### Option 1: Automatic on Startup
Seeder runs automatically in Development:
```bash
dotnet run
# Seed data applied automatically
```

### Option 2: Manual Execution
```csharp
using var scope = app.Services.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>();
await seeder.SeedAsync();
```

### Option 3: CLI Command
```bash
dotnet ef database update
# Runs migrations and seeds
```

## Idempotent Seeding

Seeder is safe to run multiple times:

```csharp
public async Task SeedAsync()
{
    // Only seeds if no users exist
    if (await _context.Users.AnyAsync())
        return;

    // ... seed data
}
```

## Customization

After generation, modify `DevelopmentSeeder.cs` for custom needs:

```csharp
private List<TodoTask> CreateTasks(...)
{
    var tasks = new List<TodoTask>();

    // Add custom tasks for your workflow
    tasks.Add(TodoTask.Create(
        userId,
        "Custom Development Task",
        "Description for manual testing",
        SystemList.Inbox,
        Priority.P2
    ));

    // With due date
    var task = TodoTask.Create(...);
    task.SetDueDate(DateTime.UtcNow.AddDays(5));

    return tasks;
}
```

## Database Reset

To clear all data and reseed:

```bash
# Remove all migrations (caution!)
dotnet ef database drop

# Reapply migrations
dotnet ef database update

# Seeder runs automatically in Development
```

## Exporting Seed Data

To export current database as seed code:

```bash
# Dump database to SQL
pg_dump todoapp_dev > seed.sql

# Or programmatically read and generate C# code
var users = await _context.Users.ToListAsync();
// Generate C# factory code from data
```

## Loading into Different Environments

### Development
```bash
dotnet run
# Automatic seeding
```

### Testing
```csharp
// In test fixture
var seeder = new DevelopmentSeeder(_context, _passwordService);
await seeder.SeedAsync();
```

### Staging
```bash
# Manually controlled
dotnet ef database update --no-build
# Then run seeder via API endpoint or CLI
```

## Resetting for Testing

If tests modify data:

```csharp
[SetUp]
public async Task Setup()
{
    // Clear database
    await _context.Database.EnsureDeletedAsync();
    await _context.Database.EnsureCreatedAsync();

    // Reseed for test
    var seeder = new DevelopmentSeeder(_context, _passwordService);
    await seeder.SeedAsync();
}
```

## Next Steps

1. Run `/seed-data-gen`
2. Review generated `DevelopmentSeeder.cs`
3. Apply migrations: `dotnet ef database update`
4. Start app: `dotnet run`
5. Login with test credentials
6. Verify realistic data is present
7. Customize if needed for your testing scenarios

## Notes

- Seed data is **idempotent** (safe to run multiple times)
- Test user passwords should **never** be used in production
- Seed data is **development only** (not deployed)
- Customize for your team's typical workflow
- Large datasets (--heavy) useful for performance testing
