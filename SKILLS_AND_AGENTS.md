# Skills and Agents Reference Guide

This guide documents all custom skills and agents available for this project. These tools accelerate development by automating repetitive tasks and providing specialized guidance.

## Quick Reference

| Skill/Agent | Type | Purpose | Usage |
|---|---|---|---|
| **scaffold-slice** | Skill | Generate vertical slice boilerplate | `/scaffold-slice CreateTask command` |
| **add-migration** | Skill | Create EF Core migration | `/add-migration AddTaskPriorityIndex` |
| **test-slice** | Skill | Run tests for specific feature | `/test-slice CreateTask` |
| **check-architecture** | Skill | Validate architecture compliance | `/check-architecture` |
| **api-client-gen** | Skill | Generate frontend API client | `/api-client-gen Tasks` |
| **seed-data-gen** | Skill | Generate development seed data | `/seed-data-gen` |
| **Domain Modeling** | Agent | Design domain entities | Use Task tool with `subagent_type=general-purpose` |
| **API Contract** | Agent | Review API design | Use Task tool with `subagent_type=Explore` |
| **Test Strategy** | Agent | Design test approach | Use Task tool with `subagent_type=general-purpose` |
| **DB Query Optimization** | Agent | Optimize complex queries | Use Task tool with `subagent_type=Explore` |
| **Frontend Component Arch** | Agent | Design component structure | Use Task tool with `subagent_type=Plan` |
| **Security Review** | Agent | Audit security implementation | Use Task tool with `subagent_type=Explore` |
| **Integration Test** | Agent | Write integration tests | Use Task tool with `subagent_type=general-purpose` |

---

## Skills (Automated Helpers)

Each skill is a reusable tool that performs a specific development task. Skills are invoked from Claude Code and follow a standard workflow.

### 1. scaffold-slice — Vertical Slice Scaffolding

**Location**: `/.claude/skills/scaffold-slice.md`

**Purpose**: Generate all boilerplate files for a new vertical slice (command/query + handler + validator + response)

**When to use**: Creating any new use case (CreateTask, UpdateTask, CompleteTask, etc.)

**How to invoke**:
```
/scaffold-slice CreateTask command
/scaffold-slice GetTasks query
/scaffold-slice CompleteTask command
```

**What it generates**:
- `Features/{Feature}/{UseCaseName}/{UseCaseName}Command.cs` (or Query)
- `Features/{Feature}/{UseCaseName}/{UseCaseName}Handler.cs`
- `Features/{Feature}/{UseCaseName}/{UseCaseName}CommandValidator.cs` (or QueryValidator)
- `Features/{Feature}/{UseCaseName}/{UseCaseName}Response.cs`
- `Tests/Unit/{UseCaseName}HandlerTests.cs`
- DI registration entry in `Program.cs`

**Expected output**:
```
✓ Created: /src/backend/Features/Tasks/CreateTask/CreateTaskCommand.cs
✓ Created: /src/backend/Features/Tasks/CreateTask/CreateTaskHandler.cs
✓ Created: /src/backend/Features/Tasks/CreateTask/CreateTaskCommandValidator.cs
✓ Created: /src/backend/Features/Tasks/CreateTask/CreateTaskResponse.cs
✓ Created: /src/backend/Tests/Unit/Features/Tasks/CreateTaskHandlerTests.cs
✓ Updated: Program.cs with DI registration
ℹ Next steps: Implement handler logic and domain entity
```

---

### 2. add-migration — EF Core Migration Helper

**Location**: `/.claude/skills/add-migration.md`

**Purpose**: Create and optionally apply EF Core migrations with correct project references

**When to use**: After modifying domain entities or entity configurations

**How to invoke**:
```
/add-migration AddTaskPriorityIndex
/add-migration CreateUserEntity
/add-migration AddProjectSoftDelete
```

**What it does**:
1. Prompts for migration name (if not provided)
2. Runs: `dotnet ef migrations add {Name} -p TodoApp.Infrastructure -s TodoApp.Api`
3. Shows migration file preview
4. Asks if you want to apply with `dotnet ef database update`
5. Confirms success or shows errors

**Expected output**:
```
? Migration name: AddTaskPriorityIndex
✓ Migration created: Migrations/20260213_AddTaskPriorityIndex.cs
ℹ Preview:
  - Add index on Task.Priority
  - Add index on Task.DueDate

? Apply migration now? (y/n): y
✓ Database updated successfully
ℹ Next: Test database changes and verify schema
```

---

### 3. test-slice — Feature-Specific Test Runner

**Location**: `/.claude/skills/test-slice.md`

**Purpose**: Run unit and integration tests for a specific vertical slice

**When to use**: After implementing a feature to get fast feedback

**How to invoke**:
```
/test-slice CreateTask
/test-slice GetTasks --integration-only
/test-slice CompleteTask --unit-only
```

**Options**:
- `--unit-only` — Run only unit tests
- `--integration-only` — Run only integration tests
- `--verbose` — Show detailed output

**What it does**:
1. Detects feature from current file or prompts
2. Runs: `dotnet test --filter "CreateTask" TodoApp.UnitTests`
3. Runs: `dotnet test --filter "CreateTask" TodoApp.IntegrationTests`
4. Displays results with pass/fail summary

**Expected output**:
```
Testing CreateTask...
✓ Unit Tests (3 passed)
  ✓ Handle_ValidCommand_CreatesTask
  ✓ Handle_InvalidName_Returns400
  ✓ Handle_DuplicateEmail_Returns409

✓ Integration Tests (2 passed)
  ✓ CreateTask_WithValidRequest_Returns201
  ✓ CreateTask_WithInvalidProject_Returns400

Summary: 5 passed, 0 failed (~2.3s)
```

---

### 4. check-architecture — Architecture Validation

**Location**: `/.claude/skills/check-architecture.md`

**Purpose**: Detect architecture violations (framework dependencies in domain, business logic in controllers, etc.)

**When to use**: Before committing, to ensure Clean Architecture principles are maintained

**How to invoke**:
```
/check-architecture
/check-architecture --strict
```

**What it checks**:
- Domain entities have no EF Core attributes (`[Table]`, `[Column]`, etc.)
- Controllers contain no business logic (only routing)
- Handlers don't directly reference Infrastructure (only interfaces)
- No cross-slice dependencies
- Proper file naming conventions
- No public setters on domain entities

**Expected output**:
```
Architecture Check Report
========================

✓ Domain Entities: Clean (8/8 entities valid)
✓ Controllers: Thin (12/12 controllers compliant)
✓ Handlers: Proper dependencies (45/45 handlers ok)
✓ File Naming: Consistent (100%)
✗ Issues Found: 2

  [WARNING] Features/Tasks/CreateTask/CreateTaskHandler.cs:15
    Handler directly references DbContext instead of ITaskRepository
    Fix: Inject ITaskRepository interface instead

  [WARNING] Features/Tasks/UpdateTask/UpdateTaskCommand.cs:8
    Property 'Name' is public writable (should be private set)
    Fix: Change to public string Name { get; private set; }

Summary: 2 warnings, 0 critical errors
Recommendation: Fix warnings before merging
```

---

### 5. api-client-gen — Frontend API Client Generator

**Location**: `/.claude/skills/api-client-gen.md`

**Purpose**: Generate TypeScript interfaces and API client methods from backend DTOs

**When to use**: After adding/modifying backend response DTOs

**How to invoke**:
```
/api-client-gen Tasks
/api-client-gen Auth
/api-client-gen Projects
```

**What it generates**:
- TypeScript interfaces for all request/response DTOs
- API client methods with proper types
- Updates to `src/services/apiClient.ts`
- Updates to `src/types/index.ts`

**Expected output**:
```
Generating API client for Tasks...
✓ Scanned: CreateTaskCommand, CreateTaskResponse, GetTasksQuery, GetTasksResponse
✓ Generated: src/types/tasks.ts
✓ Generated API methods:
  - tasks.create(command: CreateTaskCommand): Promise<CreateTaskResponse>
  - tasks.list(query: GetTasksQuery): Promise<GetTasksResponse[]>
  - tasks.update(id: string, command: UpdateTaskCommand): Promise<TaskResponse>
  - tasks.complete(id: string): Promise<TaskResponse>
  - tasks.delete(id: string): Promise<void>

✓ Updated: src/services/apiClient.ts
ℹ Next: Test API calls in components
```

---

### 6. seed-data-gen — Development Seed Data Generator

**Location**: `/.claude/skills/seed-data-gen.md`

**Purpose**: Generate realistic development seed data (test users, tasks, projects, labels)

**When to use**: Setting up development environment, testing with realistic data

**How to invoke**:
```
/seed-data-gen
/seed-data-gen --heavy  # More data for stress testing
/seed-data-gen --minimal  # Just essentials
```

**What it generates**:
- C# seeder class in `Infrastructure/Data/Seeders/DevelopmentSeeder.cs`
- 2-3 test users with known credentials
- 20-30 tasks across all system lists
- 3-5 projects with tasks
- 5-8 labels assigned to tasks
- Documentation of test user credentials

**Expected output**:
```
Generating seed data...
✓ Created: Infrastructure/Data/Seeders/DevelopmentSeeder.cs

Test Users Generated:
  1. alice@example.com / Password123 (admin)
  2. bob@example.com / Password456
  3. carol@example.com / Password789

Sample Data:
  - 25 tasks across Inbox, Next, Upcoming, Someday
  - 4 projects with 5-6 tasks each
  - 6 labels: Work, Personal, Urgent, Review, Bug, Feature
  - 8 completed tasks in archive

To apply seed data:
  1. Configure appsettings.Development.json
  2. Run: dotnet ef database update
  3. Credentials will be seeded automatically

ℹ Seed data is idempotent (safe to run multiple times)
```

---

## Agents (Specialized Advisors)

Agents are AI-powered assistants that provide guidance on complex topics. They explore code, ask clarifying questions, and suggest approaches.

### How to Invoke Agents

Use the Task tool with the provided prompts:

```
Task tool → subagent_type: general-purpose / Explore / Plan
Copy the relevant prompt from `/.claude/agents/{agent-name}.md`
```

---

### 1. Domain Modeling Agent

**Location**: `/.claude/agents/domain-modeling.md`

**Subagent Type**: `general-purpose`

**Purpose**: Design domain entities, aggregates, value objects, and business rules

**When to use**: Before implementing major features to ensure clean domain design

**Example invocation**:
```
Task tool:
  subagent_type: general-purpose
  prompt: See /.claude/agents/domain-modeling.md
```

**Sample prompt**:
```
Design the domain model for the Project entity with the following requirements:

1. A project has a name, description, optional due date, and status (Active/Completed)
2. A project belongs to a user
3. A project can have many tasks (but tasks belong to both project AND system list)
4. Project completion doesn't complete tasks (soft completion)
5. Completion date should be tracked
6. Sort order enables manual reordering

Please provide:
- Aggregate root design
- Private setters and invariants
- Factory methods
- Business rules (e.g., cannot complete if status already done)
- Value objects if needed
- Example code structure

Reference: See CLAUDE.md section "Backend Architecture"
```

**What to expect**:
- Detailed entity design with invariants
- Factory methods for construction
- Business rule implementations
- Value object suggestions
- Code examples matching the architecture

---

### 2. API Contract Agent

**Location**: `/.claude/agents/api-contract.md`

**Subagent Type**: `Explore`

**Purpose**: Review and validate API endpoint design (request/response DTOs, validation, status codes)

**When to use**: Before implementing endpoints to ensure consistency and completeness

**Example invocation**:
```
Review the proposed GetTasks endpoint:
- Should support filtering by systemList, projectId, labelId, status, archived
- Should return paginated results with sort order
- Should include related entities (labels, project name)
- Error cases: validation errors, authorization

Provide:
- Complete request/response DTO structures
- Validation rules for each field
- HTTP status codes for all scenarios
- Example curl commands
- Comparison with similar endpoints to ensure consistency
```

**What to expect**:
- Detailed DTO designs
- Comprehensive validation rules
- Status code recommendations
- Consistency checks with existing endpoints
- Edge case identification

---

### 3. Test Strategy Agent

**Location**: `/.claude/agents/test-strategy.md`

**Subagent Type**: `general-purpose`

**Purpose**: Design comprehensive test approach for complex features

**When to use**: For features with multiple paths, edge cases, or error scenarios

**Example invocation**:
```
Design test strategy for CompleteTask feature:

Requirements:
1. Marks task as Done and archived
2. Sets CompletedAt timestamp
3. Cannot complete if already done (idempotent)
4. Only owner can complete their tasks
5. Archived tasks appear in different view

Provide:
- Unit test cases (handler logic)
- Integration test cases (HTTP endpoints)
- Edge cases and error scenarios
- Test data setup strategy
- Expected assertions
- Mock/real database approach
```

**What to expect**:
- Comprehensive test case list
- Edge case identification
- Mock vs real database recommendations
- Example test code
- Data setup strategies

---

### 4. Database Query Optimization Agent

**Location**: `/.claude/agents/db-query-optimization.md`

**Subagent Type**: `Explore`

**Purpose**: Optimize complex database queries, identify N+1 problems, recommend indexes

**When to use**: For complex queries like GetUpcomingTasks with filtering and relationships

**Example invocation**:
```
Optimize the GetUpcomingTasks query:

Requirements:
- Fetch tasks where:
  - Due date within 14 days (any system list), OR
  - SystemList = Upcoming (regardless of due date)
- Include related: Labels (many-to-many), Project (FK), User (FK)
- Sort: Overdue first (oldest first), then by due date
- Only non-archived, open tasks
- Filter by authenticated user

Current approach has potential N+1 queries.

Provide:
- Optimized EF Core query with proper .Include()/.ThenInclude()
- Index recommendations
- SQL analysis
- Performance estimates
- Alternative approaches if applicable
```

**What to expect**:
- Optimized EF Core LINQ
- Index recommendations
- N+1 problem analysis
- SQL preview
- Performance comparison

---

### 5. Frontend Component Architecture Agent

**Location**: `/.claude/agents/frontend-component-arch.md`

**Subagent Type**: `Plan`

**Purpose**: Design component hierarchy, state management, and props flow

**When to use**: Before building major UI features (TaskList, TaskDetail, etc.)

**Example invocation**:
```
Design the TaskList component architecture:

Requirements:
- Display tasks in sort order
- Show task name, priority color, due date, project, labels
- Checkbox for completion with animation + undo toast
- Drag-and-drop reordering (within system list)
- Quick-add input at top
- Click to open detail panel
- Optimistic UI updates

Provide:
- Component hierarchy (TaskList, TaskRow, TaskDetail, etc.)
- Props interface for each component
- State management approach (Context vs hooks)
- Event flow (completion, drag-drop, edit)
- Loading/error states
- Code structure examples
```

**What to expect**:
- Component tree diagram
- Detailed props interfaces
- State management recommendations
- Event flow documentation
- Code examples

---

### 6. Security Review Agent

**Location**: `/.claude/agents/security-review.md`

**Subagent Type**: `Explore`

**Purpose**: Audit authentication, authorization, and security vulnerabilities

**When to use**: Before completing auth features and before production

**Example invocation**:
```
Security review for authentication system:

Components to review:
1. PasswordHashingService (PBKDF2 with salt)
2. JwtTokenService (token generation and validation)
3. RegisterCommand and LoginQuery endpoints
4. JWT bearer middleware
5. [Authorize] attributes on protected endpoints

Check for:
- Password hashing strength (iterations, salt size)
- JWT signing key size and algorithm
- Token expiration handling
- Refresh token strategy (if used)
- SQL injection prevention
- XSS prevention
- CORS configuration security
- Rate limiting on auth endpoints
- Account enumeration prevention

Provide:
- Vulnerability list (if any)
- Severity ratings (critical/high/medium)
- Recommended fixes
- Security best practices
```

**What to expect**:
- Vulnerability assessment
- Severity ratings
- Specific fix recommendations
- Security best practices
- Compliance notes (OWASP, etc.)

---

### 7. Integration Test Agent

**Location**: `/.claude/agents/integration-test.md`

**Subagent Type**: `general-purpose`

**Purpose**: Design and implement integration tests for complete feature flows

**When to use**: When testing end-to-end workflows with database persistence

**Example invocation**:
```
Create integration tests for complete task flow:

Flow to test:
1. Create task (POST /api/tasks)
2. Verify appears in GetTasks
3. Move to Next (PATCH /api/tasks/{id})
4. Assign label (POST /api/tasks/{id}/labels/{labelId})
5. Complete task (PATCH /api/tasks/{id}/complete)
6. Verify appears in archive
7. Reopen (PATCH /api/tasks/{id}/reopen)
8. Verify back in Next

Provide:
- Full test class using WebApplicationFactory
- Test data setup strategy
- Database cleanup between tests
- Assertions for each step
- Example code using xUnit and Moq
- Error case tests
```

**What to expect**:
- Complete test class
- WebApplicationFactory setup
- Test fixture design
- Assertion strategies
- Example implementations

---

## Usage Examples

### Example 1: Scaffolding a New Feature

```
1. Read the story in docs/backlog.md to understand requirements
2. Determine if it's command (write) or query (read)
3. Use /scaffold-slice CreateTask command
4. Implement handler logic
5. Use /test-slice CreateTask to run tests
6. Use /check-architecture to validate design
```

### Example 2: Complex Query Implementation

```
1. Review backlog requirements for GetUpcomingTasks
2. Launch "Database Query Optimization Agent"
3. Implement optimized query
4. Use /test-slice GetTasks to verify
5. Performance test with seed data
```

### Example 3: New Authentication Feature

```
1. Design domain entities using "Domain Modeling Agent"
2. Review API design with "API Contract Agent"
3. Implement endpoints using /scaffold-slice
4. Design tests with "Test Strategy Agent"
5. Security review with "Security Review Agent" before completion
```

---

## Tips for Maximum Productivity

1. **Use scaffold-slice first** — Start every feature by generating boilerplate
2. **Combine with agents** — Use agents for design, skills for implementation
3. **Run test-slice frequently** — Get immediate feedback during development
4. **check-architecture before commits** — Maintain clean architecture
5. **agent prompts are customizable** — Adjust prompts to your needs
6. **Keep skills up to date** — Update templates as conventions evolve

---

## Extending Skills and Agents

To add new skills or agents:

1. Create markdown file in `/.claude/skills/` or `/.claude/agents/`
2. Follow the template format (purpose, when to use, invocation, output)
3. Add entry to this guide's quick reference table
4. Document any new dependencies or requirements
5. Test with a sample use case

---

## Troubleshooting

**Skill not found**: Ensure you're using correct syntax `/skill-name arguments`

**Agent takes too long**: Narrow the scope of the prompt; agents work better with specific, focused tasks

**Generated code doesn't compile**: Skills generate templates; implement handler logic and verify

**Tests failing after scaffold**: Handler interface and DI registration may need adjustment; check error messages
