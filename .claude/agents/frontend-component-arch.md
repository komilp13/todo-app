# Agent: Frontend Component Architecture

**Subagent Type**: `Plan`

**Purpose**: Design component hierarchy, state management strategy, props flow, and interaction patterns for complex UI features.

## When to Use

- Before building major UI features (TaskList, TaskDetail, Sidebar)
- Designing component composition strategy
- Planning state management approach
- Defining component responsibilities
- Planning reusability and composability

## Example Prompts

### Example 1: Design TaskList Component Architecture

```
Design the complete component architecture for TaskList in a GTD todo app:

Requirements:
1. Display tasks in sort order (vertical list)
2. Key info visible per row:
   - Completion checkbox (click to complete with animation)
   - Task name (click to open detail panel)
   - Priority color indicator (P1=red, P2=orange, P3=blue, P4=gray)
   - Due date (relative: Today/Tomorrow/Overdue in red)
   - Project name chip (if assigned)
   - Label chips with colors
3. Quick-add input at top:
   - Placeholder: "Add a task..."
   - Enter creates task with system list context
   - Input auto-focuses after submission
4. Drag-and-drop reordering:
   - Drag handle on each row
   - Visual feedback while dragging
   - Reorder via API on drop
5. Interactions:
   - Checkbox: completion animation → fade out → undo toast (5s)
   - Row click: open detail panel (side panel)
   - Right-click context menu: move, complete, delete, edit
6. Loading states:
   - Skeleton placeholders while fetching
   - Empty state when no tasks
7. Error handling:
   - Show error toast on operation failure
   - Retry mechanism
8. Optimistic UI:
   - Update locally immediately
   - Revert on API error

Deliverables:
1. Component hierarchy diagram
2. Props interface for each component
3. State management strategy (Context, hooks, or Zustand?)
4. Event flow diagram
5. Reusable patterns
6. Code examples for:
   - TaskList component
   - TaskRow component
   - Quick-add input
   - Drag-and-drop integration
7. Custom hooks for:
   - useTaskCompletion()
   - useDragAndDrop()
   - useTaskContextMenu()
8. Error/loading patterns
9. Accessibility considerations
10. Performance optimizations (memoization)

Stack: React/Next.js, TypeScript, Tailwind CSS
```

### Example 2: Design Sidebar Navigation

```
Design sidebar component with:
1. Logo/header section
2. System lists (Inbox, Next, Upcoming, Someday):
   - Icon per list
   - Open task count badge
   - Active state highlight
   - Click navigation
3. Projects section:
   - Collapsible
   - Each project: name, open task count
   - Active projects at top, completed below
   - "+" button to add project
   - Click navigates to project
4. Labels section:
   - Collapsible
   - Each label: color dot, name, task count
   - Alphabetical sort
   - "+" button to add label
   - Click navigates to label view
5. User menu:
   - Avatar with initials
   - Display name and email
   - Dropdown: Profile, Settings, Logout
6. Responsiveness:
   - Desktop: fixed width (280px)
   - Collapse button reduces to icon-only (48px)
   - Mobile: hamburger toggle → slide-in overlay
   - Close on item click (mobile)

Provide:
1. Component structure
2. State management (collapsed/expanded)
3. Data flow from app to sidebar
4. Navigation integration
5. Responsive behavior code
6. Custom hooks (useSidebarCounts, useSidebarNav)
7. Accessibility features
8. Animation/transitions
```

### Example 3: Design Task Detail Panel

```
Design side panel for task editing:

Features:
1. Title: editable task name (click to edit)
2. Description: editable textarea
3. Properties section:
   - Due date: date picker, "Clear" button
   - Priority: P1-P4 selector with colors
   - System list: dropdown, changes task list
   - Project: selector with "None" option
   - Labels: multi-select checkboxes
4. Metadata:
   - Created date
   - Updated date
   - Completed date (if done)
5. Actions:
   - Delete button → confirmation dialog
   - Close button (X, Escape, outside click)
6. Auto-save:
   - Debounced on field changes
   - "Saved" indicator on success
   - Error toast on failure

Interactions:
1. Name: click → edit mode → save on blur/Enter
2. Description: click → edit mode → save on blur
3. Properties: dropdown/selector → auto-save immediately
4. Delete: → confirmation → hard delete

State management:
- How to sync with parent TaskList
- Optimistic updates
- Revert on error
- Locking (prevent edits while saving?)

Provide:
1. Component structure
2. Props interface
3. State machine for edit modes
4. Auto-save debounce strategy
5. Error handling
6. Confirmation dialog component
7. Keyboard navigation (Escape to close)
8. Responsive design (mobile?)
```

## What to Expect from Agent

1. **Component Hierarchy**
   ```
   TaskList
   ├── QuickAddInput
   ├── TaskList (virtualized if many tasks)
   │   └── TaskRow
   │       ├── Checkbox
   │       ├── TaskName
   │       ├── PriorityIndicator
   │       ├── DueDate
   │       ├── ProjectChip
   │       └── LabelChips
   └── EmptyState / LoadingSkeleton
   ```

2. **Props Interfaces**
   ```typescript
   interface TaskListProps {
     systemList: SystemList;
     tasks: TaskResponse[];
     isLoading: boolean;
     onTaskClick: (taskId: string) => void;
     onComplete: (taskId: string) => Promise<void>;
     onReorder: (taskIds: string[]) => Promise<void>;
   }
   ```

3. **State Management Strategy**
   ```
   - Use Context for global: (user auth, theme)
   - Use local state for: (expanded/collapsed, form inputs)
   - Use React Query for: (fetching tasks, caching)
   - Use Zustand if needed for: (complex cross-component state)
   ```

4. **Custom Hooks**
   ```typescript
   const useTaskCompletion = () => { ... }
   const useDragAndDrop = () => { ... }
   const useTaskContextMenu = () => { ... }
   ```

5. **Event Flow Diagram**
   ```
   User clicks checkbox
     ↓
   Component optimistically updates UI
     ↓
   API call completes endpoint
     ↓
   On success: toast + fade out animation
     ↓
   On error: revert UI + error toast
   ```

6. **Code Examples**
   ```typescript
   export function TaskList({ systemList }: TaskListProps) {
     const { tasks, isLoading } = useTasksQuery(systemList);
     const { mutate: reorder } = useReorderTasks();

     return (
       <div>
         <QuickAddInput systemList={systemList} />
         {isLoading ? <SkeletonList /> : <DragDropList tasks={tasks} />}
       </div>
     );
   }
   ```

7. **Performance Optimizations**
   ```
   - React.memo() for TaskRow
   - useMemo() for expensive computations
   - useCallback() for event handlers
   - Virtualization for large lists
   - Code splitting for detail panel
   ```

8. **Accessibility**
   - ARIA labels
   - Keyboard navigation
   - Focus management
   - Screen reader friendly

## Integration Points

After design:

1. **Create components** following hierarchy
2. **Setup state management** (Context/hooks/Zustand)
3. **Implement interactions** with optimistic UI
4. **Add tests** with React Testing Library
5. **Performance tune** if needed

## Common Patterns

### Optimistic UI
```typescript
const handleComplete = async (taskId: string) => {
  // Optimistically update
  setTasks(t => t.map(task =>
    task.id === taskId ? { ...task, isCompleted: true } : task
  ));

  try {
    await completeTaskApi(taskId);
    showToast("Task completed");
  } catch (error) {
    // Revert on error
    setTasks(originalTasks);
    showError("Failed to complete task");
  }
};
```

### Custom Hook for Data Fetching
```typescript
const useTasksQuery = (systemList: SystemList) => {
  const [tasks, setTasks] = useState([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const fetchTasks = async () => {
      setIsLoading(true);
      const data = await tasksApi.list({ systemList });
      setTasks(data);
      setIsLoading(false);
    };
    fetchTasks();
  }, [systemList]);

  return { tasks, isLoading };
};
```

### Context for Global State
```typescript
const TaskContext = createContext<TaskContextType>(null!);

export function TaskProvider({ children }: { children: ReactNode }) {
  const [selectedTask, setSelectedTask] = useState<string | null>(null);

  return (
    <TaskContext.Provider value={{ selectedTask, setSelectedTask }}>
      {children}
    </TaskContext.Provider>
  );
}

export const useTaskContext = () => useContext(TaskContext);
```

## State Management Options

### Option 1: Context + Local State
- Simple, built-in
- Good for small apps
- Less boilerplate

### Option 2: React Query + Context
- Query: data fetching, caching
- Context: global app state
- Recommended for this project

### Option 3: Zustand
- Simple Redux alternative
- Less boilerplate than Redux
- Good for complex state

## Mobile Responsiveness

Consider mobile-first design:
- Smaller Task detail panel (full width on mobile)
- Touch-friendly sizes (44px min tap target)
- No hover states (show on focus instead)
- Bottom sheet for modals instead of side panels

## Testing Strategy

For components:
```typescript
describe('TaskList', () => {
  it('displays tasks in order', () => {
    render(<TaskList systemList="Inbox" />);
    // Assert tasks appear
  });

  it('completes task on checkbox click', async () => {
    render(<TaskList ... />);
    // Click checkbox
    // Assert optimistic update
    // Assert API called
  });
});
```

## Follow-Up Questions

- "Should detail panel be separate route or modal/panel?"
- "Mobile-first design approach?"
- "How to handle real-time updates (WebSocket)?"
- "Pagination vs infinite scroll for large lists?"
- "Should we use React Query for caching?"

## Tips for Using This Agent

1. **Provide mockups** if you have them
2. **Be specific about interactions** — Important for UX
3. **Mention constraints** — Mobile, accessibility, performance
4. **Ask for examples** — Want real component code
5. **Request patterns** — "How to handle loading states?"

## Performance Considerations

- Virtualization for 100+ items
- Code splitting for modals/panels
- Lazy load images
- Memo components that don't change often
- Debounce API calls (auto-save)

## Accessibility Checklist

- [ ] Keyboard navigation (Tab, Enter, Escape)
- [ ] ARIA labels for buttons/sections
- [ ] Focus indicators visible
- [ ] Color not only indicator
- [ ] Screen reader friendly
- [ ] Min 44px tap targets (mobile)
