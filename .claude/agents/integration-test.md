# Agent: Integration Test

**Subagent Type**: `general-purpose`

**Purpose**: Design and implement integration tests for complete feature flows using WebApplicationFactory and real database persistence.

## When to Use

- Testing end-to-end feature workflows
- Complex multi-step operations
- Authorization and access control
- Database persistence and relationships
- Error scenarios with real infrastructure

## Example Prompts

### Example 1: Complete Task Flow Integration Tests

```
Design and implement integration tests for the complete task lifecycle:

Flow to test:
1. User registers (POST /api/auth/register)
2. User logs in (POST /api/auth/login)
3. User creates task (POST /api/tasks)
4. Verify task appears in GetTasks (GET /api/tasks)
5. User moves task to Next (PUT /api/tasks/{id} with systemList=Next)
6. Verify task appears in Next list
7. User adds label (POST /api/tasks/{id}/labels/{labelId})
8. Verify label appears in task
9. User completes task (PATCH /api/tasks/{id}/complete)
10. Verify task archived (appears in GET /api/tasks?archived=true)
11. User reopens task (PATCH /api/tasks/{id}/reopen)
12. Verify task back in Next
13. User deletes task (DELETE /api/tasks/{id})
14. Verify task gone (404 on GET)

Test structure:
- Single test class for entire flow
- Setup: create user, authenticate
- Teardown: clean up test data
- Assertions at each step
- Verify database state

Provide:
1. Complete test class using WebApplicationFactory
2. Test fixture/factory for reusable setup
3. Authentication helper (create JWT for test user)
4. Database cleanup strategy
5. Assertion helpers
6. Example code for all 14 steps

Framework: xUnit with Fluent Assertions
Database: Real PostgreSQL test instance
```

### Example 2: Authorization Tests

```
Implement authorization tests:

Scenarios:
1. User A creates task
2. User B tries to get User A's task (should 404)
3. User B tries to update User A's task (should 404)
4. User B tries to delete User A's task (should 404)
5. User A creates project
6. User B tries to get User A's project (should 404)
7. Deleted user cannot access resources

Test approach:
- Create two test users (alice, bob)
- Generate JWT for each
- Create HTTP clients for each user
- Verify cross-user access denied

Provide:
1. Test class with authorization tests
2. CreateAuthenticatedClient(email) helper
3. Assertion helpers for 404 vs other errors
4. Test data factory for users, tasks, projects

Questions:
- Should unauthorized access return 403 or 404?
- Should different error for "not found" vs "forbidden"?
- How to test deleted users?
```

### Example 3: Complex Multi-Step Feature Tests

```
Test the complex GetUpcomingTasks feature:

Requirements:
1. User creates tasks with various due dates
2. Call GET /api/tasks?view=upcoming
3. Verify response includes:
   - Tasks with due dates within 14 days
   - Tasks with SystemList = Upcoming (any date)
   - Only non-archived, open tasks
   - Sorted: overdue first, then by due date
4. Verify relationships included (labels, project)

Test data setup:
- User A has 30 tasks across all system lists
- Various priorities, due dates
- Some assigned to projects
- Some with labels
- Some completed
- Some explicitly in Upcoming

Test cases:
1. Upcoming tasks include date-driven tasks
2. Upcoming tasks include explicit Upcoming list
3. Overdue tasks appear first
4. Completed tasks excluded
5. Response includes labels and project
6. Sorting is correct
7. Performance acceptable (N+1 check)

Provide:
1. Test fixture creating diverse test data
2. Test cases for each requirement
3. Assertion helpers for sorting
4. Performance verification
5. Example test code

Consider:
- Timezone handling (UTC?)
- Date boundaries (today at 23:59?)
- Overdue definitions (before now?)
```

## What to Expect from Agent

1. **Complete Test Class**
   ```csharp
   public class CompleteTaskFlowTests : IAsyncLifetime
   {
       private readonly WebApplicationFactory<Program> _factory;
       private HttpClient _client;
       private string _authToken;
       private Guid _userId;

       public async Task InitializeAsync()
       {
           _factory = new WebApplicationFactory<Program>();
           _client = _factory.CreateClient();
           // Setup...
       }

       [Fact]
       public async Task CompleteFlow_CreateTaskThroughCompletion_Succeeds()
       {
           // Arrange
           var createCommand = new CreateTaskCommand { Name = "Test" };

           // Act
           var response = await _client.PostAsJsonAsync("/api/tasks", createCommand);

           // Assert
           response.StatusCode.Should().Be(HttpStatusCode.Created);
       }

       public async Task DisposeAsync()
       {
           _client?.Dispose();
           await _factory.DisposeAsync();
       }
   }
   ```

2. **Test Fixture Setup**
   ```csharp
   public class IntegrationTestBase : IAsyncLifetime
   {
       protected WebApplicationFactory<Program> Factory { get; set; }
       protected HttpClient Client { get; set; }
       protected string AuthToken { get; set; }

       public virtual async Task InitializeAsync()
       {
           Factory = new WebApplicationFactory<Program>();
           Client = Factory.CreateClient();
           // Register test user, get token
       }

       public virtual async Task DisposeAsync()
       {
           // Cleanup database
       }
   }
   ```

3. **Authentication Helper**
   ```csharp
   protected async Task<string> Authenticate(string email, string password)
   {
       var response = await Client.PostAsJsonAsync("/api/auth/login",
           new { email, password });
       var result = await response.Content.ReadAsAsync<LoginResponse>();
       return result.Token;
   }

   protected HttpClient CreateClientWithAuth(string token)
   {
       var client = Factory.CreateClient();
       client.DefaultRequestHeaders.Authorization =
           new AuthenticationHeaderValue("Bearer", token);
       return client;
   }
   ```

4. **Database Cleanup**
   ```csharp
   public async Task DisposeAsync()
   {
       using (var scope = Factory.Services.CreateScope())
       {
           var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
           await db.Database.EnsureDeletedAsync();
       }
   }
   ```

5. **Assertion Helpers**
   ```csharp
   protected async Task<T> ReadJsonResponseAsync<T>(HttpResponseMessage response)
   {
       return await response.Content.ReadAsAsync<T>();
   }

   protected void AssertUnauthorized(HttpResponseMessage response)
   {
       response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
   }

   protected void AssertNotFound(HttpResponseMessage response)
   {
       response.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }
   ```

6. **Complete Test Example**
   ```csharp
   [Fact]
   public async Task CreateTask_WithValidCommand_ReturnsCreated()
   {
       // Arrange
       var command = new CreateTaskCommand
       {
           Name = "Buy milk",
           SystemList = SystemList.Inbox,
           Priority = Priority.P4
       };

       // Act
       var response = await Client.PostAsJsonAsync("/api/tasks", command);

       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.Created);
       var task = await ReadJsonResponseAsync<TaskResponse>(response);
       task.Id.Should().NotBeEmpty();
       task.Name.Should().Be("Buy milk");
       task.SystemList.Should().Be(SystemList.Inbox);

       // Verify persisted
       var getResponse = await Client.GetAsync($"/api/tasks/{task.Id}");
       getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
   }
   ```

## Integration Test Patterns

### Pattern 1: Register, Login, Make Request
```csharp
[Fact]
public async Task ProtectedEndpoint_RequiresAuthentication()
{
    // Arrange
    var registerRequest = new RegisterCommand
    {
        Email = "test@example.com",
        Password = "Test@12345",
        DisplayName = "Test User"
    };

    // Act - Register
    var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
    var registerResult = await registerResponse.Content.ReadAsAsync<RegisterResponse>();
    var token = registerResult.Token;

    // Act - Authenticate
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act - Call protected endpoint
    var response = await _client.GetAsync("/api/tasks");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Pattern 2: Test Authorization
```csharp
[Fact]
public async Task GetTask_AsDifferentUser_ReturnsNotFound()
{
    // Arrange
    var aliceClient = await CreateAuthenticatedClient("alice@example.com", "Pass@123");
    var bobClient = await CreateAuthenticatedClient("bob@example.com", "Pass@456");

    // Act - Alice creates task
    var createResponse = await aliceClient.PostAsJsonAsync("/api/tasks",
        new CreateTaskCommand { Name = "Alice's task" });
    var task = await createResponse.Content.ReadAsAsync<TaskResponse>();

    // Act - Bob tries to get it
    var getResponse = await bobClient.GetAsync($"/api/tasks/{task.Id}");

    // Assert
    getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

### Pattern 3: Database Verification
```csharp
[Fact]
public async Task CreateTask_PersistsToDatabase()
{
    // Arrange
    var command = new CreateTaskCommand { Name = "Test" };

    // Act
    var response = await _client.PostAsJsonAsync("/api/tasks", command);

    // Assert - API response
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    // Assert - Database verification
    using (var scope = _factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Name == "Test");
        task.Should().NotBeNull();
        task.UserId.Should().Be(_userId);
    }
}
```

### Pattern 4: Error Scenarios
```csharp
[Fact]
public async Task CreateTask_WithoutName_Returns400()
{
    // Arrange
    var command = new CreateTaskCommand { Name = "" }; // Empty name

    // Act
    var response = await _client.PostAsJsonAsync("/api/tasks", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var error = await response.Content.ReadAsAsync<ErrorResponse>();
    error.Errors.Should().Contain(e => e.Field == "Name");
}
```

## Test Data Builders

Create reusable test data:

```csharp
public class TestDataBuilder
{
    public static CreateTaskCommand ValidTaskCommand() => new()
    {
        Name = "Test Task",
        SystemList = SystemList.Inbox,
        Priority = Priority.P4
    };

    public static RegisterCommand ValidRegisterCommand() => new()
    {
        Email = "test@example.com",
        Password = "Test@12345",
        DisplayName = "Test User"
    };
}

// Usage
var command = TestDataBuilder.ValidTaskCommand();
var response = await _client.PostAsJsonAsync("/api/tasks", command);
```

## WebApplicationFactory Setup

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override services for testing
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Use in-memory or test database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql("Server=localhost;Database=todoapp_test;...");
            });
        });
    }
}
```

## Database Cleanup Strategies

### Strategy 1: Truncate All Tables
```csharp
public async Task DisposeAsync()
{
    using (var scope = _factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Tasks\" CASCADE");
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\" CASCADE");
    }
}
```

### Strategy 2: Drop and Recreate
```csharp
public async Task DisposeAsync()
{
    using (var scope = _factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
```

### Strategy 3: Transaction Rollback
```csharp
public async Task InitializeAsync()
{
    // Start transaction
    await _transaction.BeginAsync();
}

public async Task DisposeAsync()
{
    // Rollback all changes
    await _transaction.RollbackAsync();
}
```

## Performance Testing in Integration Tests

```csharp
[Fact]
public async Task GetUpcomingTasks_PerformanceAcceptable()
{
    // Create 1000 tasks
    for (int i = 0; i < 1000; i++)
    {
        await _client.PostAsJsonAsync("/api/tasks", new CreateTaskCommand { ... });
    }

    // Measure query time
    var sw = Stopwatch.StartNew();
    var response = await _client.GetAsync("/api/tasks?view=upcoming");
    sw.Stop();

    // Assert performance
    sw.ElapsedMilliseconds.Should().BeLessThan(1000, "Query took too long");
}
```

## Follow-Up Questions

- "Should we test with real or in-memory database?"
- "How to handle concurrent test execution?"
- "Should we test WebSocket real-time updates?"
- "How to handle payment/external service integration?"

## Tips for Using This Agent

1. **Describe the flow** — Step-by-step what users do
2. **Specify assertions** — What should be verified
3. **Ask for fixtures** — Reusable test setup
4. **Request helpers** — Common assertion/setup methods
5. **Get examples** — Want concrete test code

## Best Practices

- One test per scenario
- Clear Arrange-Act-Assert
- Meaningful test names
- Reusable fixtures
- Fast execution
- No test interdependencies
- Cleanup after tests
- Use real database (not in-memory)
- Test both happy and sad paths
