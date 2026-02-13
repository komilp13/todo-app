# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**GTD Todo Application** — A web-based Getting Things Done (GTD) todo app similar to Todoist, with multi-user support, system lists (Inbox, Next, Upcoming, Someday), projects, labels, and task management.

- **Status**: Early stage — planning/scaffolding phase (Epic 1)
- **Tech Stack**:
  - Backend: C# 9 / ASP.NET Core Web API + Entity Framework Core
  - Database: PostgreSQL
  - Frontend: React/Next.js (TypeScript, Tailwind CSS)
  - Testing: xUnit (backend), Jest + React Testing Library (frontend)
  - CI/CD: GitHub Actions, Docker Compose for local dev
- **Detailed Spec**: See [docs/backlog.md](docs/backlog.md) for complete epics, features, and user stories with acceptance criteria

## Repository Structure (Target Layout)

```
/src/backend/           # C# backend — Clean Architecture + Vertical Slice Architecture
  /Core                 # Shared across all slices
    /Domain             # Core business rules, entities, enums, value objects
    /Interfaces         # Contracts (repositories, services, etc.)
  /Features             # Feature vertical slices
    /Auth
      /Register         # Registration use case
        RegisterHandler.cs
        RegisterCommand.cs
        RegisterCommandValidator.cs
        RegisterResponse.cs
      /Login            # Login use case
        LoginHandler.cs
        LoginQuery.cs
        LoginQueryValidator.cs
        LoginResponse.cs
      /GetCurrentUser   # Get current user use case
        GetCurrentUserHandler.cs
        GetCurrentUserQuery.cs
    /Tasks
      /CreateTask
        CreateTaskHandler.cs
        CreateTaskCommand.cs
        CreateTaskCommandValidator.cs
      /GetTasks
        GetTasksHandler.cs
        GetTasksQuery.cs
      /UpdateTask
        UpdateTaskHandler.cs
        UpdateTaskCommand.cs
      /CompleteTask
        CompleteTaskHandler.cs
        CompleteTaskCommand.cs
      /DeleteTask
        DeleteTaskHandler.cs
        DeleteTaskCommand.cs
      /ReorderTasks
        ReorderTasksHandler.cs
        ReorderTasksCommand.cs
    /Projects           # Similar structure for projects
    /Labels             # Similar structure for labels
  /Shared               # Cross-cutting concerns
    /Exceptions         # Domain exceptions
    /Behaviors          # Pipeline behaviors (validation, logging, etc.)
    /Middleware         # ASP.NET middleware (auth, error handling, etc.)
    /Extensions         # Extension methods, DI setup
  /API                  # API layer (minimal)
    /Controllers        # Thin controllers (route → command/query → handler)
    /Program.cs         # Startup, configuration, DI registration
  /Tests
    /Unit               # xUnit tests
    /Integration        # xUnit integration tests with WebApplicationFactory
  TodoApp.sln          # Solution file

/src/frontend/          # Next.js frontend
  /src/app             # Next.js app router pages
  /src/components      # React components
  /src/hooks           # Custom React hooks
  /src/services        # API client, utilities
  /src/types           # TypeScript interfaces
  package.json

docker-compose.yml          # Local dev environment
.env.example                # Environment variable template
.github/workflows/ci.yml     # CI pipeline
.claude/                     # Custom Claude Code resources
  /agents/                   # AI advisor agents (domain modeling, testing, security, etc.)
  /skills/                   # Automated skills (scaffolding, migrations, testing, etc.)
  /README.md                 # Guide to custom resources
SKILLS_AND_AGENTS.md         # Detailed documentation of all helpers
```

## Custom Skills & Agents

This repository includes custom Claude Code **skills** and **agents** (defined in `/.claude/`) to accelerate development:

**Skills** (automated helpers for common tasks):
- `/scaffold-slice` — Generate boilerplate for new vertical slices (command/query + handler + validator + tests)
- `/add-migration` — Create and apply EF Core migrations safely
- `/test-slice` — Run tests for a specific feature slice
- `/check-architecture` — Validate Clean Architecture compliance and identify violations
- `/api-client-gen` — Generate TypeScript interfaces and API client from backend DTOs
- `/seed-data-gen` — Generate realistic development seed data

**Agents** (specialized advisors for complex decisions):
- Domain Modeling — Design entities, aggregates, and value objects
- API Contract — Review endpoint design (DTOs, validation, status codes)
- Test Strategy — Plan comprehensive test approaches
- Database Query Optimization — Optimize complex queries and identify N+1 problems
- Frontend Component Architecture — Design component hierarchy and state flow
- Security Review — Audit authentication, authorization, and vulnerabilities
- Integration Test — Write end-to-end test scenarios

See [SKILLS_AND_AGENTS.md](SKILLS_AND_AGENTS.md) for detailed documentation.

## Key Architectural Decisions

1. **Dual Organization Model**: Tasks belong to both a system list (`SystemList`: Inbox/Next/Upcoming/Someday) AND optionally a `ProjectId`. This preserves GTD workflow while adding project grouping.
2. **Soft-Delete for Completion**: Completed tasks have `IsArchived = true` and `CompletedAt` timestamp. Hard-delete via `DELETE` endpoint is separate.
3. **Sort Order at DB Level**: `SortOrder` (int) field enables user-controlled manual ordering via drag-and-drop. Reorder operations update in an atomic transaction.
4. **Upcoming as Computed View**: The "Upcoming" view merges:
   - Tasks with due dates within 14 days (from ANY list)
   - Tasks explicitly in the Upcoming system list
5. **JWT Stateless Auth**: No server-side sessions. JWT tokens validated per request via ASP.NET Core middleware. Passwords hashed with PBKDF2 (100k+ iterations) + salt.

## Quick Start: Scaffolding a New Feature

For fastest development, use the custom scaffolding skill:

```bash
# Example: Create a new "CreateTask" command
/scaffold-slice CreateTask command

# This generates:
# - CreateTaskCommand.cs, CreateTaskCommandValidator.cs, CreateTaskResponse.cs
# - CreateTaskHandler.cs with boilerplate logic
# - Unit test template (CreateTaskHandlerTests.cs)
# - DI registration in Program.cs

# Then test your implementation:
/test-slice CreateTask

# And check architecture compliance:
/check-architecture
```

See [SKILLS_AND_AGENTS.md](SKILLS_AND_AGENTS.md) for all available helpers.

## Common Development Commands

### Backend (.NET / C#)

```bash
# From /src/backend directory
dotnet build                      # Build solution
dotnet test                       # Run all unit + integration tests
dotnet test --filter "CreateTask" # Run tests for specific feature
dotnet watch test                 # Auto-rebuild and test on changes
dotnet run                        # Start API (http://localhost:5000)

# Database & Migrations
# Run from /src/backend directory
dotnet ef migrations add MigrationName -p TodoApp.Infrastructure -s TodoApp.Api
dotnet ef database update         # Apply pending migrations
dotnet ef database drop           # Drop database (dev only)

# Custom scaffolding (run from repo root)
/scaffold-slice FeatureName command   # Generate vertical slice boilerplate
/add-migration MigrationName          # Create EF migration
/check-architecture                   # Validate Clean Architecture compliance
/api-client-gen Tasks                 # Generate TypeScript API client
```

### Frontend (Next.js)

```bash
# From /src/frontend
npm install                     # Install dependencies
npm run dev                     # Start dev server (http://localhost:3000)
npm run build                   # Production build
npm test                        # Run tests
npm run lint                    # Run ESLint

# Watch mode during development
npm run dev                     # Includes hot-reload
```

### Docker / Local Environment

```bash
# From repository root
docker-compose up               # Start PostgreSQL + backend + frontend
docker-compose down             # Stop all services
docker-compose logs -f          # Follow logs from all services
docker-compose logs api         # Logs for specific service
```

### CI / Code Quality

```bash
# Backend code style is enforced by EditorConfig + Roslyn analyzers
# Frontend linting
npm run lint --prefix src/frontend

# Building for production
dotnet build -c Release /src/backend
npm run build --prefix src/frontend
```

## Backend Architecture: Clean Architecture + Vertical Slice Architecture

**Philosophy**: Organize code by business feature (vertical slices) while maintaining Clean Architecture principles (dependency inversion, independence from frameworks).

**Core Principles**:
1. **Dependency Flow**: Always inward toward core business logic. Controllers depend on handlers, handlers depend on domain entities/interfaces, never the reverse.
2. **Vertical Slices**: Each feature (e.g., CreateTask, CompleteTask, GetTasks) is self-contained with its own command/query, handler, validators, and responses.
3. **Domain-Centric**: Domain entities and rules are completely independent of EF Core, ASP.NET, or any other framework. Use value objects, aggregates, and domain events where appropriate.
4. **CQRS Pattern** (Command Query Responsibility Segregation): Separate read (queries) from write (commands) operations, each with its own handler.

**Layers**:
- **Domain (Core)**: Pure business logic, no dependencies. Entities, enums, value objects, domain exceptions, specifications.
- **Application (Use Cases)**: Command/query handlers implement use cases. DTOs for request/response. Validators. No dependency on infrastructure or frameworks.
- **Infrastructure (Shared)**: EF Core DbContext, repositories, external service clients, logging. Implements domain interfaces.
- **API (Entry Point)**: Thin controllers that route HTTP requests to command/query handlers. No business logic here.
- **Shared (Cross-Cutting)**: Exception handling, validation pipelines, authentication middleware, utilities.

**Handler Pattern** (MediatR-style):
```
Controller receives HTTP request
  ↓
Controller creates Command/Query object
  ↓
Controller sends to handler via mediator/DI
  ↓
Handler executes business logic using domain entities and repositories
  ↓
Handler returns Response DTO
  ↓
Controller serializes response
```

**Key Endpoints** (see backlog for full spec):
- `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
- `GET /api/health` — Health check (returns 200 OK)
- `POST /api/tasks`, `GET /api/tasks`, `PUT /api/tasks/{id}`, `DELETE /api/tasks/{id}`
- `PATCH /api/tasks/{id}/complete`, `PATCH /api/tasks/{id}/reopen`
- `PATCH /api/tasks/reorder` — Atomic sort order update
- Similar CRUD for `/api/projects` and `/api/labels`

## Database Schema & Domain Entities

**Domain Entities** (defined in `/Core/Domain`, pure C# classes with NO EF Core dependencies):
- Pure business logic, validation rules, private setters
- No `[Column]`, `[Table]`, `[Required]` attributes
- Use factory methods for construction to enforce invariants

**EF Core Configurations** (in Infrastructure layer, separate from domain):
- Fluent API configurations in `EntityTypeConfiguration<T>` classes
- Maps domain entities to database tables, columns, relationships
- Configures constraints, indexes, cascade delete behavior

**Domain Model**:
- **User**: Id, Email (unique), PasswordHash, PasswordSalt, DisplayName, CreatedAt, UpdatedAt
- **TodoTask**: Id, Name, Description, DueDate, Priority (P1-P4), Status (Open/Done), SystemList (Inbox/Next/Upcoming/Someday), SortOrder, ProjectId (FK, nullable), UserId (FK), IsArchived, CompletedAt, CreatedAt, UpdatedAt
- **Project**: Id, Name, Description, DueDate, Status (Active/Completed), UserId (FK), SortOrder, CreatedAt, UpdatedAt
- **Label**: Id, Name (unique per user), Color (hex, nullable), UserId (FK), CreatedAt
- **TaskLabel**: TaskId (FK), LabelId (FK), composite PK (join table)

**Key Indexes**: UserId, ProjectId, SystemList, Status, DueDate, Email (unique)

**Example Domain Entity Structure**:
```csharp
namespace TodoApp.Domain.Entities;

public class TodoTask
{
    private TodoTask() { }  // EF Core only

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Priority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public SystemList SystemList { get; private set; }
    public int SortOrder { get; private set; }
    public Guid? ProjectId { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Business rule: cannot complete if already done
    public void Complete()
    {
        if (Status == TaskStatus.Done)
            throw new InvalidOperationException("Task is already completed.");
        Status = TaskStatus.Done;
        IsArchived = true;
        CompletedAt = DateTime.UtcNow;
    }

    // Factory method for creating new task
    public static TodoTask Create(Guid userId, string name, string? description,
        SystemList systemList = SystemList.Inbox, Priority priority = Priority.P4)
    {
        return new TodoTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            Priority = priority,
            Status = TaskStatus.Open,
            SystemList = systemList,
            SortOrder = 0,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
```

## Frontend Architecture

- **Pages** (in `app/`): System list views (`/inbox`, `/next`, `/upcoming`, `/someday`), project views (`/projects/{id}`), label views (`/labels/{id}`), auth pages (`/login`, `/register`)
- **Components**: Modular React components (TaskList, TaskRow, TaskDetail, Sidebar, etc.) with props and callbacks
- **API Client** (`services/apiClient.ts`): Centralized HTTP client with base URL config, JWT token injection, error handling
- **Auth Context**: Global auth state (user, isAuthenticated, isLoading) with localStorage persistence and token validation on app load
- **Next.js Middleware**: Protected routes — redirect unauthenticated users to login, authenticated users away from auth pages
- **Types** (`types/index.ts`): Enums and interfaces mirroring backend models (Priority, TaskStatus, SystemList, etc.)

## Testing Strategy

### Backend (Vertical Slice Testing)

**Unit Tests** (TodoApp.UnitTests):
- Test **handlers** in isolation with mocked repositories/services
- Test **validators** for correct validation rules and error messages
- Test **domain logic** (business rules within entities)
- Test pure functions, value objects, and specifications
- Example:
  ```csharp
  [Fact]
  public async Task Handle_ValidCommand_CreatesTaskAndReturnsResponse()
  {
      var command = new CreateTaskCommand { Name = "Buy milk", SystemList = SystemList.Inbox };
      var handler = new CreateTaskHandler(mockTaskRepository, mockUserRepository);

      var response = await handler.Handle(command, CancellationToken.None);

      Assert.NotNull(response);
      mockTaskRepository.Verify(r => r.AddAsync(It.IsAny<TodoTask>()), Times.Once);
  }
  ```

**Integration Tests** (TodoApp.IntegrationTests):
- Test **full vertical slice** via HTTP using WebApplicationFactory
- Test **database persistence** with real EF Core DbContext
- Test **validation**, **authorization**, **error handling**
- Example:
  ```csharp
  [Fact]
  public async Task CreateTask_WithValidRequest_Returns201AndTask()
  {
      var request = new CreateTaskCommand { Name = "Buy milk" };
      var response = await _client.PostAsJsonAsync("/api/tasks", request);

      Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
      var task = await response.Content.ReadAsAsync<CreateTaskResponse>();
      Assert.NotNull(task.Id);
  }
  ```

- Run tests during development with `dotnet watch test`
- CI runs all tests on PR to main

### Frontend

- **Component Tests** (Jest + React Testing Library): Isolated component behavior, mocking API calls
- Run with `npm test`; snapshot testing for layouts
- Coverage reporting configured in Jest config

## Shared / Cross-Cutting Concerns

**These apply to all vertical slices and are centralized in `/Shared`**:

- **Exception Handling Middleware**: Catches unhandled exceptions, returns standardized error responses
- **Validation Behavior**: Automatic validation pipeline that runs on all commands (hooks into handler pipeline)
- **Logging & Correlation**: Track requests with correlation IDs for debugging
- **Authentication Middleware**: Validates JWT, extracts user claims, sets `HttpContext.User`
- **Authorization**: `[Authorize]` attribute on protected endpoints; fine-grained checks in handlers
- **CORS Configuration**: Allows frontend origin
- **Health Check Endpoint**: `GET /api/health` — shared across all slices

## Configuration & Environment Setup

### Environment Variables

Create a `.env` file (or `.env.local` for local overrides) from `.env.example`:

```bash
cp .env.example .env
```

**Backend** (`appsettings.json` / `appsettings.Development.json`):
- `ConnectionStrings:DefaultConnection` — PostgreSQL connection string (e.g., `Server=localhost;Database=todo_app;User Id=postgres;Password=...`)
- `Jwt:SecretKey` — Signing key (min 256 bits; generate with `openssl rand -base64 32`)
- `Jwt:Issuer` — Token issuer (e.g., "TodoApp")
- `Jwt:Audience` — Token audience (e.g., "TodoApp.Client")
- `Jwt:ExpirationMinutes` — Token expiration (e.g., 1440 = 24 hours)
- `Cors:AllowedOrigins` — Frontend origin(s), comma-separated (e.g., `http://localhost:3000,https://yourdomain.com`)
- `ASPNETCORE_ENVIRONMENT` — Set to `Development`, `Staging`, or `Production`

**Frontend** (`.env.local` / `.env.development.local`):
- `NEXT_PUBLIC_API_URL` — Backend base URL (e.g., `http://localhost:5000`)
- `NEXT_PUBLIC_APP_NAME` — App name for UI (e.g., "GTD Todo")

**Docker Compose** (`.env` file in repo root):
- Uses `.env.example` to document all required variables
- Automatically configures PostgreSQL, backend, and frontend services
- Seed data is applied automatically on first `docker-compose up`

### Local Development Setup

```bash
# 1. Clone repository and install dependencies
git clone <repo-url>
cd todo-app
cp .env.example .env

# 2a. Option A: Use Docker (recommended)
docker-compose up
# Frontend: http://localhost:3000
# Backend API: http://localhost:5000
# Swagger: http://localhost:5000/swagger

# 2b. Option B: Manual setup (requires PostgreSQL 15+ locally)
# Backend
cd src/backend
dotnet restore
dotnet ef database update  # Apply migrations
dotnet run

# Frontend (in another terminal)
cd src/frontend
npm install
npm run dev
```

### Quick Configuration Check

Verify your setup works:

```bash
# Backend health check
curl http://localhost:5000/api/health

# Frontend loads
open http://localhost:3000  # Should show login page
```

## Development Workflow

### Backend (Vertical Slice Pattern)

1. **New Feature**: Read related stories in [docs/backlog.md](docs/backlog.md) for acceptance criteria
2. **Identify the Use Case**: Determine if it's a command (write) or query (read)
3. **Create the Vertical Slice**:
   - Create a folder under `/Features/{FeatureName}/{UseCaseName}` (e.g., `/Features/Tasks/CreateTask`)
   - Add files:
     - `CreateTaskCommand.cs` — Input DTO with all required fields
     - `CreateTaskCommandValidator.cs` — FluentValidation validator
     - `CreateTaskResponse.cs` — Output DTO
     - `CreateTaskHandler.cs` — Core business logic (implements `ICommandHandler<CreateTaskCommand, CreateTaskResponse>`)
4. **Domain Layer**:
   - Ensure domain entities exist and are pure (no EF Core attributes)
   - Domain entities should contain business rules
   - Create value objects or specifications if needed
5. **Infrastructure**:
   - Implement repository interfaces that the handler depends on
   - EF Core configurations in separate `EntityTypeConfiguration` classes
6. **Register in DI**:
   - Add handler registration in `Program.cs` (e.g., `services.AddScoped<ICommandHandler<CreateTaskCommand, CreateTaskResponse>, CreateTaskHandler>`)
7. **Add Controller**:
   - Create thin controller action that routes to handler (or use minimal APIs)
   - Minimal code: deserialize request → call handler → serialize response
8. **Write Tests**:
   - **Unit Tests**: Test handler logic in isolation with mocked dependencies
   - **Integration Tests**: Test full flow via HTTP using WebApplicationFactory
9. **Database Changes** (if needed):
   - Modify domain entity
   - Update EF Core `EntityTypeConfiguration`
   - Generate migration: `dotnet ef migrations add MigrationName -p TodoApp.Infrastructure -s TodoApp.Api`
   - Test migration: `dotnet ef database update`
10. **Verify**:
    - Run `dotnet test` — all must pass
    - Test manually via `dotnet run` + Swagger/Postman

### Frontend

1. **Create Components/Pages** as needed for the feature
2. **Use API Client** to call new endpoints
3. **Write Component Tests** using Jest + React Testing Library
4. **Run `npm run dev`** to test locally

### End-to-End

1. **Full test suite**: `dotnet test` (backend) + `npm test` (frontend)
2. **Docker**: `docker-compose up` to test entire stack
3. **Manual user flows**: Register → create tasks → complete → archive
4. **Push & CI**: Commit and push; GitHub Actions validates

## Common Patterns & Conventions

### Backend (Vertical Slice / CQRS)

- **Commands** (Writes): `Create{Feature}Command`, `Update{Feature}Command`, `Delete{Feature}Command`
  - Contain input data and validation rules
  - Have corresponding `{Feature}Handler : ICommandHandler<{Feature}Command, {Feature}Response>`
  - Each command is a self-contained vertical slice
- **Queries** (Reads): `Get{Feature}Query`, `Get{Features}Query`
  - Fetch data without modifying state
  - Have corresponding `{Feature}Handler : IQueryHandler<{Feature}Query, {Feature}Response>`
- **Validators**: `{Command/Query}Validator : AbstractValidator<{Command/Query}>` using FluentValidation
  - Validate at the slice level, not in handlers
  - Return validation errors in consistent format
- **Responses**: `{Feature}Response` DTOs
  - Contain only data needed for HTTP response
  - Map from domain entities in the handler
- **Domain Entities**: Pure business logic, no EF Core attributes
  - Use private setters to enforce invariants
  - Private constructors + factory methods for proper initialization
  - Contain business rules (e.g., "task cannot be completed if already done")
- **Repositories**: Interfaces in Core/Interfaces, implementations in Infrastructure
  - Thin data access layer; queries mostly in handlers
  - Use EF Core's `IQueryable<T>` when possible for composability
- **Error Responses**: Standardized JSON format with status code, message, correlation ID, validation errors
- **Authorization**:
  - `[Authorize]` attribute on protected controller endpoints
  - User ID extracted from JWT claims in handlers: `User.FindFirst(ClaimTypes.NameIdentifier)`
  - Filter queries by authenticated user ID (e.g., only return user's own tasks)
- **Soft-delete Queries**: Always filter `IsArchived = false` and `Status = Open` unless explicitly querying completed tasks
- **Timestamp Fields**: All domain entities have `CreatedAt` and `UpdatedAt` (UTC). Tasks also have `CompletedAt`.
- **Idempotency**: Some operations (e.g., assigning a label already assigned) should be idempotent — return success

### Frontend

- **Optimistic UI**: Update UI immediately on user action, revert on API error with toast notification
- **API Client**: Centralized in `services/apiClient.ts` with base URL, JWT injection, error handling

## File Naming & Organization

**Vertical Slice Conventions**:
- Feature folder: `/Features/{FeatureArea}/{UseCaseName}` (e.g., `/Features/Tasks/CreateTask`)
- Files within slice:
  - `{UseCaseName}Command.cs` — Command DTO (writes)
  - `{UseCaseName}Query.cs` — Query DTO (reads)
  - `{UseCaseName}CommandValidator.cs` / `{UseCaseName}QueryValidator.cs`
  - `{UseCaseName}Response.cs` — Response DTO
  - `{UseCaseName}Handler.cs` — Handler implementing business logic
- Domain entities: `/Core/Domain/{EntityName}.cs`
- Domain enums: `/Core/Domain/Enums/{EnumName}.cs`
- EF Core configs: `/Infrastructure/Persistence/Configurations/{EntityName}Configuration.cs`
- Shared interfaces: `/Core/Interfaces/{InterfaceName}.cs`
- Shared exceptions: `/Shared/Exceptions/{ExceptionName}.cs`

## Links & Resources

- **Backlog & Spec**: [docs/backlog.md](docs/backlog.md)
- **Todoist**: The app is inspired by Todoist UI/UX patterns
- **GTD Methodology**: https://gettingthingsdone.com/ — System lists implement David Allen's GTD framework

### Architecture Resources
- **Clean Architecture**: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html — Robert C. Martin's foundational article
- **Vertical Slice Architecture**: https://jimmybogard.com/vertical-slice-architecture/ — Jimmy Bogard's introduction to vertical slices
- **CQRS Pattern**: https://martinfowler.com/bliki/CQRS.html — Martin Fowler on Command Query Responsibility Segregation
- **MediatR**: https://github.com/jbogard/MediatR — Popular .NET mediator implementation (consider using for this project)

### Framework & Tool Docs
- **.NET Docs**: https://docs.microsoft.com/dotnet/
- **Entity Framework Core**: https://docs.microsoft.com/ef/core/
- **ASP.NET Core**: https://docs.microsoft.com/aspnet/core/
- **FluentValidation**: https://fluentvalidation.net/ — Recommended for validators
- **Next.js Docs**: https://nextjs.org/docs
- **Tailwind CSS**: https://tailwindcss.com/

## Getting Started with Implementation

When you're ready to start building features, here's the recommended flow:

1. **Read the relevant epic/story** in [docs/backlog.md](docs/backlog.md) to understand acceptance criteria
2. **Design the domain model** — Use the "Domain Modeling Agent" for complex entities
3. **Create the vertical slice** — Run `/scaffold-slice FeatureName command` (or `query` for reads)
4. **Implement the handler logic** — Add business logic to the generated handler
5. **Write tests** — Use `/test-slice` to run auto-generated unit tests
6. **Check architecture** — Run `/check-architecture` before committing
7. **Database changes** — Use `/add-migration` for schema updates

**Example: Implementing CreateTask**

```bash
# 1. Scaffold the slice
/scaffold-slice CreateTask command

# 2. The generator creates:
#    - CreateTaskCommand.cs (input DTO)
#    - CreateTaskCommandValidator.cs (validation rules)
#    - CreateTaskHandler.cs (business logic — implement here!)
#    - CreateTaskResponse.cs (output DTO)
#    - CreateTaskHandlerTests.cs (unit test template)
#    - DI registration in Program.cs

# 3. Implement handler logic in CreateTaskHandler.cs

# 4. Run and verify tests
/test-slice CreateTask --unit-only

# 5. Run integration tests
/test-slice CreateTask --integration-only

# 6. Check architecture compliance
/check-architecture

# 7. Commit and push to trigger CI
```

## Notes for Future Development

- **Epic 1 (Infrastructure)** is a prerequisite for all other epics. Complete basic project scaffolding, database schema, CI pipeline, and test infrastructure before moving to Epic 2.
- **Epic 2 (Authentication)** must be completed before user-specific features. Focus on secure password hashing (PBKDF2) and JWT token generation.
- **Tasks (Epic 4)** are the core feature — invest quality in domain modeling, query optimization, and test coverage here.
- **System lists** implement GTD philosophy — ensure filters (Inbox, Next, Upcoming, Someday) and views are correct per the backlog requirements.
- **Manual sort order** via `SortOrder` is essential for user control; test drag-and-drop reordering thoroughly with integration tests.
- **Upcoming view** is special: it's a computed view combining date-driven tasks (due within 14 days) with tasks explicitly assigned to Upcoming; test both paths.
- **End-to-end testing** is critical — test complete user workflows (register → create task → assign to Next → complete → archive) using integration tests.
- **Custom skills accelerate development** — Always use `/scaffold-slice` for new features rather than copying existing code; it maintains consistency and includes DI registration.
- **Architecture validation** — Run `/check-architecture` before every commit to catch dependency flow violations, public setters, or framework leakage into domain layer.
