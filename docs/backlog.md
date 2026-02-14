# GTD Todo Application - Product Backlog

> **Project:** GTD-based Todo Application (Todoist-style)
> **Tech Stack:** React/Next.js (Frontend) | C# 9 / PostgreSQL (Backend) | Web Only
> **Date Created:** 2026-02-13

## Context

Build a multi-user, web-based GTD (Getting Things Done) todo application similar to Todoist. The app organizes tasks into four system lists (Inbox, Next, Upcoming, Someday), supports user-created Projects and Labels, and provides a clean Todoist-like UI. Key requirements: email/password auth with salted hash, P1-P4 priority, manual task ordering, soft-delete for completed tasks, hard-delete for removed items, and dual organization (tasks belong to both a system list and optionally a project).

---

## Epic 1: Project Infrastructure & Setup

**Description:** Establish the foundational project structure, development environment, CI/CD pipeline, and database schema for both the C# backend and the React/Next.js frontend. This epic ensures that all developers can clone the repository and begin productive work immediately, with consistent tooling, code quality standards, and deployment capabilities in place.

**Business Value:** Without solid infrastructure, every subsequent feature will be slower to build, harder to test, and more error-prone. This epic de-risks the entire project by setting up conventions, automated quality gates, and a deployable skeleton from day one.

---

### Feature 1.1: Backend Project Scaffolding (C# 9 / ASP.NET Core Web API)

**Description:** Initialize the C# backend project with a clean layered architecture (API, Application/Service, Domain, Infrastructure/Data layers), configure dependency injection, set up configuration management, and establish coding conventions.

**Acceptance Criteria (High-Level):**
- A working ASP.NET Core Web API project exists under `/src/backend`
- The solution follows a layered architecture with clear separation of concerns
- The project builds successfully with `dotnet build`
- A health-check endpoint (`GET /api/health`) returns 200 OK
- Swagger/OpenAPI documentation is auto-generated and accessible in development
- EditorConfig and Roslyn analyzers enforce consistent code style

---

#### Story 1.1.1: Initialize ASP.NET Core Web API Solution — `5 SP`
- [x] **COMPLETED**

**Description:** Create the .NET solution file and project structure under `/src/backend`. The solution should contain four projects: `TodoApp.Api` (controllers, middleware, startup), `TodoApp.Application` (services, DTOs, interfaces), `TodoApp.Domain` (entities, enums, value objects), and `TodoApp.Infrastructure` (EF Core DbContext, repositories, migrations). Configure project references so that dependencies flow inward.

**Acceptance Criteria:**
- Solution file `TodoApp.sln` exists at `/src/backend/TodoApp.sln`
- Four projects exist: `TodoApp.Api`, `TodoApp.Application`, `TodoApp.Domain`, `TodoApp.Infrastructure`
- Project references enforce layered dependency: Api → Application + Infrastructure; Application → Domain; Infrastructure → Domain
- Solution builds with `dotnet build` with zero errors and zero warnings
- `.editorconfig` is present at solution root with C# conventions
- Roslyn analyzers are configured

---

#### Story 1.1.2: Configure Dependency Injection and Application Startup — `5 SP`
- [x] **COMPLETED**

**Description:** Set up the ASP.NET Core dependency injection container in `Program.cs` to register services, repositories, and the DbContext. Configure the middleware pipeline with proper ordering: exception handling, CORS, authentication, authorization, routing, and Swagger. Support environment-based configuration via `appsettings.{Environment}.json`.

**Acceptance Criteria:**
- `Program.cs` configures DI container with service registration extension methods (e.g., `AddApplicationServices()`, `AddInfrastructureServices()`)
- CORS policy allows the frontend origin (configurable via appsettings)
- Global exception handling middleware returns standardized error responses (JSON with status code, message, correlation ID)
- Swagger UI is available at `/swagger` in Development environment only
- `GET /api/health` returns `200 OK` with `{ "status": "healthy", "timestamp": "..." }`
- Configuration values can be overridden by environment variables

---

#### Story 1.1.3: Set Up Unit and Integration Test Projects — `5 SP`
- [x] **COMPLETED**

**Description:** Add test projects: `TodoApp.UnitTests` (xUnit, for testing services and domain logic) and `TodoApp.IntegrationTests` (xUnit with WebApplicationFactory, for testing API endpoints). Configure test conventions and a base test class.

**Acceptance Criteria:**
- `TodoApp.UnitTests` project exists and references `TodoApp.Application` and `TodoApp.Domain`
- `TodoApp.IntegrationTests` project exists and references `TodoApp.Api`
- At least one passing unit test exists (e.g., health check service)
- At least one passing integration test exists (e.g., `GET /api/health` returns 200)
- Tests run with `dotnet test` from the solution root
- Test projects use xUnit with Moq or NSubstitute for mocking

---

### Feature 1.2: Database Setup & Schema

**Description:** Configure Entity Framework Core with PostgreSQL, establish the database connection strategy, create the initial database schema via EF Core migrations, and set up seed data for development.

**Acceptance Criteria (High-Level):**
- EF Core is configured with the Npgsql PostgreSQL provider
- Initial migration creates all required tables with proper constraints
- Database can be created/migrated via `dotnet ef database update`
- Foreign keys, indexes, and constraints are properly defined
- Development seed data populates the database with test records

---

#### Story 1.2.1: Configure EF Core with PostgreSQL Provider — `3 SP`
- [x] **COMPLETED**

**Description:** Install and configure Entity Framework Core with the Npgsql provider in `TodoApp.Infrastructure`. Create `ApplicationDbContext`, configure the connection string in appsettings, and register DbContext in DI.

**Acceptance Criteria:**
- `TodoApp.Infrastructure` references `Npgsql.EntityFrameworkCore.PostgreSQL`
- `ApplicationDbContext` inherits from `DbContext`
- Connection string is in `appsettings.Development.json` (not hardcoded)
- DbContext registered in DI with `AddDbContext<ApplicationDbContext>()`
- EF Core CLI tools (`dotnet ef`) can detect the DbContext
- `OnModelCreating` uses Fluent API entity configurations

---

#### Story 1.2.2: Create Domain Entities and EF Core Entity Configurations — `8 SP`
- [x] **COMPLETED**

**Description:** Create the core domain entities in `TodoApp.Domain` and corresponding `IEntityTypeConfiguration<T>` classes in `TodoApp.Infrastructure` defining table names, column types, constraints, indexes, and relationships.

**Acceptance Criteria:**
- **User** entity: `Id` (ULID), `Email` (string, unique), `PasswordHash` (string), `PasswordSalt` (string), `DisplayName` (string), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ)
- **TodoTask** entity: `Id` (ULID), `Name` (string, required, max 500), `Description` (string, nullable, max 4000), `DueDate` (DateTime, nullable), `Priority` (enum P1-P4, default P4), `Status` (enum Open/Done, default Open), `SystemList` (enum Inbox/Next/Upcoming/Someday, default Inbox), `SortOrder` (int), `ProjectId` (ULID, nullable FK), `UserId` (ULID, FK), `IsArchived` (bool, default false), `CompletedAt` (DateTime, nullable), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ)
- **Project** entity: `Id` (ULID), `Name` (string, required, max 100), `Description` (string, nullable, max 4000), `DueDate` (DateTime, nullable), `Status` (enum Active/Completed), `UserId` (ULID, FK), `SortOrder` (int), `CreatedAt` (TIMESTAMPTZ), `UpdatedAt` (TIMESTAMPTZ)
- **Label** entity: `Id` (ULID), `Name` (string, required, max 100, unique per user), `Color` (string, nullable), `UserId` (ULID, FK), `CreatedAt` (TIMESTAMPTZ)
- **TaskLabel** join entity: `TaskId` (ULID, FK), `LabelId` (ULID, FK), composite PK
- All entity configurations use Fluent API (no data annotations)
- Indexes on: `TodoTask.UserId`, `TodoTask.ProjectId`, `TodoTask.SystemList`, `TodoTask.Status`, `TodoTask.DueDate`, `Project.UserId`, `Label.UserId`, `User.Email` (unique)

---

#### Story 1.2.3: Create Initial Database Migration — `3 SP`
- [x] **COMPLETED**

**Description:** Generate the initial EF Core migration creating all tables, indexes, constraints, and relationships. Validate it applies cleanly to a fresh PostgreSQL database.

**Acceptance Criteria:**
- Migration generated via `dotnet ef migrations add InitialCreate`
- Creates tables: `Users`, `Tasks`, `Projects`, `Labels`, `TaskLabels`
- All FK relationships correctly established with appropriate cascade/restrict delete
- Migration applies with `dotnet ef database update`
- Migration rolls back with `dotnet ef database update 0`
- README documents how to run migrations

---

#### Story 1.2.4: Create Development Seed Data — `5 SP`
- [x] **COMPLETED**

**Description:** Create a data seeding mechanism populating the database with realistic test data in Development environment: sample users, tasks across all system lists, projects with tasks, and labels.

**Acceptance Criteria:**
- Seed data runs automatically on startup in Development environment only
- At least 2 test users with known credentials (documented in README)
- At least 15 tasks spanning all four system lists
- At least 2 projects, each with 3-5 tasks
- At least 4 labels assigned to various tasks
- Tasks have varying priorities and some have due dates
- Some tasks are Done/archived
- Seed data is idempotent (no duplicates on re-run)

---

### Feature 1.3: Frontend Project Scaffolding (React/Next.js)

**Description:** Initialize the Next.js frontend under `/src/frontend` with TypeScript, configure the build toolchain, set up API client utilities, and define folder structure conventions.

**Acceptance Criteria (High-Level):**
- A working Next.js application exists under `/src/frontend`
- TypeScript is configured with strict mode
- Application starts with `npm run dev`
- API client utility communicates with the backend
- Tailwind CSS for styling
- ESLint and Prettier configured

---

#### Story 1.3.1: Initialize Next.js Application with TypeScript — `5 SP`
- [x] **COMPLETED**

**Description:** Create a new Next.js application under `/src/frontend` with TypeScript, App Router, path aliases, folder structure, and Tailwind CSS.

**Acceptance Criteria:**
- Next.js app at `/src/frontend` with App Router enabled
- TypeScript `strict: true` in `tsconfig.json`
- Path alias `@/` maps to `src/`
- Folder structure: `src/app`, `src/components`, `src/hooks`, `src/services`, `src/types`, `src/utils`, `src/styles`
- Tailwind CSS installed and configured
- `npm run dev` renders a placeholder page
- `npm run build` completes without errors
- ESLint + Prettier configured

---

#### Story 1.3.2: Set Up API Client and Type Definitions — `5 SP`
- [x] **COMPLETED**

**Description:** Create a centralized API client module handling base URL, JWT bearer token injection, request/response interceptors, and error handling. Define TypeScript interfaces mirroring backend models.

**Acceptance Criteria:**
- API client at `src/services/apiClient.ts`
- Base URL configurable via `NEXT_PUBLIC_API_URL`
- Auto-attaches JWT token from localStorage to `Authorization` header
- Handles 401 responses by redirecting to login
- Transforms errors into consistent `ApiError` type
- TypeScript interfaces for: `User`, `Task`, `Project`, `Label`, `ApiError`
- Enums mirror backend: `Priority` (P1-P4), `TaskStatus` (Open/Done), `SystemList` (Inbox/Next/Upcoming/Someday), `ProjectStatus` (Active/Completed)
- All types exported from `src/types/index.ts`

---

#### Story 1.3.3: Configure Frontend Testing Framework — `3 SP`
- [x] **COMPLETED**

**Description:** Set up Jest with React Testing Library for unit and component testing. Create test utilities and verify with a sample test.

**Acceptance Criteria:**
- Jest configured for Next.js + TypeScript
- React Testing Library installed
- Path aliases resolve in test files
- Custom `renderWithProviders()` utility
- At least one passing component test
- Tests run via `npm test`
- Coverage reporting configured

---

### Feature 1.4: Development Environment & CI

**Description:** Containerize the dev environment with Docker Compose and set up a GitHub Actions CI pipeline.

**Acceptance Criteria (High-Level):**
- `docker-compose.yml` starts PostgreSQL, backend, and frontend
- CI pipeline runs on every PR to `main`
- CI validates: backend build/tests, frontend build/tests, linting
- README documents development setup

---

#### Story 1.4.1: Create Docker Compose Development Environment — `5 SP`
- [x] **COMPLETED**

**Description:** Create `docker-compose.yml` at the repo root orchestrating PostgreSQL, C# backend (hot-reload), and Next.js frontend (hot-reload). Include `.env.example`.

**Acceptance Criteria:**
- `docker-compose.yml` at repository root
- PostgreSQL service with persistent named volume
- Backend service with `dotnet watch` hot-reload
- Frontend service with hot-reload
- `.env.example` documents all required environment variables
- `docker-compose up` starts all services successfully
- Backend connects to PostgreSQL and runs migrations
- Frontend reaches the backend API

---

#### Story 1.4.2: Set Up CI Pipeline with GitHub Actions — `5 SP`
- [x] **COMPLETED**

**Description:** Create a GitHub Actions workflow that runs on PRs to `main`: backend build/test, frontend lint/build/test, with PostgreSQL service container.

**Acceptance Criteria:**
- `.github/workflows/ci.yml` exists
- Triggers on pull requests to `main`
- PostgreSQL service container for integration tests
- Backend steps: restore, build, unit test, integration test
- Frontend steps: install, lint, build, test
- Caching for NuGet packages and npm modules
- All steps must pass for success
- Target run time under 10 minutes

---

## Epic 2: User Authentication & Account Management

**Description:** Implement secure user registration, login, and session management using email/password authentication with salted hash password storage. Covers backend JWT auth system and frontend login/registration pages with protected route middleware.

**Business Value:** Authentication is the gateway to the entire application. Multi-user support requires secure identity management. Without this, no user-specific data can be stored correctly.

---

### Feature 2.1: Backend JWT Authentication with Salted Password Hashing

**Description:** Server-side auth: registration with validation, login with salted hash verification, JWT token generation/validation, and ASP.NET Core auth middleware.

**Acceptance Criteria (High-Level):**
- `POST /api/auth/register` registers users
- `POST /api/auth/login` authenticates and returns JWT
- Passwords stored as salted hashes
- JWT tokens include user ID and email claims
- Protected endpoints reject requests without valid JWT
- Duplicate email returns clear error

---

#### Story 2.1.1: Implement Password Hashing Service — `3 SP`
- [x] **COMPLETED**

**Description:** Create a `PasswordHashingService` with methods to hash a password (generating random salt) and verify a password against stored hash/salt. Use PBKDF2 with HMAC-SHA256 (100k+ iterations) or bcrypt.

**Acceptance Criteria:**
- `IPasswordHashingService` with `HashPassword(string password)` → `(string hash, string salt)` and `VerifyPassword(string password, string hash, string salt)` → `bool`
- Uses PBKDF2 with HMAC-SHA256 (min 100k iterations) or bcrypt (cost >= 12)
- Each `HashPassword` generates a unique cryptographically random salt
- Same password hashed twice produces different hashes
- Unit tests verify correct hashing, verification, and wrong-password rejection

---

#### Story 2.1.2: Implement JWT Token Service — `5 SP`
- [x] **COMPLETED**

**Description:** Create a `JwtTokenService` that generates signed JWT tokens and validates them. Token contains user ID, email, expiration. Signing key, issuer, audience, expiration configurable via appsettings. Integrates with ASP.NET Core auth middleware.

**Acceptance Criteria:**
- `IJwtTokenService` with `GenerateToken(User user)` → JWT string and `ValidateToken(string token)` → claims or null
- Claims: `sub` (user ID), `email`, `iat`, `exp`
- HMAC-SHA256 signing with configurable secret key (min 256 bits)
- Configurable expiration (default: 24 hours)
- ASP.NET Core `AddAuthentication().AddJwtBearer()` configured
- `[Authorize]` correctly rejects unauthenticated requests
- Unit tests for token generation, claims, and expired token rejection

---

#### Story 2.1.3: Implement User Registration Endpoint — `5 SP`
- [x] **COMPLETED**

**Description:** `POST /api/auth/register` accepting email, password, display name. Validates input, checks duplicate emails, hashes password, creates user, returns JWT for immediate login.

**Acceptance Criteria:**
- Accepts `{ email, password, displayName }`
- Returns `201 Created` with `{ token, user: { id, email, displayName } }`
- Returns `400` for: missing fields, invalid email, password < 8 chars, password missing uppercase/lowercase/digit
- Returns `409` for duplicate email
- Password stored as salted hash
- Email stored lowercase (case-insensitive)
- Integration tests for success, duplicate email, validation errors

---

#### Story 2.1.4: Implement User Login Endpoint — `5 SP`
- [x] **COMPLETED**

**Description:** `POST /api/auth/login` accepting email and password, verifying against stored salted hash, returning JWT. Generic error message prevents user enumeration.

**Acceptance Criteria:**
- Accepts `{ email, password }`
- Returns `200 OK` with `{ token, user: { id, email, displayName } }`
- Returns `401` with "Invalid email or password" for both wrong email and wrong password
- Case-insensitive email matching
- Integration tests for: correct creds, wrong password, non-existent email, token validity

---

#### Story 2.1.5: Implement Current User Endpoint — `2 SP`
- [x] **COMPLETED**

**Description:** `GET /api/auth/me` protected endpoint returning the current user's profile based on JWT claims.

**Acceptance Criteria:**
- Requires authentication (401 without valid token)
- Returns `200 OK` with `{ id, email, displayName, createdAt }`
- Returns `404` if user ID from token no longer exists
- Integration tests for correct data and 401 without token

---

### Feature 2.2: Frontend Authentication Pages and Auth State Management

**Description:** Registration page, login page, auth state management (React context), protected route middleware, JWT token storage.

**Acceptance Criteria (High-Level):**
- Registration page for new signups
- Login page for existing users
- Auth state persists across reloads (localStorage)
- Unauthenticated users redirected to login
- Authenticated users redirected away from login/register
- Logout clears state and redirects

---

#### Story 2.2.1: Build Registration Page — `5 SP`
- [x] **COMPLETED**

**Description:** Registration page at `/register` with display name, email, password, confirm password fields. Client-side validation matching backend rules. On success, stores JWT and redirects to app.

**Acceptance Criteria:**
- Accessible at `/register`
- Fields: Display Name, Email, Password, Confirm Password
- Client-side validation: required fields, email format, password strength, passwords match
- Validation errors inline below each field
- Loading state on submit button
- On success: token stored, redirect to `/`
- On API error (duplicate email): error displayed in form
- Link to login page visible

---

#### Story 2.2.2: Build Login Page — `3 SP`
- [x] **COMPLETED**

**Description:** Login page at `/login` with email and password fields. Stores JWT and redirects on success. Generic error on auth failure.

**Acceptance Criteria:**
- Accessible at `/login`
- Fields: Email, Password
- Client-side validation: required fields, email format
- Loading state on submit
- On success: token stored, redirect to `/`
- On 401: "Invalid email or password"
- Link to registration page visible
- Visually consistent with registration page

---

#### Story 2.2.3: Implement Auth State Management and Protected Routes — `8 SP`
- [x] **COMPLETED**

**Description:** React context for auth state (current user, login/logout, loading). Next.js middleware protecting routes. Token validation on app load via `GET /api/auth/me`.

**Acceptance Criteria:**
- `AuthContext` provides: `user`, `isAuthenticated`, `isLoading`, `login(token)`, `logout()`
- JWT stored in localStorage
- On app load with token: calls `GET /api/auth/me` to validate and populate user state
- Invalid/expired token: cleared, redirect to `/login`
- Middleware redirects unauthenticated users to `/login` (except `/login` and `/register`)
- Authenticated users on `/login` or `/register` redirected to `/`
- `logout()` clears token, resets state, redirects to `/login`
- Loading state shown while auth is being determined

---

## Epic 3: Application Shell & Navigation

**Description:** Build the main application layout: sidebar navigation, content area, responsive design. The shell frames all task, project, and label views with Todoist-like design.

**Business Value:** The shell is the user's primary interface. A clean, intuitive navigation structure impacts usability, discoverability, and satisfaction. The sidebar-driven layout is a proven pattern for productivity tools.

---

### Feature 3.1: Application Shell with Sidebar and Content Area

**Description:** Primary layout with collapsible sidebar (left) and main content area (right). Sidebar contains system lists, projects, and labels navigation. Responsive — collapses on mobile.

**Acceptance Criteria (High-Level):**
- Sidebar + content area layout
- Sidebar shows system lists, projects, labels navigation
- Responsive (sidebar collapses on mobile)
- Active navigation item highlighted
- Sidebar collapse/expand toggle

---

#### Story 3.1.1: Build Application Shell Layout Component — `5 SP`
- [x] **COMPLETED**

**Description:** Root layout dividing screen into fixed-width sidebar (~280px) and flexible content area. Sidebar has header (logo/user), scrollable navigation, and footer. Content area has header bar and scrollable content.

**Acceptance Criteria:**
- Sidebar (280px) and content area (remaining width)
- Sidebar: header (logo/user), navigation (scrollable), footer
- Content area: header bar, scrollable content
- Full viewport height (no page-level scrolling)
- Todoist-like styling: white/light gray, subtle borders, clean typography
- Shell wraps all authenticated pages via Next.js layout

---

#### Story 3.1.2: Implement Sidebar Navigation with System Lists — `5 SP`
- [x] **COMPLETED**

**Description:** Populate sidebar with Inbox, Next, Upcoming, Someday. Each shows icon, name, and open task count badge. Active list highlighted. Click navigates to list view.

**Acceptance Criteria:**
- Four system list items: Inbox, Next, Upcoming, Someday
- Distinct icon per list
- Count badge showing open (non-archived) tasks
- Active list visually highlighted
- Click navigates to corresponding route
- Counts update reactively on task changes

---

#### Story 3.1.3: Implement Responsive Sidebar Behavior — `5 SP`
- [x] **COMPLETED**

**Description:** Collapsible sidebar: desktop shows full or icon-only strip; mobile shows slide-in overlay with hamburger toggle.

**Acceptance Criteria:**
- Desktop (>= 1024px): visible by default; collapse button reduces to icon-only (~48px)
- Mobile (< 1024px): hidden by default; hamburger button toggles slide-in overlay with backdrop
- Backdrop click or nav item click closes mobile sidebar
- Collapse state persisted in localStorage (desktop)
- Smooth CSS transitions (~200ms)

---

#### Story 3.1.4: Add User Profile Menu to Sidebar — `3 SP`
- [x] **COMPLETED**

**Description:** Show user display name and email in sidebar header. Dropdown menu with "Log out" option.

**Acceptance Criteria:**
- Display name and email shown (truncated if long)
- Colored avatar circle with user initials
- Click opens dropdown menu
- "Log out" option clears auth state and redirects to `/login`
- Dropdown closes on outside click

---

## Epic 4: Task Management (Core)

**Description:** Complete task lifecycle: create, view, edit, complete (soft-delete/archive), delete (hard-delete), manual sort order via drag-and-drop. Tasks have name, description, due date, priority (P1-P4), status (Open/Done), system list, optional project, and labels.

**Business Value:** Task management is the fundamental feature. Every user interaction revolves around tasks. Quality and usability here determines adoption vs. abandonment. Manual sort order and drag-and-drop give users control essential for GTD methodology.

---

### Feature 4.1: Task CRUD API Endpoints

**Description:** Full REST API for tasks scoped to authenticated user. Create (defaulting to Inbox), retrieve with filtering/sorting, update attributes, complete (archive), reopen, delete (permanent), and reorder.

**Acceptance Criteria (High-Level):**
- `POST /api/tasks` creates task defaulting to Inbox
- `GET /api/tasks` returns tasks with filtering by system list, project, label, status
- `GET /api/tasks/{id}` returns single task with full details
- `PUT /api/tasks/{id}` updates task attributes
- `PATCH /api/tasks/{id}/complete` marks Done and archives
- `PATCH /api/tasks/{id}/reopen` restores to Open
- `DELETE /api/tasks/{id}` permanently deletes
- `PATCH /api/tasks/reorder` updates sort order
- All endpoints scoped to authenticated user

---

#### Story 4.1.1: Implement Create Task Endpoint — `5 SP`
- [x] **COMPLETED**

**Description:** `POST /api/tasks` creates a task for the authenticated user. Accepts name (required), optionally description, due date, priority, system list, project ID. Defaults: Inbox, P4, Open. Sort order places new task at top.

**Acceptance Criteria:**
- Accepts `{ name, description?, dueDate?, priority?, systemList?, projectId? }`
- Returns `201 Created` with full task including generated ID and timestamps
- `name` required (400 if missing), max 500 chars; `description` max 4000 chars
- Defaults: `status = Open`, `systemList = Inbox`, `priority = P4`, `isArchived = false`
- `sortOrder` positions new task at top of target list
- `projectId` if provided must reference existing user-owned project (400 otherwise)
- Task associated with authenticated user from JWT
- Integration tests: creation, defaults, validation, invalid project ID

---

#### Story 4.1.2: Implement Get Tasks Endpoint with Filtering — `8 SP`

**Description:** `GET /api/tasks` returns authenticated user's tasks with filtering by system list, project ID, label ID, status, and archived flag. Sorted by sort order. Includes associated labels and project name.

**Acceptance Criteria:**
- Returns all non-archived, open tasks by default
- Query params: `systemList`, `projectId`, `labelId`, `status` (Open/Done/All), `archived` (bool, default false)
- Sorted by `sortOrder` ascending
- Each task includes label names/IDs and project name
- `archived=true` returns completed tasks sorted by `completedAt` desc
- Filters combinable
- Only user's own tasks
- Returns `200 OK` with array (empty if none)
- Integration tests for default, each filter, combined, archived

---

#### Story 4.1.3: Implement Get Single Task Endpoint — `2 SP`

**Description:** `GET /api/tasks/{id}` returns full task details with labels and project info.

**Acceptance Criteria:**
- Returns `200 OK` with full task, labels, project info
- `404` if not found or belongs to another user
- `400` if ID not valid GUID
- Integration tests for success and 404 for another user's task

---

#### Story 4.1.4: Implement Update Task Endpoint — `5 SP`

**Description:** `PUT /api/tasks/{id}` updates any mutable attribute (partial updates). Refreshes `updatedAt`.

**Acceptance Criteria:**
- Accepts subset of `{ name, description, dueDate, priority, systemList, projectId }`
- Only provided fields updated; omitted fields unchanged
- `updatedAt` refreshed on every update
- Validation: name max 500, description max 4000, valid enums
- `projectId = null` disassociates; valid GUID must reference user's project
- Returns `200 OK` with updated task; `404` if not found; `400` for validation
- Integration tests: each field, clearing optional fields, validation

---

#### Story 4.1.5: Implement Complete and Reopen Task Endpoints — `5 SP`

**Description:** `PATCH /api/tasks/{id}/complete` sets Done + archived + completedAt. `PATCH /api/tasks/{id}/reopen` reverses: Open + unarchived + clears completedAt, task goes back to original list at top.

**Acceptance Criteria:**
- Complete: `status = Done`, `isArchived = true`, `completedAt = now`; 200 OK; idempotent
- Reopen: `status = Open`, `isArchived = false`, `completedAt = null`; retains original `systemList`; `sortOrder` at top; 200 OK
- Both return 404 if not found or wrong user
- Integration tests verify both flows and archive query

---

#### Story 4.1.6: Implement Delete Task Endpoint — `3 SP`

**Description:** `DELETE /api/tasks/{id}` permanently hard-deletes task and its label associations.

**Acceptance Criteria:**
- Permanently removes task from database
- Associated `TaskLabel` records also deleted
- Returns `204 No Content`; `404` if not found or wrong user
- Task no longer appears in any query
- Integration tests: deletion, subsequent GET returns 404

---

#### Story 4.1.7: Implement Task Sort Order Endpoint — `5 SP`

**Description:** `PATCH /api/tasks/reorder` accepts ordered array of task IDs and updates sort order values. Scoped to same system list, atomic transaction.

**Acceptance Criteria:**
- Accepts `{ taskIds: [guid1, guid2, ...], systemList: "Inbox" }`
- Updates `sortOrder` to match array position (index 0 = order 0)
- All tasks must belong to authenticated user (400 otherwise)
- All tasks must belong to specified system list (400 otherwise)
- Atomic transaction (all or nothing)
- Returns `200 OK`
- Integration tests: correct order, authorization check

---

### Feature 4.2: Task List View with Inline Actions

**Description:** Frontend task list displaying tasks in sort order with inline actions. Each row shows name, priority indicator, due date, project, labels. Complete with checkbox click. Priority colors.

**Acceptance Criteria (High-Level):**
- Tasks displayed in sorted vertical list
- Key info visible at a glance per row
- Checkbox completion
- Priority color differentiation
- Due dates with relative time and overdue highlighting
- Reactive updates

---

#### Story 4.2.1: Build Task List Component — `8 SP`

**Description:** `TaskList` component rendering ordered task rows. Each row: completion checkbox, name, priority color (P1=red, P2=orange, P3=blue, P4=gray), due date (relative: Today/Tomorrow/etc.), project chip, label chips. Empty state. Click opens detail view. Loading skeleton.

**Acceptance Criteria:**
- Vertical list of `TaskRow` components
- Each row: checkbox, name, priority color, due date, project name, labels
- Priority colors: P1=#ff4440, P2=#ff9933, P3=#4073ff, P4=gray
- Due dates: "Today", "Tomorrow", "Yesterday", or formatted date
- Overdue dates in red
- Project name as subtle chip if assigned
- Labels as small colored chips
- Empty state: "No tasks here. Enjoy your free time!"
- Click on row (except checkbox) opens task detail
- Loading skeleton placeholders

---

#### Story 4.2.2: Implement Task Completion Toggle — `5 SP`

**Description:** Checkbox click triggers completion animation, calls complete API, task fades out after delay. Undo toast during delay allows reversal.

**Acceptance Criteria:**
- Checkbox click initiates completion
- Immediate visual: checkbox fills, text strikethrough
- 1.5s delay, then fade+slide animation out
- Toast: "Task completed" with "Undo" button (auto-dismiss after 5s)
- Undo reverts visual and calls reopen endpoint
- API failure reverts visual with error toast
- Optimistic UI

---

#### Story 4.2.3: Implement Drag-and-Drop Task Reordering — `8 SP`

**Description:** Drag-and-drop with `@dnd-kit/core` or similar. Drag handle, visual feedback, optimistic reorder, API call on drop.

**Acceptance Criteria:**
- Drag handle (grip dots) on each row
- Visual drag representation
- Drop indicator (line/gap)
- Smooth reorder animation during drag
- Optimistic order update on drop; revert on API failure
- Works within single system list only
- Disabled on mobile (or long-press activated)

---

### Feature 4.3: Task Creation Interface

**Description:** Two creation modes: quick-add inline input (just name, Enter to submit) and full form modal (all attributes). Quick-add respects current context.

**Acceptance Criteria (High-Level):**
- Quick-add for rapid entry (name only)
- Full form for all attributes
- Optimistic UI for new tasks
- Context-aware (current list/project)

---

#### Story 4.3.1: Build Quick-Add Task Input — `5 SP`

**Description:** Inline input at top of every task list. "+" icon, placeholder "Add a task...". Enter creates task with current list context. Optimistic UI. Input stays focused for rapid entry.

**Acceptance Criteria:**
- Visible at top of every task list view
- "+" icon and "Add a task..." placeholder
- Enter creates task via `POST /api/tasks`
- Inherits current list's systemList; if in project, assigns projectId too
- New task at top of list (optimistic)
- Input clears and stays focused after submission
- Empty Enter does nothing
- API failure removes optimistic task with error toast

---

#### Story 4.3.2: Build Full Task Creation Form — `8 SP`

**Description:** Modal dialog with all attribute fields: name, description, due date picker, priority selector, system list dropdown, project dropdown, label multi-select. Opens via button or "Q" keyboard shortcut.

**Acceptance Criteria:**
- Modal dialog with clean design
- Fields: Name (required), Description (textarea), Due Date (date picker), Priority (P1-P4 with colors), System List (dropdown), Project (dropdown, optional), Labels (multi-select)
- Defaults: System List = current context or Inbox, Priority = P4
- Validation: name required, max lengths
- Submit creates task and closes modal
- Cancel closes without action
- "Q" shortcut opens modal from anywhere
- Loading state prevents double submission

---

### Feature 4.4: Task Detail View and Inline Editing

**Description:** Side panel showing all task attributes with inline editing. Opens on task row click. Auto-saves changes (debounced). Delete with confirmation.

**Acceptance Criteria (High-Level):**
- Click opens detail side panel
- All attributes editable inline
- Debounced auto-save
- Delete with confirmation

---

#### Story 4.4.1: Build Task Detail Side Panel — `8 SP`

**Description:** Slide-in panel from right (~400px) showing all task attributes. Editable name heading, description textarea, properties section (due date, priority, system list, project, labels), metadata (created, updated, completed dates). Close via X, Escape, or outside click.

**Acceptance Criteria:**
- Right slide-in panel (~400px wide)
- All attributes in organized layout
- Name as large editable text field
- Description as editable textarea
- Properties section: due date, priority, system list, project, labels
- Metadata: created, updated, completed dates
- Close via X button, Escape key, or outside click
- Loads from `GET /api/tasks/{id}` on open

---

#### Story 4.4.2: Implement Inline Editing in Task Detail Panel — `8 SP`

**Description:** Each attribute editable inline: name (click to edit, save on blur/Enter), description (click to edit, save on blur), due date (date picker), priority/system list/project (dropdowns), labels (multi-select). Auto-save via `PUT /api/tasks/{id}`.

**Acceptance Criteria:**
- Name: click to edit, auto-save on blur/Enter, Escape cancels
- Description: click to edit, auto-save on blur, multi-line
- Due date: click opens date picker, save on select, "Clear" option
- Priority: P1-P4 selector with colors, save immediately
- System list: dropdown, save immediately (moves task)
- Project: selector dropdown with "None" option, save immediately
- Labels: multi-select, toggle saves immediately
- All saves via `PUT /api/tasks/{id}` with only changed field
- "Saved" indicator on success; revert + error toast on failure

---

#### Story 4.4.3: Add Delete Task Functionality — `3 SP`

**Description:** Delete button in detail panel opens confirmation dialog ("permanent, cannot be undone"). On confirm, hard-delete via API, panel closes, task removed from list.

**Acceptance Criteria:**
- "Delete task" button/menu in detail panel
- Confirmation: "Delete task? This action cannot be undone."
- "Delete" (red) and "Cancel" buttons
- Confirm: API delete, panel closes, task removed
- Cancel: dialog closes
- Failure: error toast
- Task gone from all lists/queries

---

### Feature 4.5: Completed Task Archive View

**Description:** View for soft-deleted (completed) tasks. Shows completion date. Users can reopen tasks to restore them.

**Acceptance Criteria (High-Level):**
- View completed/archived tasks
- Shows completion date
- Can reopen archived tasks
- Excluded from active list views

---

#### Story 4.5.1: Build Completed Tasks Archive View — `5 SP`

**Description:** Archive view accessible from sidebar. Shows completed tasks sorted by completion date (newest first). Each row: name (strikethrough), completion date, original list. Click opens detail panel with "Reopen" button.

**Acceptance Criteria:**
- Accessible via sidebar link or "Show completed" button
- Fetched from `GET /api/tasks?archived=true`
- Sorted by `completedAt` descending
- Each row: name (strikethrough), completion date ("Completed 2 days ago"), original list
- Click opens detail panel
- Detail panel shows "Reopen" button instead of checkbox
- Reopen calls `PATCH /api/tasks/{id}/reopen`, removes from archive, restores to original list
- Empty state: "No completed tasks yet"

---

## Epic 5: System Lists (GTD Framework)

**Description:** Implement the four GTD system lists as first-class navigation destinations. Inbox is default entry point. Next is manually curated. Upcoming is date-driven. Someday is for deferred items. Manual movement between lists.

**Business Value:** The GTD methodology is the core workflow philosophy. System lists differentiate this from a simple to-do app. Inbox ensures nothing is lost, Next provides focus, Upcoming provides time-awareness, Someday prevents clutter.

---

### Feature 5.1: Inbox - Default Task Entry Point

**Description:** Inbox list view as default landing page. All new tasks without explicit list go here. GTD "capture" bucket for triage.

**Acceptance Criteria (High-Level):**
- Default view after login (home page)
- All new tasks without explicit list go to Inbox
- Tasks movable to other lists
- Sidebar badge reflects count

---

#### Story 5.1.1: Build Inbox List View Page — `3 SP`

**Description:** Inbox page at root route displaying tasks with `systemList = Inbox`. Uses `TaskList` component with quick-add, completion, drag-to-reorder.

**Acceptance Criteria:**
- Rendered at `/` (or `/inbox`)
- Title: "Inbox" with open task count
- Fetches `GET /api/tasks?systemList=Inbox`
- Uses `TaskList` component with all interactions
- Quick-add defaults to Inbox
- Empty state: "Your Inbox is clear! Nice work."
- Sidebar Inbox item highlighted
- Loading skeleton while fetching

---

### Feature 5.2: Next - Manually Curated Focus List

**Description:** Next list for tasks the user has decided to work on soon. Manually curated.

**Acceptance Criteria (High-Level):**
- Shows tasks with `systemList = Next`
- Can add directly or move from other lists
- Drag-and-drop reordering

---

#### Story 5.2.1: Build Next List View Page — `2 SP`

**Description:** Next page at `/next` displaying tasks with `systemList = Next`. Same structure as Inbox with different messaging.

**Acceptance Criteria:**
- Rendered at `/next`
- Title: "Next" with open task count
- Fetches `GET /api/tasks?systemList=Next`
- Uses `TaskList` component with all interactions
- Quick-add creates tasks with `systemList = Next`
- Empty state: "What will you work on next? Move tasks here from Inbox."
- Sidebar Next item highlighted

---

### Feature 5.3: Upcoming - Date-Driven Schedule View

**Description:** Upcoming view automatically showing tasks with near-future due dates from ANY list, plus manually assigned Upcoming tasks. Grouped by date with overdue highlighting.

**Acceptance Criteria (High-Level):**
- Shows tasks with due dates in next 14 days regardless of list
- Tasks grouped by date
- Overdue tasks prominently displayed
- Manual Upcoming list assignment also supported

---

#### Story 5.3.1: Implement Upcoming Tasks Backend Query — `5 SP`

**Description:** Specialized query returning tasks with due dates within 14 days OR tasks with `systemList = Upcoming`. Overdue first, then by due date ascending.

**Acceptance Criteria:**
- `GET /api/tasks?view=upcoming` returns tasks matching:
  - Due date within next 14 days (any system list), OR
  - `systemList = Upcoming` (regardless of due date)
- Overdue tasks included, sorted before future tasks
- Sort: overdue first (oldest first), then due date ascending
- Only non-archived, open tasks
- Results include task's actual `systemList` value
- Integration tests: date-range filtering, explicit Upcoming list, overdue ordering

---

#### Story 5.3.2: Build Upcoming List View Page with Date Grouping — `5 SP`

**Description:** Upcoming page with tasks grouped by date: "Overdue" (red), "Today", "Tomorrow", specific dates. Tasks within groups sorted by priority. "No date" section for dateless Upcoming-list tasks.

**Acceptance Criteria:**
- Rendered at `/upcoming`
- Title: "Upcoming"
- Tasks grouped with headers: "Overdue" (red), "Today", "Tomorrow", then dates
- Within groups: sorted by priority (P1 first)
- "Overdue" only appears if overdue tasks exist
- "No date" section for dateless Upcoming-list tasks
- Each row shows system list badge (cross-list view)
- Quick-add defaults to today's date + `systemList = Upcoming`
- Empty state: "No upcoming tasks. You're all caught up!"

---

### Feature 5.4: Someday - Deferred Tasks

**Description:** Someday list for "maybe later" tasks. GTD Someday/Maybe list.

**Acceptance Criteria (High-Level):**
- Shows tasks with `systemList = Someday`
- Move tasks here from other lists
- Promote back to Next or Inbox when ready

---

#### Story 5.4.1: Build Someday List View Page — `2 SP`

**Description:** Someday page at `/someday`. Same structure as Inbox/Next with deferred-item messaging.

**Acceptance Criteria:**
- Rendered at `/someday`
- Title: "Someday" with open task count
- Fetches `GET /api/tasks?systemList=Someday`
- Uses `TaskList` component with all interactions
- Quick-add creates tasks with `systemList = Someday`
- Empty state: "Nothing on the back burner. Add tasks you might want to do someday."
- Sidebar Someday item highlighted

---

### Feature 5.5: Task Movement Between System Lists

**Description:** Move tasks between lists via detail panel dropdown and right-click context menu. Moved task disappears from source, appears at top of destination.

**Acceptance Criteria (High-Level):**
- Change list from detail panel
- Right-click context menu with "Move to" submenu
- Task disappears from source, appears at top of destination
- Sort order updated; sidebar counts updated

---

#### Story 5.5.1: Implement Task Move via Detail Panel and Context Menu — `8 SP`

**Description:** System list selector in detail panel (saves via PUT). Right-click context menu: "Move to" > lists (excluding current), "Complete", "Delete", "Edit". Smooth animations, sidebar count updates, success toast.

**Acceptance Criteria:**
- Detail panel system list selector changes task's list via `PUT /api/tasks/{id}`
- Right-click context menu: "Move to" > Inbox/Next/Upcoming/Someday (excluding current), "Complete", "Delete" (with confirm), "Edit"
- Move updates `systemList` via `PUT /api/tasks/{id}`
- Moved task animates out of current view
- Appears at top of destination list
- Sidebar badge counts update
- Toast: "Task moved to {destination}"

---

## Epic 6: Project Management

**Description:** Projects group related tasks toward a goal. Tasks belong to both a system list and a project (dual organization). Project view shows tasks grouped by system list. Manual completion. Progress tracking.

**Business Value:** Projects organize multi-step goals. Without them, users are limited to flat lists. The dual-organization model preserves GTD workflow while adding project grouping. Progress visibility provides motivation.

---

### Feature 6.1: Project CRUD API Endpoints

**Description:** REST API for projects: create, list (with task stats), get single, update, complete/reopen, delete (orphan tasks, don't delete them).

**Acceptance Criteria (High-Level):**
- `POST /api/projects` creates project
- `GET /api/projects` lists with task statistics
- `GET /api/projects/{id}` returns project with stats
- `PUT /api/projects/{id}` updates attributes
- `PATCH /api/projects/{id}/complete` and `/reopen`
- `DELETE /api/projects/{id}` deletes project, orphans tasks

---

#### Story 6.1.1: Implement Create and List Projects Endpoints — `5 SP`

**Description:** `POST /api/projects` for creation; `GET /api/projects` listing all user projects with computed stats (total tasks, completed tasks, percentage). Sorted by sort order.

**Acceptance Criteria:**
- `POST /api/projects` accepts `{ name, description?, dueDate? }`; returns `201 Created`
- `name` required (max 200 chars); defaults: `status = Active`, `sortOrder` = top
- `GET /api/projects` returns all user projects
- Each includes: all fields + `totalTaskCount`, `completedTaskCount`, `completionPercentage` (0-100)
- Sorted by `sortOrder` ascending; only user's projects
- Integration tests for creation, listing, stats

---

#### Story 6.1.2: Implement Get, Update, Complete, and Delete Project Endpoints — `5 SP`

**Description:** Single project retrieval with stats, update, complete/reopen (doesn't affect tasks), delete (orphans tasks by setting projectId to null).

**Acceptance Criteria:**
- `GET /api/projects/{id}` returns project with stats; 404 if not found or wrong user
- `PUT /api/projects/{id}` updates name, description, due date; returns 200
- `PATCH /api/projects/{id}/complete` sets `status = Completed`; does NOT complete tasks
- `PATCH /api/projects/{id}/reopen` sets `status = Active`
- `DELETE /api/projects/{id}` deletes project; tasks get `projectId = null`
- 404 for non-existent or wrong-user projects
- Integration tests for all operations

---

### Feature 6.2: Project Navigation, View, and Management UI

**Description:** Sidebar project list, project detail view (tasks grouped by system list), create/edit forms, progress visualization.

**Acceptance Criteria (High-Level):**
- Projects in sidebar below system lists
- Click shows tasks grouped by system list
- Create, edit, delete projects
- Completion progress displayed

---

#### Story 6.2.1: Add Project List to Sidebar Navigation — `5 SP`

**Description:** "Projects" section in sidebar with each project as nav item (name + open task count badge). "+" button for adding. Active projects first, completed in collapsible section.

**Acceptance Criteria:**
- "Projects" section header with "+" button
- Each active project: name, open task count badge
- Click navigates to `/projects/{id}`
- Active projects sorted by `sortOrder`
- Completed projects in collapsible "Completed" section (collapsed by default)
- No projects: "Create your first project" message
- Current project highlighted
- List refreshes on create/complete/delete

---

#### Story 6.2.2: Build Project Detail View with Task Grouping — `8 SP`

**Description:** Project page at `/projects/{id}` with header (name, description, due date, progress bar), tasks grouped by system list sections (Inbox/Next/Upcoming/Someday). Quick-add assigns to project + Inbox.

**Acceptance Criteria:**
- Rendered at `/projects/{id}`
- Header: project name (editable), description (editable), due date, progress bar with "4 of 10 tasks done - 40%"
- Tasks from `GET /api/tasks?projectId={id}` grouped by Inbox/Next/Upcoming/Someday
- Each section has header (e.g., "Next (3)"), collapsible
- Empty sections hidden
- Uses `TaskList` component with all interactions
- Quick-add creates with `projectId` and `systemList = Inbox`
- "Complete project" button with confirmation
- 404 for invalid projects

---

#### Story 6.2.3: Build Project Create and Edit Modal — `5 SP`

**Description:** Modal for creating (from sidebar "+") and editing projects. Fields: name (required), description, due date.

**Acceptance Criteria:**
- "Add project" modal from sidebar "+"
- Fields: Name (required, max 200), Description (textarea), Due Date (date picker)
- Create submits to `POST /api/projects`, adds to sidebar
- Edit modal from project page header menu, pre-populated, submits to `PUT`
- Cancel closes without saving
- Validation errors inline
- Success: modal closes, sidebar/page updates

---

#### Story 6.2.4: Implement Project Deletion with Confirmation — `3 SP`

**Description:** "Delete project" in page header menu. Confirmation dialog explains tasks remain but lose project association. Redirect to Inbox.

**Acceptance Criteria:**
- "Delete project" in header menu
- Dialog: "This will permanently delete '{name}'. Tasks will not be deleted but will no longer belong to any project."
- "Delete" (red) and "Cancel" buttons
- Confirm: API delete, sidebar updates, redirect to `/inbox`
- Cancel: dialog closes
- Failure: error toast

---

## Epic 7: Labels & Tagging

**Description:** User-created labels for cross-cutting task categorization. Labels have name and optional color. Filter tasks by label. Labels in sidebar, clicking one shows all matching tasks.

**Business Value:** Labels provide flexible, user-defined organization complementing fixed system lists and projects. While lists = workflow state and projects = goals, labels = contexts/categories (e.g., @work, @home). This accommodates diverse workflows without structural changes.

---

### Feature 7.1: Label CRUD API Endpoints

**Description:** REST API for labels: create, list (with task counts), update, delete (cascade remove from tasks). Assign/remove labels from tasks.

**Acceptance Criteria (High-Level):**
- `POST /api/labels` creates label
- `GET /api/labels` returns all with task counts
- `PUT /api/labels/{id}` updates name/color
- `DELETE /api/labels/{id}` deletes and removes from tasks
- Assign/remove labels from tasks

---

#### Story 7.1.1: Implement Label CRUD Endpoints — `5 SP`

**Description:** Full CRUD: create, list (with task counts), update, delete (cascade). Name unique per user (case-insensitive). Color optional (hex).

**Acceptance Criteria:**
- `POST /api/labels` accepts `{ name, color? }`; returns `201 Created`
- `name` required, max 100 chars; unique per user case-insensitive (409 if duplicate)
- `color` optional; valid hex (e.g., "#ff4040")
- `GET /api/labels` returns all with `taskCount` of open tasks, sorted alphabetically
- `PUT /api/labels/{id}` updates name/color; uniqueness applies
- `DELETE /api/labels/{id}` removes label + all `TaskLabel` records
- 404 for non-existent or wrong-user labels
- Integration tests for all CRUD and constraints

---

#### Story 7.1.2: Implement Task-Label Assignment Endpoints — `3 SP`

**Description:** `POST /api/tasks/{taskId}/labels/{labelId}` assigns label; `DELETE` removes. Idempotent. Both task and label must belong to user.

**Acceptance Criteria:**
- `POST /api/tasks/{taskId}/labels/{labelId}` creates `TaskLabel`; returns updated task with labels
- Idempotent (duplicate assign = success)
- Both task and label must belong to user (404 otherwise)
- `DELETE /api/tasks/{taskId}/labels/{labelId}` removes; returns updated task
- Idempotent (removing unassigned = success)
- Integration tests for assignment, removal, idempotency

---

### Feature 7.2: Label Navigation, Filtering, and Management UI

**Description:** Sidebar label list, label-filtered task views, label management UI, label selection in task forms.

**Acceptance Criteria (High-Level):**
- Labels in sidebar below projects
- Click label shows all matching tasks
- Create, edit, delete labels
- Label selection in task create/edit

---

#### Story 7.2.1: Add Label List to Sidebar Navigation — `3 SP`

**Description:** "Labels" section in sidebar. Each label: color dot, name, open task count. "+' button. Click navigates to filtered view.

**Acceptance Criteria:**
- "Labels" section header with "+" button
- Each label: colored dot, name, task count badge
- Sorted alphabetically
- Click navigates to `/labels/{id}`
- Current label highlighted
- No labels: "Create your first label" message
- List refreshes on create/edit/delete

---

#### Story 7.2.2: Build Label-Filtered Task View — `5 SP`

**Description:** Page at `/labels/{id}` showing all open, non-archived tasks with specified label, grouped by system list. Shows project name per task. Quick-add auto-assigns label.

**Acceptance Criteria:**
- Rendered at `/labels/{id}`
- Header: label name with color
- Tasks from `GET /api/tasks?labelId={id}` grouped by system list
- Each task shows project name chip if assigned
- Standard interactions (complete, edit, reorder)
- Quick-add creates in Inbox with current label auto-assigned
- Empty state: "No tasks with this label"
- 404 for invalid label

---

#### Story 7.2.3: Build Label Create/Edit/Delete UI — `5 SP`

**Description:** Modal for creating/editing labels. Fields: name, color picker. Delete with confirmation in edit modal. Ensure label multi-select integrated in task forms.

**Acceptance Criteria:**
- "Add label" modal from sidebar "+"
- Fields: Name (required, max 100), Color (picker with presets + custom hex)
- Create → `POST /api/labels`, adds to sidebar
- Edit modal from label sidebar context menu or label view header
- Pre-populates; submits to `PUT /api/labels/{id}`
- Duplicate name: inline error
- Delete in edit modal → confirmation: "Delete '{name}'? Removes from all tasks."
- Confirm: API delete, sidebar updates, redirect to Inbox
- Cancel closes without saving

---

## Backlog Summary

### Story Point Totals by Epic

| Epic | Stories | Story Points |
|------|---------|-------------|
| 1. Project Infrastructure & Setup | 12 | 62 |
| 2. User Authentication & Account Management | 8 | 36 |
| 3. Application Shell & Navigation | 4 | 18 |
| 4. Task Management (Core) | 13 | 94 |
| 5. System Lists (GTD Framework) | 6 | 25 |
| 6. Project Management | 6 | 31 |
| 7. Labels & Tagging | 5 | 21 |
| **Total** | **54** | **287** |

### Recommended Sprint Sequence (~30 SP per 2-week sprint)

| Sprint | Focus | SP |
|--------|-------|----|
| 1 | Infrastructure: backend + DB entities | 26 |
| 2 | Infrastructure: DB migration + seed + frontend setup | 21 |
| 3 | CI + Auth backend (hashing, JWT, register) | 25 |
| 4 | Auth (login, me, frontend pages, auth state) | 23 |
| 5 | App shell + sidebar + Task create API | 23 |
| 6 | Task API (get, update, complete, delete, reorder) | 28 |
| 7 | Task UI (list, completion toggle, drag-and-drop, quick-add) | 26 |
| 8 | Task creation form + detail panel + editing | 27 |
| 9 | Archive + all system list pages + upcoming backend | 22 |
| 10 | Task movement + Projects backend | 18 |
| 11 | Projects UI + Labels backend | 26 |
| 12 | Labels UI + polish | 16 |

### Dependency Graph

```
Epic 1 (Infrastructure) → ALL OTHER EPICS
Epic 2 (Auth) → Epic 3, 4, 5, 6, 7
Epic 3 (Shell) → Epic 5, 6, 7 (sidebar navigation)
Epic 4 (Tasks) → Epic 5 (list views use task components)
Epic 4 (Tasks) → Epic 6 (project view uses task components)
Epic 4 (Tasks) → Epic 7 (label views use task components)
```

### Key Architectural Decisions

1. **Dual Organization Model:** Tasks belong to both a system list AND optionally a project via separate `SystemList` and `ProjectId` fields.
2. **Soft-Delete for Completion:** `IsArchived = true` on completed tasks. Hard-delete is separate via `DELETE`.
3. **Sort Order at DB Level:** `SortOrder` (int) enables user-controlled ordering. Reorder updates multiple rows in a transaction.
4. **Upcoming as Computed View:** Merges date-driven (next 14 days) and manual (Upcoming list) tasks.
5. **JWT Stateless Auth:** No server-side sessions. Token validated per request via ASP.NET Core middleware.

### Verification Plan

1. **Backend:** Run `dotnet test` from `/src/backend` — all unit and integration tests must pass
2. **Frontend:** Run `npm test` from `/src/frontend` — all component tests must pass
3. **End-to-end:** Start the stack via `docker-compose up`, register a user, create tasks across lists, create a project, assign labels, complete/delete tasks, verify archive
4. **CI:** Push a PR and verify the GitHub Actions pipeline passes all checks
