/**
 * TaskList Component
 * Fetches and displays tasks for a given system list
 * Shows loading skeleton, empty state, and handles task interactions
 * Supports task completion with animation and undo toast
 * Supports drag-and-drop reordering with dnd-kit
 */

'use client';

import { useEffect, useState, useRef, useMemo } from 'react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  TouchSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { TodoTask, SystemList, TaskStatus, Priority } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useToast } from '@/hooks/useToast';
import DraggableTaskRow from './DraggableTaskRow';
import TaskListSkeleton from './TaskListSkeleton';
import QuickAddTaskInput from './QuickAddTaskInput';
import ToastContainer from '../Toast/ToastContainer';

interface TaskListProps {
  systemList: SystemList;
  projectId?: string;
  onTaskClick?: (task: TodoTask) => void;
  onTaskComplete?: (taskId: string) => void;
  onTaskMoved?: () => void;
  onTaskDeleted?: () => void;
  refresh?: number; // Increment to trigger refetch
  emptyMessage?: string; // Custom empty state message
}

interface CompletingTask {
  taskId: string;
  originalTask: TodoTask;
  toastId?: string;
  timeoutId?: NodeJS.Timeout;
}

export default function TaskList({
  systemList,
  projectId,
  onTaskClick,
  onTaskComplete,
  onTaskMoved,
  onTaskDeleted,
  refresh = 0,
  emptyMessage = 'No tasks here. Enjoy your free time! 🎉',
}: TaskListProps) {
  const [tasks, setTasks] = useState<TodoTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [animatingOutTaskIds, setAnimatingOutTaskIds] = useState<Set<string>>(
    new Set()
  );
  const [isReordering, setIsReordering] = useState(false);
  const completingTasksRef = useRef<Map<string, CompletingTask>>(new Map());
  const { toasts, show, dismiss } = useToast();

  // Detect if on mobile (disable drag-drop on mobile or use long-press)
  const [isMobile, setIsMobile] = useState(false);

  // dnd-kit sensors
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(TouchSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const taskIds = useMemo(() => tasks.map((t) => t.id), [tasks]);

  // Detect mobile on mount and window resize
  useEffect(() => {
    const checkMobile = () => {
      setIsMobile(window.innerWidth < 1024);
    };

    checkMobile();
    window.addEventListener('resize', checkMobile);
    return () => window.removeEventListener('resize', checkMobile);
  }, []);

  useEffect(() => {
    const fetchTasks = async () => {
      setLoading(true);
      setError(null);

      try {
        const { data } = await apiClient.get<{
          tasks: TodoTask[];
          totalCount: number;
        }>(`/tasks?systemList=${systemList}`);
        setTasks(data.tasks);
      } catch (err) {
        console.error('Failed to fetch tasks:', err);
        setError('Failed to load tasks. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchTasks();
  }, [systemList, refresh]);

  const handleTaskComplete = async (taskId: string) => {
    const taskIndex = tasks.findIndex((t) => t.id === taskId);
    if (taskIndex === -1) return;

    const originalTask = tasks[taskIndex];
    const optimisticTask: TodoTask = {
      ...originalTask,
      status: TaskStatus.Done,
      isArchived: true,
    };

    // Optimistically update UI
    const newTasks = [...tasks];
    newTasks[taskIndex] = optimisticTask;
    setTasks(newTasks);

    try {
      // Call API to complete task
      const { data } = await apiClient.patch<TodoTask>(
        `/tasks/${taskId}/complete`
      );

      // Update with API response
      const updatedTasks = [...tasks];
      const updatedIndex = updatedTasks.findIndex((t) => t.id === taskId);
      if (updatedIndex !== -1) {
        updatedTasks[updatedIndex] = data;
        setTasks(updatedTasks);
      }

      // Track this task for completion
      const completingTask: CompletingTask = {
        taskId,
        originalTask,
      };
      completingTasksRef.current.set(taskId, completingTask);

      // Show undo toast
      const toastId = show('Task completed', {
        type: 'success',
        duration: 5000,
        action: {
          label: 'Undo',
          onClick: () => handleUndo(taskId),
        },
      });
      completingTask.toastId = toastId;

      // Start animation after 1.5s if not undone
      const timeoutId = setTimeout(() => {
        if (completingTasksRef.current.has(taskId)) {
          setAnimatingOutTaskIds((prev) => new Set(prev).add(taskId));

          // Remove from DOM after animation completes
          setTimeout(() => {
            setTasks((currentTasks) =>
              currentTasks.filter((t) => t.id !== taskId)
            );
            setAnimatingOutTaskIds((prev) => {
              const next = new Set(prev);
              next.delete(taskId);
              return next;
            });
            completingTasksRef.current.delete(taskId);
          }, 300); // Animation duration
        }
      }, 1500);

      completingTask.timeoutId = timeoutId;
    } catch (err) {
      // Revert optimistic update - use current state
      setTasks((currentTasks) => {
        const revertedTasks = [...currentTasks];
        const revertedIndex = revertedTasks.findIndex((t) => t.id === taskId);
        if (revertedIndex !== -1) {
          revertedTasks[revertedIndex] = originalTask;
        }
        return revertedTasks;
      });

      console.error('Failed to complete task:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to complete task', { type: 'error' });
      } else {
        show('Failed to complete task', { type: 'error' });
      }
    }
  };

  const handleUndo = async (taskId: string) => {
    const completingTask = completingTasksRef.current.get(taskId);
    if (!completingTask) return;

    // Clear animation timeout
    if (completingTask.timeoutId) {
      clearTimeout(completingTask.timeoutId);
    }

    // Clear animation state
    setAnimatingOutTaskIds((prev) => {
      const next = new Set(prev);
      next.delete(taskId);
      return next;
    });

    try {
      // Call API to reopen task
      const { data } = await apiClient.patch<TodoTask>(
        `/tasks/${taskId}/reopen`
      );

      // Update task back to original state
      const updatedTasks = [...tasks];
      const updatedIndex = updatedTasks.findIndex((t) => t.id === taskId);
      if (updatedIndex !== -1) {
        updatedTasks[updatedIndex] = data;
      } else {
        // Task might have been removed, add it back
        updatedTasks.push(data);
      }
      setTasks(updatedTasks);

      show('Task restored', { type: 'success' });
    } catch (err) {
      console.error('Failed to undo task completion:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to restore task', { type: 'error' });
      } else {
        show('Failed to restore task', { type: 'error' });
      }
    } finally {
      completingTasksRef.current.delete(taskId);
    }
  };

  const handleTaskMoved = () => {
    // Notify parent component to refresh
    onTaskMoved?.();
  };

  const handleTaskDeleted = () => {
    // Notify parent component to refresh
    onTaskDeleted?.();
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;

    if (!over || active.id === over.id) {
      return;
    }

    // Get the old and new index
    const oldIndex = tasks.findIndex((t) => t.id === active.id);
    const newIndex = tasks.findIndex((t) => t.id === over.id);

    if (oldIndex === -1 || newIndex === -1) {
      return;
    }

    // Optimistically reorder local state
    const reorderedTasks = [...tasks];
    const [movedTask] = reorderedTasks.splice(oldIndex, 1);
    reorderedTasks.splice(newIndex, 0, movedTask);

    setTasks(reorderedTasks);
    setIsReordering(true);

    try {
      // Call API to persist reorder
      const taskIds = reorderedTasks.map((t) => t.id);
      await apiClient.patch('/tasks/reorder', {
        taskIds,
        systemList,
      });

      show('Tasks reordered', { type: 'success' });
    } catch (err) {
      // Rollback on failure
      setTasks(tasks);
      console.error('Failed to reorder tasks:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to reorder tasks', { type: 'error' });
      } else {
        show('Failed to reorder tasks', { type: 'error' });
      }
    } finally {
      setIsReordering(false);
    }
  };

  const handleQuickAddTask = async (taskName: string) => {
    try {
      // Create optimistic task object
      const optimisticTask: TodoTask = {
        id: `temp-${Date.now()}`,
        userId: 'temp',
        name: taskName,
        priority: Priority.P4,
        status: TaskStatus.Open,
        systemList,
        sortOrder: 0,
        isArchived: false,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        projectId: projectId || undefined,
      };

      // Add to top of list optimistically
      setTasks([optimisticTask, ...tasks]);

      // Call API to create task
      const { data: createdTask } = await apiClient.post<TodoTask>(
        '/tasks',
        {
          name: taskName,
          systemList,
          projectId: projectId || undefined,
        }
      );

      // Replace optimistic task with real task
      setTasks((currentTasks) =>
        currentTasks.map((t) =>
          t.id === optimisticTask.id ? createdTask : t
        )
      );

      show('Task created', { type: 'success' });
    } catch (err) {
      // Remove optimistic task on failure
      setTasks((currentTasks) =>
        currentTasks.filter((t) => t.id !== `temp-${Date.now()}`)
      );

      console.error('Failed to create task:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to create task', { type: 'error' });
      } else {
        show('Failed to create task', { type: 'error' });
      }
    }
  };

  if (loading) {
    return <TaskListSkeleton />;
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-center">
        <p className="text-sm text-red-700">{error}</p>
      </div>
    );
  }

  if (tasks.length === 0) {
    return (
      <>
        <QuickAddTaskInput
          systemList={systemList}
          projectId={projectId}
          onTaskCreated={handleQuickAddTask}
          onError={(error) => show(error, { type: 'error' })}
        />
        <div className="rounded-lg border border-gray-200 bg-white p-8 text-center">
          <p className="text-gray-500">{emptyMessage}</p>
          <ToastContainer toasts={toasts} onDismiss={dismiss} />
        </div>
      </>
    );
  }

  return (
    <>
      <QuickAddTaskInput
        systemList={systemList}
        projectId={projectId}
        onTaskCreated={handleQuickAddTask}
        onError={(error) => show(error, { type: 'error' })}
      />

      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragEnd={handleDragEnd}
      >
        <SortableContext
          items={taskIds}
          strategy={verticalListSortingStrategy}
          disabled={isMobile || isReordering}
        >
          <div className="space-y-2">
            {tasks.map((task) => (
              <DraggableTaskRow
                key={task.id}
                task={task}
                onComplete={handleTaskComplete}
                onClick={onTaskClick}
                onTaskMoved={handleTaskMoved}
                onTaskDeleted={handleTaskDeleted}
                isAnimatingOut={animatingOutTaskIds.has(task.id)}
                isDragDisabled={isMobile}
              />
            ))}
          </div>
        </SortableContext>
      </DndContext>
      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </>
  );
}
