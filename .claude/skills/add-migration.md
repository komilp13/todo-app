# Skill: add-migration

Creates and optionally applies EF Core migrations with correct project references.

## Invocation

```
/add-migration <MigrationName> [--apply] [--dry-run]
```

## Examples

```
/add-migration InitialCreate
/add-migration AddTaskPriorityIndex --apply
/add-migration CreateProjectEntity --dry-run
/add-migration AddSoftDeleteColumns
```

## Parameters

- **MigrationName** (required): PascalCase migration name (without timestamp)
- **--apply** (optional): Automatically apply migration after creating (runs `dotnet ef database update`)
- **--dry-run** (optional): Show what would happen without creating files

## What It Does

### Step 1: Create Migration
```bash
dotnet ef migrations add {MigrationName} \
  -p /src/backend/TodoApp.Infrastructure \
  -s /src/backend/TodoApp.Api
```

**Output**: Migration file in `Infrastructure/Migrations/` folder

### Step 2: Show Migration Preview
```
Migration file created:
  Infrastructure/Migrations/20260213_AddTaskPriorityIndex.cs

Changes detected:
  - Add column: Task.Priority (enum)
  - Add index: IX_Task_Priority
  - Alter table: Tasks
```

### Step 3: Apply (if --apply flag)
```bash
dotnet ef database update -p TodoApp.Infrastructure -s TodoApp.Api
```

**Output**:
```
Applying migration: AddTaskPriorityIndex
Build started...
Build succeeded.
Applying migration '20260213_AddTaskPriorityIndex'
Done. Successfully applied 1 migration(s) to database.
```

## Before Creating Migration

Ensure you have:

1. **Modified domain entity** (e.g., added property to TodoTask):
   ```csharp
   public int Priority { get; private set; }
   ```

2. **Updated EF Core configuration** (in EntityTypeConfiguration):
   ```csharp
   builder.Property(t => t.Priority)
       .HasConversion<string>()
       .HasMaxLength(10);

   builder.HasIndex(t => t.Priority);
   ```

3. **Running locally**: PostgreSQL must be running (Docker Compose or local)

4. **Connection string set**: In `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=todoapp_dev;..."
     }
   }
   ```

## Common Patterns

### Adding a New Entity
```
/add-migration CreateProjectEntity --apply
```

**Domain entity** (`Domain/Entities/Project.cs`):
```csharp
public class Project
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public string Status { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

**EF Configuration** (`Infrastructure/Persistence/Configurations/ProjectConfiguration.cs`):
```csharp
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId);
        // Configure properties...
    }
}
```

### Adding a Column
```
/add-migration AddTaskDescription --apply
```

**Domain entity change**:
```csharp
public string? Description { get; private set; }  // Added
```

**EF Configuration change**:
```csharp
builder.Property(t => t.Description)
    .HasMaxLength(4000)
    .IsRequired(false);
```

### Adding an Index
```
/add-migration AddTaskPriorityIndex --apply
```

**EF Configuration**:
```csharp
builder.HasIndex(t => t.Priority);
builder.HasIndex(t => t.DueDate);
```

### Soft Delete Implementation
```
/add-migration AddSoftDeleteColumns --apply
```

**Domain entity**:
```csharp
public bool IsArchived { get; private set; }
public DateTime? CompletedAt { get; private set; }
```

**EF Configuration**:
```csharp
builder.Property(t => t.IsArchived)
    .HasDefaultValue(false);

// Query filter for soft delete
builder.HasQueryFilter(t => !t.IsArchived);
```

## Migration File Format

Generated migration files follow this structure:

```csharp
namespace TodoApp.Infrastructure.Migrations
{
    public partial class AddTaskPriorityIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Tasks",
                type: "text",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Task_Priority",
                table: "Tasks",
                column: "Priority");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Task_Priority",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Tasks");
        }
    }
}
```

## Rollback

If you need to revert a migration:

```bash
# Revert to previous migration
dotnet ef database update PreviousMigrationName \
  -p TodoApp.Infrastructure \
  -s TodoApp.Api

# Remove migration file
rm Infrastructure/Migrations/20260213_AddTaskPriorityIndex.cs
```

## Troubleshooting

### "No database provider"
- Ensure `appsettings.Development.json` has connection string
- Verify PostgreSQL is running

### "Conflicting migrations"
- Two migrations with same timestamp
- Regenerate with unique names or delete conflicting file

### "Model snapshot out of sync"
- Run migration again to regenerate snapshot
- Delete migration and recreate

## Best Practices

1. **Small focused migrations** — One logical change per migration
2. **Descriptive names** — `AddTaskPriority` not `UpdateDatabase`
3. **Test before applying** — Use `--dry-run` to preview
4. **Keep domain model in sync** — Update entity and config together
5. **Document schema changes** — Add comments if complex logic

## Next Steps

1. Create migration: `/add-migration MigrationName`
2. Preview changes (automatic)
3. Test: `dotnet ef database update` (or use `--apply` flag)
4. Verify with `dotnet ef migrations list` to see applied migrations
5. Commit migration file and domain changes together
