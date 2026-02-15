# Task Refresh Integration Guide

## Overview

When a new task is created via the TaskCreateModal (button or "Q" key), all registered task list pages automatically refresh their task lists without a full page reload.

This is achieved through the **TaskRefreshContext** which maintains a registry of refresh callbacks from each page.

---

## How It Works

1. **TaskRefreshContext** (global context)
   - Manages a registry of refresh callbacks
   - Each page registers its refresh function on mount
   - When a task is created, `triggerRefresh()` calls all registered callbacks

2. **useTaskRefresh** hook
   - Pages use this hook to register their refresh callback
   - Automatically unregisters when the page unmounts

3. **TaskCreateModal**
   - After successfully creating a task, calls `triggerRefresh()`
   - All registered pages refresh simultaneously

---

## Integration Example

### Inbox Page (or any task list page)

```typescript
// src/app/inbox/page.tsx

'use client';

import { useState, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query'; // or use your own fetch
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import TaskList from '@/components/Tasks/TaskList';

export default function InboxPage() {
  // Your existing query setup
  const { data: tasks, refetch } = useQuery({
    queryKey: ['tasks', 'inbox'],
    queryFn: async () => {
      const response = await fetch('/api/tasks?systemList=Inbox');
      return response.json();
    },
  });

  // Register this page's refresh callback
  useTaskRefresh('inbox', () => refetch());

  return (
    <div>
      <h1>Inbox</h1>
      <TaskList tasks={tasks} />
    </div>
  );
}
```

### Without React Query (vanilla fetch)

```typescript
'use client';

import { useState, useCallback } from 'react';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import TaskList from '@/components/Tasks/TaskList';

export default function NextPage() {
  const [tasks, setTasks] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  // Function to fetch tasks
  const fetchTasks = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await fetch('/api/tasks?systemList=Next');
      const data = await response.json();
      setTasks(data);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Register this page's refresh callback
  useTaskRefresh('next', fetchTasks);

  // Initial fetch on mount
  useEffect(() => {
    fetchTasks();
  }, [fetchTasks]);

  return (
    <div>
      <h1>Next</h1>
      <TaskList tasks={tasks} isLoading={isLoading} />
    </div>
  );
}
```

---

## Hook Signature

```typescript
useTaskRefresh(
  pageId: string,
  refreshCallback: () => void | Promise<void>
): void
```

### Parameters

- **pageId** (string): Unique identifier for the page (e.g., 'inbox', 'next', 'upcoming')
  - Used to manage the callback registry
  - Important: Should be unique per page to avoid conflicts

- **refreshCallback** (function): Function that refetches/refreshes the task list
  - Can be sync (`() => void`) or async (`() => Promise<void>`)
  - Called when a task is created anywhere in the app
  - Should refetch tasks from the API

### Returns
- `void` — Hook handles registration/unregistration automatically

---

## Key Features

✅ **Automatic Cleanup** — Callback unregisters when component unmounts
✅ **Async Support** — Callbacks can be async, all execute in parallel
✅ **Error Handling** — Errors in one callback don't break others
✅ **Zero Config** — Just call the hook with a callback
✅ **Multiple Pages** — Works across all open pages simultaneously

---

## Example Workflow

1. User is on the Inbox page (Inbox page registers `refetchTasks`)
2. User presses "Q" or clicks "New Task" button
3. TaskCreateModal opens
4. User creates a task and submits
5. Modal calls `triggerRefresh()`
6. All registered pages' refresh callbacks execute
7. Inbox page refetches tasks and displays the new task
8. Modal closes

---

## Pages That Need Integration

These pages should implement `useTaskRefresh` to automatically refresh when tasks are created:

- [ ] Inbox (`src/app/inbox/page.tsx`)
- [ ] Next (`src/app/next/page.tsx`)
- [ ] Upcoming (`src/app/upcoming/page.tsx`)
- [ ] Someday (`src/app/someday/page.tsx`)
- [ ] Any future project/label filtered views

---

## Testing

To test the integration:

1. Start the app: `npm run dev`
2. Navigate to a task list page (e.g., Inbox)
3. Open the task creation modal (Q key or New Task button)
4. Create a new task
5. Verify the new task appears in the list without a page reload
6. Open a second page (e.g., Next) in another browser tab
7. Create a task in the modal again
8. Both pages should refresh simultaneously

---

## Troubleshooting

### Tasks don't refresh after creation

1. Check that `useTaskRefresh` is called in the page component
2. Verify the refresh callback is working (test it manually)
3. Confirm TaskRefreshProvider is in the layout hierarchy
4. Check browser console for errors

### "useTaskRefreshContext must be used within TaskRefreshProvider"

- Ensure layout.tsx includes `<TaskRefreshProvider>` wrapper
- Check that page component is rendered within the provider

### Multiple registrations for same pageId

- Ensure each page uses a unique pageId
- If component re-renders, the hook handles cleanup automatically

---

## Implementation Status

- [x] TaskRefreshContext created
- [x] useTaskRefresh hook created
- [x] TaskRefreshProvider added to layout
- [x] TaskCreateModal integrated with triggerRefresh
- [ ] Pages integrated (Inbox, Next, Upcoming, Someday)
