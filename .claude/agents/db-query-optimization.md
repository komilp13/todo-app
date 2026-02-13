# Agent: Database Query Optimization

**Subagent Type**: `Explore`

**Purpose**: Optimize complex database queries, identify N+1 problems, recommend indexes and performance improvements.

## When to Use

- Complex queries with relationships
- Potential N+1 query problems
- Performance-critical queries
- Pagination and filtering logic
- Understanding query execution

## Example Prompts

### Example 1: Optimize GetUpcomingTasks

```
Optimize the GetUpcomingTasks query for a GTD todo app:

Requirements:
- Fetch tasks where:
  - Due date within 14 days (from ANY system list), OR
  - SystemList = Upcoming (regardless of date)
- Include related:
  - Labels (many-to-many via TaskLabel)
  - Project (FK relationship)
  - User info (FK relationship)
- Sort: Overdue first (oldest first), then by due date
- Filter: Only non-archived, open tasks
- Filter: Only authenticated user's tasks
- Return: Full task with related data

Current Approach (Inefficient):
```csharp
var tasks = await _context.Tasks
    .Where(t => t.UserId == userId)
    .Where(t => !t.IsArchived && t.Status == TaskStatus.Open)
    .ToListAsync();  // ← Query 1

foreach (var task in tasks)
{
    var labels = await _context.TaskLabels
        .Where(tl => tl.TaskId == task.Id)
        .ToListAsync();  // ← N additional queries!
}
```

Provide:
1. Optimized EF Core LINQ query
2. Detailed .Include()/.ThenInclude() strategy
3. .Select() projection if applicable
4. SQL that would be generated
5. Index recommendations
6. Performance estimates (before/after)
7. Alternative approaches if applicable
8. N+1 problem explanation
```

### Example 2: Optimize GetTasksWithFiltering

```
Optimize this complex filtered query:

Endpoint: GET /api/tasks?systemList=Inbox&projectId=123&labelId=456&status=Open&sortBy=dueDate

Requirements:
- Filter by systemList (optional)
- Filter by projectId (optional)
- Filter by labelId (optional, tasks with that label)
- Filter by status (optional: Open/Done/All)
- Sort by multiple fields (sortBy param)
- Pagination (page, pageSize)

Issues:
- labelId is many-to-many (need JOIN)
- Multiple optional filters (complex WHERE)
- Pagination with sorting
- Relationship loading

Provide:
1. Optimized IQueryable<Task> builder pattern
2. Dynamic predicate building
3. N+1 prevention strategy
4. Pagination handling
5. Sorting implementation
6. Generated SQL preview
7. Index strategy
8. Performance impact
```

### Example 3: Optimize Task Count by Status Query

```
Optimize sidebar task counts query:

Currently:
```csharp
var inboxCount = await _context.Tasks
    .Where(t => t.UserId == userId && t.SystemList == SystemList.Inbox && !t.IsArchived)
    .CountAsync();

var nextCount = await _context.Tasks
    .Where(t => t.UserId == userId && t.SystemList == SystemList.Next && !t.IsArchived)
    .CountAsync();

// ... repeated for each system list
```

This runs 4 separate queries!

Better approach:
- Single query returning all counts
- GROUP BY SystemList
- Cache strategy

Provide:
1. Single query returning all counts
2. DTO structure for response
3. Caching strategy
4. Index requirements
5. Performance comparison
```

## What to Expect from Agent

1. **Optimized EF Core Query**
   ```csharp
   var tasks = await _context.Tasks
       .Where(t => t.UserId == userId)
       .Where(t => !t.IsArchived && t.Status == TaskStatus.Open)
       .Include(t => t.Labels)
           .ThenInclude(tl => tl.Label)
       .Include(t => t.Project)
       .OrderBy(t => t.DueDate)
       .ThenBy(t => t.SortOrder)
       .ToListAsync();
   ```

2. **Generated SQL**
   ```sql
   SELECT t.*, p.*, l.*, tl.*
   FROM Tasks t
   LEFT JOIN Projects p ON t.ProjectId = p.Id
   LEFT JOIN TaskLabels tl ON t.Id = tl.TaskId
   LEFT JOIN Labels l ON tl.LabelId = l.Id
   WHERE t.UserId = @userId
     AND t.IsArchived = false
     AND t.Status = 'Open'
   ORDER BY t.DueDate, t.SortOrder
   ```

3. **Index Recommendations**
   ```
   CREATE INDEX IX_Tasks_UserId_IsArchived_Status
     ON Tasks(UserId, IsArchived, Status)
     INCLUDE (DueDate, SortOrder)

   CREATE INDEX IX_TaskLabels_TaskId
     ON TaskLabels(TaskId)
   ```

4. **N+1 Problem Explanation**
   ```
   ✗ Problem: 1 query for tasks + N queries for labels = 1 + N queries
   ✓ Solution: Use .Include(t => t.Labels).ThenInclude(...) = 1 query
   ```

5. **Performance Comparison**
   ```
   Before: 101 queries, ~2500ms
   After:  1 query, ~50ms

   Improvement: 50x faster
   ```

6. **Memory Considerations**
   ```
   .Select() projection if full entities not needed
   Pagination to limit memory usage
   ```

## Integration Points

After optimization:

1. **Implement in handler**:
   ```csharp
   public class GetUpcomingTasksHandler : IQueryHandler<GetUpcomingTasksQuery, List<TaskResponse>>
   {
       public async Task<List<TaskResponse>> Handle(...)
       {
           // Use optimized query from agent
       }
   }
   ```

2. **Create indexes** if recommended:
   `/add-migration AddTaskPerformanceIndexes`

3. **Test performance**:
   ```bash
   /test-slice GetUpcomingTasks
   ```

## Common Optimization Patterns

### Pattern 1: Include with ThenInclude
```csharp
.Include(t => t.Labels)
    .ThenInclude(tl => tl.Label)
.Include(t => t.Project)
```

### Pattern 2: Select Projection (if not needing full entity)
```csharp
.Select(t => new TaskResponse
{
    Id = t.Id,
    Name = t.Name,
    // Only needed fields
})
```

### Pattern 3: Dynamic Filtering
```csharp
var query = _context.Tasks.AsQueryable();

if (systemList.HasValue)
    query = query.Where(t => t.SystemList == systemList);

if (!string.IsNullOrEmpty(projectId))
    query = query.Where(t => t.ProjectId == projectId);

return await query.ToListAsync();
```

### Pattern 4: Grouping for Aggregates
```csharp
var counts = await _context.Tasks
    .Where(t => t.UserId == userId)
    .GroupBy(t => t.SystemList)
    .Select(g => new { SystemList = g.Key, Count = g.Count() })
    .ToListAsync();
```

## Index Strategy

### Covering Index
```sql
CREATE INDEX IX_Tasks_Lookup
  ON Tasks(UserId, SystemList, IsArchived)
  INCLUDE (DueDate, SortOrder, Name)
```

### Composite Index
```sql
CREATE INDEX IX_Tasks_UserId_SystemList
  ON Tasks(UserId, SystemList)
```

## Performance Checklist

After optimization, verify:
- [ ] Single query for single operation
- [ ] .Include() for relationships
- [ ] .ThenInclude() for nested relationships
- [ ] Indexes on FK and filter columns
- [ ] No N+1 queries
- [ ] Pagination for large datasets
- [ ] .Select() for projections if applicable
- [ ] Query benchmarked
- [ ] SQL reviewed for efficiency

## Tools for Analysis

### EF Core Query Logging
```csharp
// In Program.cs
optionsBuilder
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging();
```

### SQL Server Query Profiler
- Run generated SQL
- Check execution plan
- Identify missing indexes
- View actual vs estimated rows

## Follow-Up Questions

- "Should I use projection (.Select()) or full entities?"
- "What's the expected dataset size?"
- "Is pagination necessary?"
- "Should results be cached?"
- "How frequently is this query called?"

## Caching Considerations

For frequently accessed data:
```csharp
public async Task<List<TaskResponse>> GetUpcomingTasks(...)
{
    var cacheKey = $"upcoming_tasks_{userId}";
    if (_cache.TryGetValue(cacheKey, out var cached))
        return cached;

    var tasks = await GetUpcomingTasksQuery(...);
    _cache.Set(cacheKey, tasks, TimeSpan.FromMinutes(5));
    return tasks;
}

// Invalidate when tasks change
_cache.Remove(cacheKey);
```

## Tips for Using This Agent

1. **Provide context** — Expected dataset size, frequency
2. **Show current query** — Agent can identify problems
3. **Ask for SQL** — See generated SQL for index planning
4. **Request benchmarks** — Understand performance gains
5. **Ask about alternatives** — Multiple approaches often exist

## Common Mistakes to Avoid

- [ ] Forgetting .Include() → N+1 queries
- [ ] Multiple queries in loops
- [ ] Calling .ToList() before filtering
- [ ] Selecting all columns when only few needed
- [ ] Missing indexes on join/filter columns
- [ ] Not using AsNoTracking() for read-only queries

## Performance Baseline

Typical expectations:
- Single-task queries: <10ms
- List queries (100 items): <50ms
- Complex filtered queries: <100ms
- Pagination queries: <50ms

Anything slower suggests optimization opportunity.
