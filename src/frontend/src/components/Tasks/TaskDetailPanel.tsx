/**
 * TaskDetailPanel Component
 * Right-sliding side panel displaying complete task details
 * Shows all task attributes in organized sections with inline editing
 * Supports editable fields: name, description, due date, priority, system list, project
 * Auto-saves with optimistic updates and rollback on error (Story 4.4.2)
 */

'use client';

import { useEffect, useState, useRef } from 'react';
import { TodoTask, Priority, TaskStatus, SystemList, ProjectStatus } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useToast } from '@/hooks/useToast';
import { useProjects } from '@/hooks/useProjects';
import { useLabels } from '@/hooks/useLabels';
import { formatSystemList, formatPriority } from '@/utils/enumFormatter';
import ConfirmationModal from '@/components/shared/ConfirmationModal';

interface TaskDetailPanelProps {
  isOpen: boolean;
  taskId: string | null;
  onClose: () => void;
  isArchiveView?: boolean;
  onTaskReopened?: () => void;
  onTaskDeleted?: () => void;
  onTaskUpdated?: () => void;
  onTaskMoved?: (oldList: SystemList, newList: SystemList) => void;
}

export default function TaskDetailPanel({
  isOpen,
  taskId,
  onClose,
  isArchiveView = false,
  onTaskReopened,
  onTaskDeleted,
  onTaskUpdated,
  onTaskMoved,
}: TaskDetailPanelProps) {
  const [task, setTask] = useState<TodoTask | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editingField, setEditingField] = useState<string | null>(null);
  const [editValue, setEditValue] = useState<any>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);
  const { show } = useToast();
  const { projects } = useProjects();
  const { labels: allLabels } = useLabels();

  // Load task data when panel opens or taskId changes
  useEffect(() => {
    if (!isOpen || !taskId) {
      return;
    }

    const fetchTask = async () => {
      setLoading(true);
      setError(null);

      try {
        const { data } = await apiClient.get<TodoTask>(`/tasks/${taskId}`);
        setTask(data);
      } catch (err) {
        const errorMessage =
          err instanceof ApiError ? err.message : 'Failed to load task details';
        setError(errorMessage);
        console.error('Failed to fetch task:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchTask();
  }, [isOpen, taskId]);

  // Handle Escape key to close panel (or cancel edit if in edit mode)
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        if (editingField) {
          // Cancel edit mode without closing panel
          setEditingField(null);
          setEditValue(null);
          e.stopPropagation();
        } else {
          // Close panel if not editing
          onClose();
        }
      }
    };

    if (isOpen) {
      window.addEventListener('keydown', handleEscape);
      return () => window.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, editingField, onClose]);

  // Close on backdrop click
  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  // Handle task delete
  const handleDeleteClick = () => {
    setShowDeleteConfirm(true);
  };

  const handleDeleteConfirm = async () => {
    if (!task || isDeleting) return;

    setIsDeleting(true);

    try {
      await apiClient.delete(`/tasks/${task.id}`);

      show('Task deleted', { type: 'success' });

      // Close confirmation modal
      setShowDeleteConfirm(false);

      // Close panel
      onClose();

      // Notify parent to refresh list
      onTaskDeleted?.();
    } catch (err) {
      console.error('Failed to delete task:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to delete task', { type: 'error' });
      } else {
        show('Failed to delete task', { type: 'error' });
      }
    } finally {
      setIsDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setShowDeleteConfirm(false);
  };

  // Handle task reopen (for archived tasks)
  const handleReopen = async () => {
    if (!task || isSaving) return;

    setIsSaving(true);

    try {
      const { data } = await apiClient.patch<TodoTask>(
        `/tasks/${taskId}/reopen`
      );

      show('Task reopened', { type: 'success' });

      // Notify parent component
      if (onTaskReopened) {
        onTaskReopened();
      }
    } catch (err) {
      const errorMessage =
        err instanceof ApiError ? err.message : 'Failed to reopen task';
      show(errorMessage, { type: 'error' });
      console.error('Failed to reopen task:', err);
    } finally {
      setIsSaving(false);
    }
  };

  // Core field update handler with optimistic updates and rollback
  const handleFieldUpdate = async (field: string, value: any) => {
    if (!task || isSaving || isArchiveView) return;

    // Validate input based on field type
    if (field === 'name' && typeof value === 'string') {
      if (value.trim().length === 0) {
        show('Task name cannot be empty', { type: 'error' });
        return;
      }
      if (value.length > 500) {
        show('Task name must be 500 characters or less', { type: 'error' });
        return;
      }
    }

    if (field === 'description' && typeof value === 'string') {
      if (value.length > 4000) {
        show('Description must be 4000 characters or less', { type: 'error' });
        return;
      }
    }

    // Don't update if value hasn't changed
    if (task[field as keyof TodoTask] === value) {
      setEditingField(null);
      setEditValue(null);
      return;
    }

    // Save original state for rollback
    const originalTask = { ...task };
    const oldSystemList = task.systemList;

    // Optimistic update
    const updatedTask = { ...task, [field]: value };
    setTask(updatedTask);
    setIsSaving(true);

    try {
      // Call API with only the changed field
      const { data } = await apiClient.put<TodoTask>(`/tasks/${taskId}`, {
        [field]: value,
      });

      // Replace with server response
      setTask(data);
      setEditingField(null);
      setEditValue(null);

      // Show success toast - special message for system list changes
      if (field === 'systemList') {
        show(`Task moved to ${formatSystemList(value as SystemList)}`, { type: 'success', duration: 3000 });

        // Notify parent to refresh the list view
        if (onTaskMoved && oldSystemList !== value) {
          onTaskMoved(oldSystemList, value as SystemList);
        }
      } else {
        show('Saved', { type: 'success', duration: 2000 });
        onTaskUpdated?.();
      }
    } catch (err) {
      // Rollback on error
      setTask(originalTask);

      // Show error message
      const errorMessage =
        err instanceof ApiError ? err.message : 'Failed to save changes';
      show(errorMessage, { type: 'error' });
      console.error('Failed to update task field:', field, err);
    } finally {
      setIsSaving(false);
    }
  };

  const handleAssignLabel = async (labelId: string) => {
    if (!task || isSaving || isArchiveView) return;

    const label = allLabels.find((l) => l.id === labelId);
    if (!label) return;

    // Optimistic update
    const originalTask = { ...task, labels: [...(task.labels || [])] };
    setTask({
      ...task,
      labels: [...(task.labels || []), { id: label.id, name: label.name, color: label.color }],
    });
    setIsSaving(true);

    try {
      await apiClient.post(`/tasks/${taskId}/labels/${labelId}`);
      show('Label added', { type: 'success', duration: 2000 });
      onTaskUpdated?.();
    } catch (err) {
      setTask(originalTask);
      const errorMessage =
        err instanceof ApiError ? err.message : 'Failed to add label';
      show(errorMessage, { type: 'error' });
    } finally {
      setIsSaving(false);
    }
  };

  const handleRemoveLabel = async (labelId: string) => {
    if (!task || isSaving || isArchiveView) return;

    // Optimistic update
    const originalTask = { ...task, labels: [...(task.labels || [])] };
    setTask({
      ...task,
      labels: (task.labels || []).filter((l) => l.id !== labelId),
    });
    setIsSaving(true);

    try {
      await apiClient.delete(`/tasks/${taskId}/labels/${labelId}`);
      show('Label removed', { type: 'success', duration: 2000 });
      onTaskUpdated?.();
    } catch (err) {
      setTask(originalTask);
      const errorMessage =
        err instanceof ApiError ? err.message : 'Failed to remove label';
      show(errorMessage, { type: 'error' });
    } finally {
      setIsSaving(false);
    }
  };

  if (!isOpen) {
    return null;
  }

  return (
    <>
      {/* Backdrop - only covers left side (list area) */}
      <div
        className="fixed left-0 top-0 bottom-0 z-30 bg-black/15 transition-opacity"
        style={{ width: 'calc(100% - 24rem)' }}
        onClick={handleBackdropClick}
      />

      {/* Side Panel */}
      <div
        ref={panelRef}
        className="fixed right-0 top-0 bottom-0 z-40 w-96 bg-white shadow-lg transition-transform duration-300 overflow-hidden flex flex-col"
      >
        {/* Header */}
        <div className="flex-shrink-0 border-b border-gray-200 px-6 py-4">
          {/* Reopen button for archived tasks */}
          {isArchiveView && task && (
            <div className="mb-4">
              <button
                onClick={handleReopen}
                disabled={isSaving}
                className="w-full px-4 py-2 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isSaving ? 'Reopening...' : 'Reopen Task'}
              </button>
            </div>
          )}

          <div className="flex items-start justify-between gap-4">
            <div className="flex-1 min-w-0">
              {editingField === 'name' && !isArchiveView ? (
                <input
                  type="text"
                  autoFocus
                  value={editValue}
                  onChange={(e) => setEditValue(e.target.value)}
                  onBlur={() => {
                    if (editValue.trim() !== task?.name?.trim()) {
                      handleFieldUpdate('name', editValue.trim());
                    } else {
                      setEditingField(null);
                      setEditValue(null);
                    }
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.currentTarget.blur();
                    } else if (e.key === 'Escape') {
                      setEditingField(null);
                      setEditValue(null);
                    }
                  }}
                  className="text-xl font-bold text-gray-900 w-full px-2 py-1 rounded-md border border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none"
                  disabled={isSaving}
                />
              ) : (
                <h2
                  onClick={() => {
                    if (!isArchiveView) {
                      setEditingField('name');
                      setEditValue(task?.name || '');
                    }
                  }}
                  className={`text-xl font-bold text-gray-900 break-words ${
                    isArchiveView
                      ? 'line-through text-gray-500'
                      : 'cursor-pointer hover:text-blue-600 transition-colors'
                  }`}
                >
                  {task?.name || 'Loading...'}
                </h2>
              )}
              {editingField === 'name' && (
                <p className="mt-1 text-xs text-gray-500">
                  {editValue.length}/500
                </p>
              )}
              <p className="mt-1 text-xs text-gray-400">{taskId}</p>
            </div>
            <button
              onClick={onClose}
              className="flex-shrink-0 text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Close panel"
            >
              <svg
                className="w-6 h-6"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
          {loading ? (
            <div className="space-y-4">
              {/* Loading skeleton */}
              <div className="h-20 bg-gray-100 rounded animate-pulse" />
              <div className="space-y-2">
                <div className="h-4 bg-gray-100 rounded w-3/4 animate-pulse" />
                <div className="h-4 bg-gray-100 rounded w-1/2 animate-pulse" />
              </div>
              <div className="h-32 bg-gray-100 rounded animate-pulse" />
            </div>
          ) : error ? (
            <div className="rounded-lg bg-red-50 border border-red-200 p-4">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          ) : task ? (
            <div className="space-y-6">
              {/* Description Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-2">
                  Description
                </h3>
                {editingField === 'description' && !isArchiveView ? (
                  <div className="space-y-1">
                    <textarea
                      autoFocus
                      value={editValue}
                      onChange={(e) => setEditValue(e.target.value)}
                      onBlur={() => {
                        if (editValue.trim() !== task.description?.trim()) {
                          handleFieldUpdate('description', editValue.trim());
                        } else {
                          setEditingField(null);
                          setEditValue(null);
                        }
                      }}
                      onKeyDown={(e) => {
                        if (e.key === 'Escape') {
                          setEditingField(null);
                          setEditValue(null);
                        }
                      }}
                      className="w-full px-3 py-2 border rounded-lg text-sm outline-none transition-colors resize-none border-blue-500 focus:ring-2 focus:ring-blue-200"
                      rows={4}
                      maxLength={4000}
                      disabled={isSaving}
                    />
                    <p className="text-xs text-gray-500">
                      {editValue.length}/4000
                    </p>
                  </div>
                ) : (
                  <div
                    onClick={() => {
                      if (!isArchiveView) {
                        setEditingField('description');
                        setEditValue(task.description || '');
                      }
                    }}
                    className={`text-sm text-gray-600 whitespace-pre-wrap break-words p-2 rounded ${
                      isArchiveView
                        ? ''
                        : 'cursor-pointer hover:text-blue-600 transition-colors hover:bg-blue-50'
                    }`}
                  >
                    {task.description || (
                      <span className="text-gray-400 italic">
                        No description
                      </span>
                    )}
                  </div>
                )}
              </div>

              {/* Properties Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3">
                  Properties
                </h3>
                <div className="space-y-3">
                  {/* Due Date */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Due Date:</span>
                    {editingField === 'dueDate' && !isArchiveView ? (
                      <div className="flex gap-2">
                        <input
                          type="date"
                          autoFocus
                          value={editValue || ''}
                          onChange={(e) => setEditValue(e.target.value || null)}
                          onBlur={() => {
                            handleFieldUpdate('dueDate', editValue);
                          }}
                          className="text-sm px-2 py-1 border rounded-md border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none"
                          disabled={isSaving}
                        />
                        {task.dueDate && (
                          <button
                            onClick={() => handleFieldUpdate('dueDate', null)}
                            className="text-xs px-2 py-1 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                            disabled={isSaving}
                          >
                            Clear
                          </button>
                        )}
                      </div>
                    ) : (
                      <span
                        onClick={() => {
                          if (!isArchiveView) {
                            setEditingField('dueDate');
                            setEditValue(
                              task.dueDate
                                ? task.dueDate.split('T')[0]
                                : ''
                            );
                          }
                        }}
                        className={`text-sm font-medium text-gray-900 p-1 rounded ${
                          isArchiveView
                            ? ''
                            : 'cursor-pointer hover:text-blue-600 transition-colors hover:bg-blue-50'
                        }`}
                      >
                        {task.dueDate ? (
                          formatDate(new Date(task.dueDate))
                        ) : (
                          <span className="text-gray-400">No due date</span>
                        )}
                      </span>
                    )}
                  </div>

                  {/* Priority */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Priority:</span>
                    <div className="flex gap-1">
                      {Object.values(Priority).map((p) => (
                        <button
                          key={p}
                          type="button"
                          onClick={() => handleFieldUpdate('priority', task.priority === p ? null : p)}
                          className={`px-2 py-1 rounded text-xs font-semibold transition-colors ${
                            task.priority === p
                              ? getPriorityColor(p) + ' text-white'
                              : 'border border-gray-300 text-gray-700 hover:border-gray-400'
                          }`}
                          disabled={isSaving || isArchiveView}
                        >
                          {formatPriority(p)}
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* System List */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">System List:</span>
                    <select
                      value={task.systemList}
                      onChange={(e) =>
                        handleFieldUpdate('systemList', e.target.value)
                      }
                      className="text-sm px-2 py-1 border border-gray-300 rounded-md outline-none transition-colors focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:opacity-60 disabled:cursor-not-allowed"
                      disabled={isSaving || isArchiveView}
                    >
                      {Object.values(SystemList)
                        .filter((list) => list !== SystemList.Upcoming)
                        .map((list) => (
                          <option key={list} value={list}>
                            {formatSystemList(list)}
                          </option>
                        ))}
                    </select>
                  </div>

                  {/* Project */}
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Project:</span>
                    {isArchiveView ? (
                      <span className="text-sm font-medium text-gray-900 p-1 rounded">
                        {task.projectId ? (
                          <span className="px-2 py-1 bg-blue-50 text-blue-700 rounded text-xs">
                            {task.projectName || 'Unknown project'}
                          </span>
                        ) : (
                          <span className="text-gray-400">None</span>
                        )}
                      </span>
                    ) : (
                      <select
                        value={task.projectId || ''}
                        onChange={(e) =>
                          handleFieldUpdate('projectId', e.target.value || null)
                        }
                        className="text-sm px-2 py-1 border border-gray-300 rounded-md outline-none transition-colors focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:opacity-60 disabled:cursor-not-allowed"
                        disabled={isSaving}
                      >
                        <option value="">None</option>
                        {projects
                          .filter((p) => p.status === ProjectStatus.Active)
                          .map((p) => (
                            <option key={p.id} value={p.id}>
                              {p.name}
                            </option>
                          ))}
                      </select>
                    )}
                  </div>

                  {/* Labels */}
                  <div>
                    <span className="text-sm text-gray-600 block mb-2">
                      Labels:
                    </span>
                    {/* Assigned label chips with remove button */}
                    <div className="flex flex-wrap gap-1 mb-2">
                      {task.labels && task.labels.length > 0 ? (
                        task.labels.map((label) => (
                          <span
                            key={label.id}
                            className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium text-gray-700"
                            style={{
                              backgroundColor: label.color
                                ? `${label.color}20`
                                : '#f3f4f6',
                              borderLeft: `3px solid ${label.color || '#d1d5db'}`,
                            }}
                          >
                            {label.name}
                            {!isArchiveView && (
                              <button
                                type="button"
                                onClick={() => handleRemoveLabel(label.id)}
                                disabled={isSaving}
                                className="ml-0.5 text-gray-400 hover:text-red-500 transition-colors disabled:opacity-50"
                                aria-label={`Remove label ${label.name}`}
                              >
                                &times;
                              </button>
                            )}
                          </span>
                        ))
                      ) : (
                        <span className="text-xs text-gray-400">
                          No labels assigned
                        </span>
                      )}
                    </div>
                    {/* Dropdown to add a label */}
                    {!isArchiveView && (
                      <select
                        value=""
                        onChange={(e) => {
                          if (e.target.value) {
                            handleAssignLabel(e.target.value);
                          }
                        }}
                        className="text-sm px-2 py-1 border border-gray-300 rounded-md outline-none transition-colors focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:opacity-60 disabled:cursor-not-allowed w-full"
                        disabled={isSaving}
                      >
                        <option value="">Add a label...</option>
                        {allLabels
                          .filter(
                            (l) =>
                              !task.labels?.some((tl) => tl.id === l.id)
                          )
                          .map((l) => (
                            <option key={l.id} value={l.id}>
                              {l.name}
                            </option>
                          ))}
                      </select>
                    )}
                  </div>
                </div>
              </div>

              {/* Metadata Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3">
                  Metadata
                </h3>
                <div className="space-y-2 text-xs text-gray-600">
                  <div className="flex items-center justify-between">
                    <span>Created:</span>
                    <span className="font-medium">
                      {formatDateTime(new Date(task.createdAt))}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Updated:</span>
                    <span className="font-medium">
                      {formatDateTime(new Date(task.updatedAt))}
                    </span>
                  </div>
                  {task.completedAt && (
                    <div className="flex items-center justify-between">
                      <span>Completed:</span>
                      <span className="font-medium">
                        {formatDateTime(new Date(task.completedAt))}
                      </span>
                    </div>
                  )}
                  <div className="flex items-center justify-between">
                    <span>Status:</span>
                    <span className="font-medium">
                      {task.status === TaskStatus.Done ? (
                        <span className="text-green-600">Done</span>
                      ) : (
                        <span className="text-blue-600">Open</span>
                      )}
                    </span>
                  </div>
                </div>
              </div>

              {/* Delete Section - only show for non-archived tasks */}
              {!isArchiveView && (
                <div className="mt-8 pt-6 border-t border-gray-200">
                  <button
                    onClick={handleDeleteClick}
                    disabled={isDeleting}
                    className="w-full px-4 py-2 bg-red-600 text-white rounded-lg font-medium hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Delete Task
                  </button>
                  <p className="mt-2 text-xs text-gray-500 text-center">
                    This action cannot be undone
                  </p>
                </div>
              )}
            </div>
          ) : null}
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <ConfirmationModal
        isOpen={showDeleteConfirm}
        title="Delete Task?"
        message="This action cannot be undone. The task will be permanently deleted."
        confirmLabel="Delete"
        cancelLabel="Cancel"
        onConfirm={handleDeleteConfirm}
        onCancel={handleDeleteCancel}
        isDanger={true}
        isLoading={isDeleting}
      />
    </>
  );
}

/**
 * Helper: Get color class for priority badge
 */
function getPriorityColor(priority: Priority): string {
  switch (priority) {
    case Priority.P1:
      return 'bg-red-500';
    case Priority.P2:
      return 'bg-orange-500';
    case Priority.P3:
      return 'bg-blue-500';
    case Priority.P4:
    default:
      return 'bg-gray-400';
  }
}

/**
 * Helper: Format date as "Jan 15, 2026"
 */
function formatDate(date: Date): string {
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

/**
 * Helper: Format datetime as "Jan 15, 2026 at 2:30 PM"
 */
function formatDateTime(date: Date): string {
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}
