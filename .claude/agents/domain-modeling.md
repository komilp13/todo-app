# Agent: Domain Modeling

**Subagent Type**: `general-purpose`

**Purpose**: Design domain entities, aggregates, value objects, and business rules for new features.

## When to Use

- Before implementing major features
- Designing complex business logic
- Creating new aggregate roots
- Establishing value objects
- Defining invariants and business rules

## Example Prompts

### Example 1: Design Task Entity

```
You are a domain modeling expert for a GTD todo application.
Design the complete domain model for a TodoTask entity based on these requirements:

Requirements:
1. A task belongs to a user and has a unique ID
2. A task has:
   - Name (required, max 500 chars)
   - Description (optional, max 4000 chars)
   - Priority (P1-P4 enum)
   - Status (Open/Done enum)
   - SystemList (Inbox/Next/Upcoming/Someday enum)
   - DueDate (optional)
   - ProjectId (optional FK)
   - SortOrder (for manual drag-and-drop reordering)
   - IsArchived (soft delete flag)
   - CompletedAt (timestamp when marked done)
   - CreatedAt / UpdatedAt (audit timestamps)
3. Tasks can be moved between system lists
4. A task cannot be completed if already done (idempotent completion)
5. Completing a task archives it and sets CompletedAt
6. Reopening a task unarchives it and clears CompletedAt
7. A task can have labels (many-to-many relationship)

Design the domain entity with:
- Private constructor (for EF Core only)
- Private setters to enforce invariants
- Factory method Create() with sensible defaults
- Methods: Complete(), Reopen(), MoveTo(SystemList), UpdateName(), etc.
- Business rule enforcement (e.g., prevent double-completion)
- No EF Core attributes [Table], [Column], etc.
- No ASP.NET dependencies

Provide:
1. Complete C# class code
2. Explanation of invariants and business rules
3. Value objects if applicable (e.g., TaskName, TaskDescription)
4. Aggregate relationships (how Task interacts with Label, Project, User)
5. Any domain events that should be raised

Reference architecture: Clean Architecture + Vertical Slice Architecture (see CLAUDE.md)
```

### Example 2: Design Project Entity with Completion

```
Design the Project aggregate for our GTD app with these requirements:

Features:
1. Project has name, description, optional due date
2. Project has status (Active/Completed)
3. Project can be marked as Completed (but this doesn't complete tasks)
4. Project tracks completion percentage (# completed tasks / total tasks)
5. Project has manual SortOrder for reordering
6. Project belongs to a user
7. Tasks can be associated with a project

Design:
- Aggregate root entity
- Completion business logic
- Specification pattern for computing completion %
- Invariants (e.g., cannot mark inactive projects as completed)
- Value objects if needed
- No EF Core attributes

Output: Complete domain entity code + explanations
```

### Example 3: Design Label Entity

```
Design the Label entity with these requirements:

1. User-created labels for cross-cutting task categorization
2. Each label has name (unique per user), optional color
3. Labels can be assigned to tasks (many-to-many via TaskLabel)
4. Deleting a label removes it from all tasks
5. Label name is case-insensitive but stored as-is

Constraints:
- Name: max 100 chars, unique per user (case-insensitive)
- Color: hex value (e.g., "#FF4444"), optional
- No frequency data in the entity itself (computed separately)

Design:
- Pure domain entity
- Factory method
- Validation rules
- Proper encapsulation
- TaskLabel join entity design

Output: Complete code + value objects if applicable
```

## What to Expect from Agent

1. **Complete Domain Entity Code**
   - Private constructor for EF Core
   - Private setters
   - Factory methods
   - Business logic methods
   - Invariant enforcement

2. **Business Rules Documentation**
   - Explicit list of rules
   - Invariant explanations
   - State transition diagrams if complex
   - Edge case handling

3. **Value Object Recommendations**
   - When to use value objects
   - Example implementations
   - Equality and comparison rules

4. **Aggregate Design**
   - Root entity identification
   - Child entity relationships
   - Boundary definitions
   - Transaction consistency

5. **Code Examples**
   ```csharp
   public class TodoTask
   {
       // Implementation...
   }
   ```

6. **EF Core Configuration Preview**
   - Property configurations needed
   - Index recommendations
   - Relationship setup hints

## Integration Points

After agent provides design:

1. **Create domain entity file**: `/Core/Domain/Entities/{EntityName}.cs`
2. **Create value objects** if recommended: `/Core/Domain/ValueObjects/{Name}.cs`
3. **Create EF configuration**: `/Infrastructure/Persistence/Configurations/{Name}Configuration.cs`
4. **Create migration**: `/add-migration Create{EntityName}Table`
5. **Create repository interface**: `/Core/Interfaces/I{EntityName}Repository.cs`

## Follow-Up Questions Agent Might Ask

- "Should this be an aggregate root or a child entity?"
- "Do you need to track state transitions (history)?"
- "Are there any domain events that should be raised?"
- "How will this entity interact with other aggregates?"
- "What are your concurrency/consistency requirements?"

## Example Output Structure

```
# Domain Entity Design: TodoTask

## Overview
- Aggregate Root: Yes
- Root Cause: User owns tasks; they're the consistency boundary
- Lifecycle: Create → Modify → Complete → Archive → Delete

## Invariants
1. Task cannot be completed twice (idempotent)
2. Completed task must be archived
3. Task name cannot be empty
4. SortOrder must be >= 0
5. CompletedAt only set when Status = Done

## Domain Entity Code
[Full implementation]

## Value Objects
[Any value objects recommended]

## Business Methods
[Complete() method, Reopen() method, etc.]

## Factory Methods
[TodoTask.Create(...)]

## EF Core Configuration Hints
[What IEntityTypeConfiguration should look like]

## Testing Implications
[What domain logic to unit test]
```

## Tips for Using This Agent

1. **Be specific about requirements** — The more details, the better the design
2. **Ask follow-up questions** — "What if..." scenarios help refine the design
3. **Request examples** — Ask for usage examples showing the entity in action
4. **Validate assumptions** — "Does this handle X scenario?"
5. **Get architectural guidance** — Ask about relationships and aggregate boundaries

## Common Topics to Explore

- **Immutability**: When to use private setters vs. fluent builders
- **Factory Methods**: Ensuring proper initialization and invariant enforcement
- **Domain Events**: What events should be raised during state changes
- **Value Objects**: When name/email/etc. should be value objects
- **Specifications**: Reusable query specifications for complex logic
- **Aggregates**: Boundaries between root and child entities
