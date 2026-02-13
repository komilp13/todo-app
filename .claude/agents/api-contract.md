# Agent: API Contract

**Subagent Type**: `Explore`

**Purpose**: Review and validate API endpoint design (request/response DTOs, validation, HTTP status codes, consistency).

## When to Use

- Before implementing endpoints
- Designing new features
- Ensuring API consistency
- Reviewing error handling strategies
- Planning API contracts

## Example Prompts

### Example 1: Validate GetTasks Endpoint

```
Review the proposed GetTasks endpoint design for a GTD todo app:

Endpoint: GET /api/tasks

Proposed Request:
- Query parameters: systemList, projectId, labelId, status, archived
- No body

Proposed Response:
```typescript
{
  id: string;
  name: string;
  description?: string;
  dueDate?: string;
  priority: 'P1'|'P2'|'P3'|'P4';
  status: 'Open'|'Done';
  systemList: 'Inbox'|'Next'|'Upcoming'|'Someday';
  projectId?: string;
  isArchived: boolean;
  labels: { id: string; name: string; color?: string }[];
  project?: { id: string; name: string };
  createdAt: string;
  updatedAt: string;
}[]
```

Requirements:
1. Filter by systemList, projectId, labelId
2. Filter by status (Open, Done, All)
3. Filter by archived (true/false)
4. Return only user's own tasks
5. Include related labels and project info
6. Sort by sortOrder ascending

Validation needed:
- Invalid systemList value
- Invalid projectId (not found or wrong user)
- Invalid labelId
- Pagination handling?

Please provide:
1. Complete request/response DTO schema
2. Query parameter validation rules
3. HTTP status codes for all scenarios
4. Error response format
5. Comparison with similar endpoints (consistency)
6. Example curl commands
7. Pagination recommendation (if applicable)
8. Rate limiting considerations
```

### Example 2: Design Complete Task Endpoint

```
Design the CompleteTask endpoint:

Current thinking:
- Endpoint: PATCH /api/tasks/{id}/complete
- No request body
- Success: 200 OK with updated task
- Idempotent (completing already-done task = success)

Questions:
1. Should this endpoint return the full task or just status?
2. How to handle "task not found" vs "unauthorized"?
3. Should it be 200 OK or 204 No Content?
4. Need transaction log entry?
5. Should archive date be separate from completion date?

Provide:
- Recommended HTTP status codes
- Request/response structure
- Error scenarios and codes
- Comparison with Todoist/similar apps
- Best practices for soft-delete patterns
```

### Example 3: Validate Task Creation Endpoint

```
Review CreateTask endpoint consistency:

Proposed:
- POST /api/tasks
- Request body: name, description, dueDate, priority, systemList, projectId
- Response: 201 Created with full task object
- Validation: name required, max 500 chars

Consistency check against:
- UpdateTask (PUT /api/tasks/{id})
- GetTask (GET /api/tasks/{id})
- DeleteTask (DELETE /api/tasks/{id})

Questions:
1. Should response include all fields (especially IDs)?
2. Naming: "systemList" vs "list" vs "category"?
3. Default values: should they be in DTO or set by handler?
4. Field ordering in response (alphabetical vs logical)?
5. Include generated fields (createdAt, updatedAt)?

Check for:
- Consistent field naming across endpoints
- Consistent response structure
- Consistent error handling
- Consistent status codes

Provide:
- Detailed DTO for request
- Detailed DTO for response
- Consistency recommendations
- Suggested curl examples
```

## What to Expect from Agent

1. **Complete DTO Designs**
   ```typescript
   interface GetTasksQuery {
     systemList?: SystemList;
     projectId?: string;
     // ... all parameters
   }

   interface TaskResponse {
     id: string;
     // ... all fields
   }
   ```

2. **Validation Rules**
   ```
   - name: required, 1-500 chars
   - dueDate: optional, must be ISO 8601
   - priority: enum P1|P2|P3|P4
   - projectId: optional, must exist and belong to user
   ```

3. **HTTP Status Codes**
   - 200 OK — Success
   - 201 Created — Resource created
   - 400 Bad Request — Validation error
   - 401 Unauthorized — Not authenticated
   - 403 Forbidden — Not authorized
   - 404 Not Found — Resource not found
   - 409 Conflict — Resource already exists

4. **Error Response Format**
   ```json
   {
     "error": {
       "code": "VALIDATION_ERROR",
       "message": "Validation failed",
       "details": [
         {
           "field": "name",
           "message": "Name is required"
         }
       ],
       "correlationId": "uuid"
     }
   }
   ```

5. **Consistency Analysis**
   - Field naming consistency
   - Response structure patterns
   - Status code consistency
   - Error handling patterns

6. **Example Requests/Responses**
   ```bash
   curl -X GET http://localhost:5000/api/tasks?systemList=Inbox
   # Response: [{ id, name, ... }]
   ```

## Integration Points

After agent review:

1. **Create DTOs**:
   - `Features/{Feature}/{UseCase}/{UseCase}Query.cs`
   - `Features/{Feature}/{UseCase}/{UseCase}Response.cs`

2. **Create Validators**:
   - `Features/{Feature}/{UseCase}/{UseCase}QueryValidator.cs`

3. **Create Controller Endpoint**:
   - `API/Controllers/{Feature}Controller.cs`

4. **Document in CLAUDE.md** if new patterns

## Follow-Up Questions

- "Should pagination be included?"
- "How should sorting work for drag-drop?"
- "Is there a need for bulk operations?"
- "Should filtering support AND vs OR logic?"
- "What's the max response size?"

## Common Patterns to Review

### Filtering
```
?systemList=Inbox&projectId=123&status=Open
```

### Pagination
```
?page=1&pageSize=20&sortBy=dueDate&sortOrder=asc
```

### Error Responses
```json
{
  "code": "RESOURCE_NOT_FOUND",
  "message": "Task with ID ... not found",
  "correlationId": "..."
}
```

### Soft-Delete Queries
```
// Don't expose archived flag to users
GET /api/tasks/archived  // Get completed tasks
GET /api/tasks           // Get active tasks only
```

## Validation Checklist

After design, verify:

- [ ] Consistent field naming (camelCase for API)
- [ ] Consistent response structure
- [ ] All error scenarios documented
- [ ] Proper status codes used
- [ ] Validation rules specified
- [ ] Examples provided
- [ ] Authorization considered
- [ ] Performance implications reviewed
- [ ] Compared with similar endpoints
- [ ] Pagination strategy (if needed)

## Tips for Using This Agent

1. **Provide context** — Mention similar endpoints already designed
2. **Ask about standards** — "What's the industry standard for...?"
3. **Request examples** — "Show me the exact curl command"
4. **Validate assumptions** — "Is this how Todoist does it?"
5. **Consider edge cases** — "What if the user doesn't have permission?"

## Common Topics

- **Soft delete handling** — How to expose archived data
- **Pagination** — Offset, cursor, or none?
- **Filtering combinations** — AND vs OR logic
- **Sorting** — Multiple sort fields?
- **Bulk operations** — Batch create/update/delete?
- **Partial updates** — PATCH vs PUT
- **Versioning** — API versioning strategy
- **Rate limiting** — How to communicate limits
