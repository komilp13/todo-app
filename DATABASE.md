# Database Management

This document describes how to manage the TodoApp database using Entity Framework Core migrations.

## Prerequisites

- PostgreSQL 15+ installed and running
- .NET 8 SDK installed
- `dotnet-ef` CLI tool installed globally: `dotnet tool install --global dotnet-ef`

## Connection String

The database connection string is configured in `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=5432;Database=todo_app;User Id=postgres;Password=postgres;"
}
```

Update the connection string as needed for your environment.

## Creating the Database

### Option 1: Using Migrations (Recommended)

From the `/src/backend` directory:

```bash
# Apply all pending migrations
dotnet ef database update --project TodoApp.Api --startup-project TodoApp.Api
```

This creates the database and applies all migrations.

### Option 2: From Scratch with Docker

If using Docker Compose (see `docker-compose.yml`):

```bash
# Start PostgreSQL service
docker-compose up -d postgres

# Apply migrations
cd src/backend
dotnet ef database update --project TodoApp.Api --startup-project TodoApp.Api
```

## Managing Migrations

### Create a New Migration

After modifying domain entities or entity configurations:

```bash
cd src/backend
dotnet ef migrations add MigrationName --project TodoApp.Infrastructure --startup-project TodoApp.Api
```

Example:
```bash
dotnet ef migrations add AddUserRoles --project TodoApp.Infrastructure --startup-project TodoApp.Api
```

### Apply Migrations

```bash
# Apply all pending migrations
dotnet ef database update --project TodoApp.Api --startup-project TodoApp.Api

# Apply specific migration
dotnet ef database update 20260213230930_InitialCreate --project TodoApp.Api --startup-project TodoApp.Api
```

### Revert Migrations

```bash
# Revert to a previous migration
dotnet ef database update 20260213230930_InitialCreate --project TodoApp.Api --startup-project TodoApp.Api

# Revert all migrations (drop database)
dotnet ef database update 0 --project TodoApp.Api --startup-project TodoApp.Api
```

### View Migration History

```bash
# List all migrations
dotnet ef migrations list --project TodoApp.Infrastructure --startup-project TodoApp.Api

# View pending migrations
dotnet ef database update --dry-run --project TodoApp.Api --startup-project TodoApp.Api
```

## Database Schema

### Tables

- **users** — User accounts with authentication credentials
- **projects** — Goals/projects that group tasks
- **tasks** — Individual todo items with GTD system list assignment
- **labels** — User-created categories for task organization
- **task_labels** — Join table for task-label relationships

### Key Constraints

- User email is unique (case-insensitive)
- Label name is unique per user
- Foreign keys enforce referential integrity:
  - Tasks and Projects cascade delete with User (deleting user deletes their tasks/projects)
  - Tasks set ProjectId to NULL if Project is deleted (tasks remain but lose project)
  - TaskLabels cascade delete with Task or Label

### Indexes

Optimized for common queries:
- `tasks.UserId`, `tasks.ProjectId`, `tasks.SystemList`, `tasks.Status`, `tasks.DueDate`
- `projects.UserId`, `labels.UserId`
- `users.Email` (unique)

## Development Workflow

### Initial Setup

```bash
cd src/backend

# Restore packages
dotnet restore

# Create database and apply migrations
dotnet ef database update --project TodoApp.Api --startup-project TodoApp.Api

# (Optional) Seed with sample data
# See Story 1.2.4 for seed data implementation
```

### Making Schema Changes

1. Modify domain entities in `TodoApp.Domain/Entities/`
2. Update entity configurations in `TodoApp.Infrastructure/Persistence/Configurations/`
3. Create migration: `dotnet ef migrations add DescriptiveNameOfChange --project TodoApp.Infrastructure --startup-project TodoApp.Api`
4. Apply migration: `dotnet ef database update --project TodoApp.Api --startup-project TodoApp.Api`
5. Test with unit and integration tests

### Resetting During Development

To drop and recreate the database:

```bash
# Drop all tables
dotnet ef database update 0 --project TodoApp.Api --startup-project TodoApp.Api

# Re-apply all migrations
dotnet ef database update --project TodoApp.Api --startup-project TodoApp.Api
```

## Troubleshooting

### "Unable to connect to PostgreSQL"

Ensure PostgreSQL is running:
- Check connection string in `appsettings.Development.json`
- Default: `Server=localhost;Port=5432;User Id=postgres;Password=postgres;`
- Test connection: `psql -U postgres -h localhost`

### "migrations not found"

Ensure you're in the `/src/backend` directory and using correct project paths:

```bash
# Verify Infrastructure project path
cd src/backend
ls TodoApp.Infrastructure/Migrations/
```

### "No DbContext named 'ApplicationDbContext'"

Ensure `appsettings.Development.json` is in the Api project and DbContext is registered in DI.

## References

- [Entity Framework Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [dotnet-ef Tool Documentation](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
