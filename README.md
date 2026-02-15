# GTD Todo App

A web-based Getting Things Done (GTD) todo application inspired by Todoist, with multi-user support, system lists, projects, labels, and task management.

## Overview

GTD Todo App organizes tasks using David Allen's [GTD methodology](https://gettingthingsdone.com/). Tasks flow through four system lists — **Inbox**, **Next**, **Upcoming**, and **Someday** — giving users a structured workflow for capturing, organizing, and executing work. Tasks can also belong to **Projects** and be tagged with **Labels** for flexible cross-cutting organization.

### Key Features

- **System Lists (GTD)**: Inbox (capture), Next (focus), Upcoming (time-aware), Someday (deferred)
- **Projects**: Group related tasks toward a goal with progress tracking
- **Labels**: User-created tags for flexible categorization (e.g., @work, @home)
- **Task Management**: Create, edit, complete (soft-delete), delete (hard-delete), and manually reorder tasks via drag-and-drop
- **Priority Levels**: P1 (urgent) through P4 (low) with color-coded indicators
- **Dual Organization**: Tasks belong to both a system list AND optionally a project
- **Multi-User**: Email/password authentication with JWT tokens

## Tech Stack

| Layer      | Technology                                      |
|------------|-------------------------------------------------|
| Backend    | C# 9 / ASP.NET Core Web API, Entity Framework Core |
| Database   | PostgreSQL                                      |
| Frontend   | React / Next.js (TypeScript), Tailwind CSS      |
| Testing    | xUnit (backend), Jest + React Testing Library (frontend) |
| CI/CD      | GitHub Actions                                  |
| Dev Environment | Docker Compose                             |

## Architecture

### Backend — Clean Architecture + Vertical Slice

The backend combines **Clean Architecture** (dependency inversion, framework independence) with **Vertical Slice Architecture** (feature-organized code) and **CQRS** (command/query separation).

```
┌─────────────────────────────────────────────┐
│  API Layer (thin controllers)               │
├─────────────────────────────────────────────┤
│  Features (vertical slices)                 │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │ Auth     │ │ Tasks    │ │ Projects │    │
│  │ Register │ │ Create   │ │ Create   │    │
│  │ Login    │ │ GetTasks │ │ List     │    │
│  │ GetMe    │ │ Update   │ │ ...      │    │
│  │          │ │ Complete │ │          │    │
│  │          │ │ Delete   │ │          │    │
│  │          │ │ Reorder  │ │          │    │
│  └──────────┘ └──────────┘ └──────────┘    │
├─────────────────────────────────────────────┤
│  Core (domain entities, interfaces)         │
├─────────────────────────────────────────────┤
│  Infrastructure (EF Core, repositories)     │
└─────────────────────────────────────────────┘
```

Each feature slice contains its own command/query, validator, handler, and response DTO. Dependencies flow inward — controllers depend on handlers, handlers depend on domain entities and interfaces, never the reverse.

### Frontend — Next.js App Router

The frontend uses Next.js with the App Router, React context for auth state, and a centralized API client with JWT token injection. Pages map to GTD system lists (`/inbox`, `/next`, `/upcoming`, `/someday`), projects (`/projects/{id}`), and labels (`/labels/{id}`).

### Domain Model

```
User ──< TodoTask >── Project
              │
         TaskLabel
              │
           Label
```

- **User**: Email/password authentication, display name
- **TodoTask**: Name, description, due date, priority (P1–P4), status (Open/Done), system list, sort order, optional project
- **Project**: Name, description, due date, status (Active/Completed), sort order
- **Label**: Name (unique per user), optional color
- **TaskLabel**: Many-to-many join between tasks and labels

### Key Design Decisions

1. **Dual Organization**: Tasks have both a `SystemList` (GTD workflow state) and an optional `ProjectId` (goal grouping)
2. **Soft-Delete for Completion**: Completed tasks are archived (`IsArchived = true`, `CompletedAt` timestamp), not deleted
3. **DB-Level Sort Order**: `SortOrder` integer field enables manual drag-and-drop reordering with atomic batch updates
4. **Upcoming as Computed View**: Merges tasks with due dates within 14 days (from any list) with tasks explicitly in the Upcoming list
5. **Stateless JWT Auth**: No server-side sessions; passwords hashed with PBKDF2 (100k+ iterations) + salt

## Project Structure

```
src/
├── backend/                    # C# backend (Clean + Vertical Slice Architecture)
│   ├── Core/
│   │   ├── Domain/             # Entities, enums, value objects
│   │   └── Interfaces/         # Repository and service contracts
│   ├── Features/               # Vertical slices (Auth, Tasks, Projects, Labels)
│   ├── Shared/                 # Cross-cutting (middleware, validation, exceptions)
│   ├── API/                    # Controllers, Program.cs
│   └── Tests/                  # Unit and integration tests
└── frontend/                   # Next.js frontend
    └── src/
        ├── app/                # App Router pages
        ├── components/         # React components
        ├── hooks/              # Custom hooks
        ├── services/           # API client
        └── types/              # TypeScript interfaces
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [PostgreSQL 15+](https://www.postgresql.org/) (or Docker)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) (optional, for containerized dev)

### Quick Start with Docker

```bash
# Clone the repository
git clone <repo-url>
cd todo-app

# Copy environment variables
cp .env.example .env

# Start all services (PostgreSQL, backend, frontend)
docker-compose up
```

The app will be available at:
- **Frontend**: http://localhost:3000 (port 3000)
- **Backend API**: http://localhost:5000 (port 5000)
- **Swagger docs**: http://localhost:5000/swagger (port 5000, development only)

### Manual Setup

**Backend:**

```bash
cd src/backend

# Restore and build
dotnet build

# Apply database migrations
dotnet ef database update

# Run the API
dotnet run --project API
```

**Frontend:**

```bash
cd src/frontend

# Install dependencies
npm install

# Ensure .env is configured with correct API URL on port 5000:
# NEXT_PUBLIC_API_URL=http://localhost:5000/api

# Start dev server
npm run dev
```

## Development

### Running Tests

```bash
# Backend tests
cd src/backend
dotnet test

# Frontend tests
cd src/frontend
npm test
```

### Useful Commands

```bash
# Backend - watch mode (auto-rebuild on changes)
dotnet watch test

# Frontend - lint
npm run lint

# Docker - view logs
docker-compose logs -f

# Database - add a new migration
dotnet ef migrations add MigrationName -p TodoApp.Infrastructure -s TodoApp.Api
```

## API Endpoints

| Method | Endpoint                          | Description              |
|--------|-----------------------------------|--------------------------|
| GET    | `/api/health`                     | Health check             |
| POST   | `/api/auth/register`              | Register new user        |
| POST   | `/api/auth/login`                 | Login, returns JWT       |
| GET    | `/api/auth/me`                    | Current user profile     |
| POST   | `/api/tasks`                      | Create task              |
| GET    | `/api/tasks`                      | List tasks (with filters)|
| GET    | `/api/tasks/{id}`                 | Get single task          |
| PUT    | `/api/tasks/{id}`                 | Update task              |
| PATCH  | `/api/tasks/{id}/complete`        | Complete (archive) task  |
| PATCH  | `/api/tasks/{id}/reopen`          | Reopen archived task     |
| DELETE | `/api/tasks/{id}`                 | Permanently delete task  |
| PATCH  | `/api/tasks/reorder`              | Reorder tasks            |
| POST   | `/api/projects`                   | Create project           |
| GET    | `/api/projects`                   | List projects with stats |
| GET    | `/api/projects/{id}`              | Get project details      |
| PUT    | `/api/projects/{id}`              | Update project           |
| DELETE | `/api/projects/{id}`              | Delete project           |
| POST   | `/api/labels`                     | Create label             |
| GET    | `/api/labels`                     | List labels              |
| PUT    | `/api/labels/{id}`                | Update label             |
| DELETE | `/api/labels/{id}`                | Delete label             |

## Documentation

- [Product Backlog](docs/backlog.md) — Full epics, features, user stories, and acceptance criteria

## License

This project is private and not licensed for distribution.
