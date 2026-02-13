# Skill: api-client-gen

Generates TypeScript interfaces and API client methods from backend DTOs.

## Invocation

```
/api-client-gen <FeatureArea> [--overwrite] [--dry-run]
```

## Examples

```
/api-client-gen Tasks
/api-client-gen Auth
/api-client-gen Projects --overwrite
/api-client-gen Labels --dry-run
```

## Parameters

- **FeatureArea** (required): Feature name (Tasks, Auth, Projects, Labels)
- **--overwrite** (optional): Replace existing types (without confirmation)
- **--dry-run** (optional): Show what would be generated without creating files

## What It Does

### Step 1: Scan Backend DTOs
Scans C# response classes in `/src/backend/Features/{Feature}/`:
```csharp
// Creates these from Response DTOs
public class CreateTaskResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Step 2: Generate TypeScript Interfaces
Creates `src/types/{feature}.ts`:
```typescript
export interface CreateTaskResponse {
  id: string;
  name: string;
  createdAt: string;
}
```

### Step 3: Generate API Client Methods
Updates `src/services/apiClient.ts`:
```typescript
export const tasksApi = {
  create(command: CreateTaskCommand): Promise<CreateTaskResponse> {
    return apiClient.post('/api/tasks', command);
  },

  list(query: GetTasksQuery): Promise<GetTasksResponse[]> {
    return apiClient.get('/api/tasks', { params: query });
  },

  update(id: string, command: UpdateTaskCommand): Promise<TaskResponse> {
    return apiClient.put(`/api/tasks/${id}`, command);
  },
};
```

### Step 4: Update Central Types
Updates `src/types/index.ts`:
```typescript
export * from './tasks';
export * from './auth';
export * from './projects';
export * from './labels';
```

## Generated Files

### Example: Tasks Feature

**Input** (Backend):
```
/src/backend/Features/Tasks/CreateTask/CreateTaskCommand.cs
/src/backend/Features/Tasks/CreateTask/CreateTaskResponse.cs
/src/backend/Features/Tasks/GetTasks/GetTasksQuery.cs
/src/backend/Features/Tasks/GetTasks/GetTasksResponse.cs
/src/backend/Features/Tasks/UpdateTask/UpdateTaskCommand.cs
```

**Output** (Frontend):
```
src/types/tasks.ts
```

With:
```typescript
export interface CreateTaskCommand {
  name: string;
  description?: string;
  dueDate?: string;
  priority?: Priority;
  systemList?: SystemList;
  projectId?: string;
}

export interface CreateTaskResponse {
  id: string;
  name: string;
  description?: string;
  dueDate?: string;
  priority: Priority;
  status: TaskStatus;
  systemList: SystemList;
  sortOrder: number;
  projectId?: string;
  isArchived: boolean;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface GetTasksQuery {
  systemList?: SystemList;
  projectId?: string;
  labelId?: string;
  status?: TaskStatus;
  archived?: boolean;
}

export interface GetTasksResponse extends CreateTaskResponse {
  labels: Label[];
  project?: ProjectResponse;
}

// ... more interfaces
```

And in `src/services/apiClient.ts`:
```typescript
export const tasksApi = {
  create(command: CreateTaskCommand): Promise<CreateTaskResponse> {
    return apiClient.post('/api/tasks', command);
  },

  list(query: GetTasksQuery): Promise<GetTasksResponse[]> {
    return apiClient.get('/api/tasks', { params: query });
  },

  getById(id: string): Promise<GetTasksResponse> {
    return apiClient.get(`/api/tasks/${id}`);
  },

  update(id: string, command: UpdateTaskCommand): Promise<UpdateTaskResponse> {
    return apiClient.put(`/api/tasks/${id}`, command);
  },

  complete(id: string): Promise<TaskResponse> {
    return apiClient.patch(`/api/tasks/${id}/complete`);
  },

  reopen(id: string): Promise<TaskResponse> {
    return apiClient.patch(`/api/tasks/${id}/reopen`);
  },

  delete(id: string): Promise<void> {
    return apiClient.delete(`/api/tasks/${id}`);
  },

  reorder(payload: ReorderTasksCommand): Promise<void> {
    return apiClient.patch('/api/tasks/reorder', payload);
  },
};
```

## Type Mapping

### C# to TypeScript
| C# Type | TypeScript Type |
|---------|-----------------|
| `string` | `string` |
| `Guid` | `string` (UUID) |
| `int` | `number` |
| `bool` | `boolean` |
| `decimal` | `number` |
| `DateTime` | `string` (ISO 8601) |
| `DateTime?` | `string \| undefined` |
| `enum` | `enum` (generated) |
| `List<T>` | `T[]` |
| `T[]` | `T[]` |

### Enum Mapping

**Backend**:
```csharp
public enum Priority
{
    P1,
    P2,
    P3,
    P4
}
```

**Generated Frontend**:
```typescript
export enum Priority {
  P1 = 'P1',
  P2 = 'P2',
  P3 = 'P3',
  P4 = 'P4',
}
```

## Manual Adjustments

For complex scenarios, you can manually edit generated files:

### Custom API Method
```typescript
// In tasksApi object, add custom methods
async bulkComplete(taskIds: string[]): Promise<void> {
  return Promise.all(taskIds.map(id => this.complete(id)));
}
```

### Optional Query Builders
```typescript
export class GetTasksQueryBuilder {
  private query: GetTasksQuery = {};

  withSystemList(list: SystemList): this {
    this.query.systemList = list;
    return this;
  }

  withProjectId(id: string): this {
    this.query.projectId = id;
    return this;
  }

  build(): GetTasksQuery {
    return this.query;
  }
}

// Usage in component
const tasks = await tasksApi.list(
  new GetTasksQueryBuilder()
    .withSystemList(SystemList.Inbox)
    .withProjectId(projectId)
    .build()
);
```

## Component Usage Example

After generation, use in React components:

```typescript
import { tasksApi, CreateTaskCommand, TaskResponse } from '@/types';

export function CreateTaskForm() {
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (formData: CreateTaskCommand) => {
    setIsLoading(true);
    try {
      const response: TaskResponse = await tasksApi.create(formData);
      // Type-safe response handling
      console.log(`Task created: ${response.name}`);
    } catch (error) {
      console.error('Failed to create task', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    // Form JSX with full TypeScript support
  );
}
```

## Keeping Frontend and Backend Synchronized

### Workflow

1. **Backend**: Add new DTO or modify existing
   ```csharp
   public class CreateTaskResponse
   {
       public Guid Id { get; set; }
       public string Name { get; set; }
       // ... new field added
   }
   ```

2. **Frontend**: Run generation
   ```bash
   /api-client-gen Tasks --overwrite
   ```

3. **Frontend**: TypeScript interfaces automatically updated
   ```typescript
   export interface CreateTaskResponse {
       id: string;
       name: string;
       // ... new field appears
   }
   ```

4. **Component**: TypeScript error if not handling new field
   ```typescript
   // Compiler error if field not used/handled
   const task: CreateTaskResponse = response;
   // 'age' is not assignable... (if age was added)
   ```

## Dry-Run Example

```
/api-client-gen Tasks --dry-run
```

Output:
```
Scanning backend for Tasks DTOs...
✓ Found 8 response types:
  - CreateTaskCommand
  - CreateTaskResponse
  - GetTasksQuery
  - GetTasksResponse
  - UpdateTaskCommand
  - UpdateTaskResponse
  - CompleteTaskCommand
  - ReorderTasksCommand

Would generate:
  → src/types/tasks.ts (420 lines)
  → Update src/types/index.ts with export

Would generate API methods:
  - tasksApi.create()
  - tasksApi.list()
  - tasksApi.getById()
  - tasksApi.update()
  - tasksApi.complete()
  - tasksApi.reopen()
  - tasksApi.delete()
  - tasksApi.reorder()

Use --overwrite to apply changes
```

## Error Handling

### Circular References
If Response DTOs reference each other:
```csharp
public class TaskResponse
{
    public ProjectResponse Project { get; set; }
}

public class ProjectResponse
{
    public List<TaskResponse> Tasks { get; set; }
}
```

Generated with proper handling:
```typescript
export interface TaskResponse {
  project?: ProjectResponse;
}

export interface ProjectResponse {
  tasks: TaskResponse[];
}
```

### Missing DTOs
If command/response DTOs don't exist:
```
⚠ Warning: GetTasksCommand not found
  Assuming query-only endpoint
  Generated: getTasksList(query?: Filters): Promise<TaskResponse[]>
```

## Version Sync

Always regenerate after:
- ✓ Adding new endpoints
- ✓ Modifying request/response DTOs
- ✓ Adding enums to backend
- ✓ Changing field names or types

## Integration with CI/CD

### Pre-Deployment Validation
```yaml
- name: Generate API Client
  run: /api-client-gen Tasks Auth Projects Labels

- name: Check for uncommitted changes
  run: git diff --exit-code src/types/
  # Fails if generated types differ from committed
```

## Troubleshooting

### Generated Types Don't Match Backend
```bash
# Regenerate
/api-client-gen Tasks --overwrite

# Verify types match
npm run type-check
```

### Build Errors in Components
```typescript
// Error: Property 'name' does not exist on type 'TaskResponse'
const task: TaskResponse = response;
console.log(task.name); // ← Regenerate types from backend

/api-client-gen Tasks --overwrite
```

### API Methods Missing
Ensure Response DTOs follow naming convention: `{UseCaseName}Response`

## Best Practices

1. **After every backend change** — Run generation immediately
2. **Commit generated types** — Don't gitignore; track changes
3. **Use strict TypeScript** — Catch API mismatches at compile time
4. **Type-safe API calls** — No `any` types in API calls
5. **Document custom methods** — If adding manual API methods, document them

## Next Steps

1. Implement backend response DTOs
2. Run `/api-client-gen FeatureName`
3. Use generated types in React components
4. TypeScript compiler ensures frontend/backend compatibility
5. Regenerate after any API changes
