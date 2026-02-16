/**
 * TaskCreateModal Component
 * Full-featured task creation form in a modal dialog
 * Supports: name, description, due date, priority, system list, project, labels
 * Opens via button or "Q" keyboard shortcut
 * Optimistic UI with error handling
 */

'use client';

import { useState, useEffect, useRef } from 'react';
import { SystemList, Priority, TodoTask } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useTaskRefreshContext } from '@/contexts/TaskRefreshContext';

interface TaskCreateModalProps {
  isOpen: boolean;
  onClose: () => void;
  onTaskCreated?: (task: TodoTask) => void;
  defaultSystemList?: SystemList;
  defaultProjectId?: string;
  projects?: Array<{ id: string; name: string }>;
  labels?: Array<{ id: string; name: string; color?: string }>;
}

interface FormData {
  name: string;
  description: string;
  dueDate: string;
  priority: Priority | null;
  systemList: SystemList;
  projectId: string;
  labelIds: string[];
}

const INITIAL_FORM_DATA: FormData = {
  name: '',
  description: '',
  dueDate: '',
  priority: null,
  systemList: SystemList.Inbox,
  projectId: '',
  labelIds: [],
};

export default function TaskCreateModal({
  isOpen,
  onClose,
  onTaskCreated,
  defaultSystemList = SystemList.Inbox,
  defaultProjectId = '',
  projects = [],
  labels = [],
}: TaskCreateModalProps) {
  const [formData, setFormData] = useState<FormData>({
    ...INITIAL_FORM_DATA,
    systemList: defaultSystemList,
    projectId: defaultProjectId,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [submitError, setSubmitError] = useState<string>('');

  const { triggerRefresh } = useTaskRefreshContext();

  const modalRef = useRef<HTMLDivElement>(null);
  const nameInputRef = useRef<HTMLInputElement>(null);

  // Focus name input when modal opens
  useEffect(() => {
    if (isOpen) {
      setTimeout(() => nameInputRef.current?.focus(), 0);
      setSubmitError('');
    }
  }, [isOpen]);

  // Close modal on Escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };

    if (isOpen) {
      window.addEventListener('keydown', handleEscape);
      return () => window.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, onClose]);

  // Close on backdrop click
  const handleBackdropClick = (
    e: React.MouseEvent<HTMLDivElement>
  ) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Task name is required';
    } else if (formData.name.length > 500) {
      newErrors.name = 'Task name must be 500 characters or less';
    }

    if (formData.description.length > 4000) {
      newErrors.description = 'Description must be 4000 characters or less';
    }

    if (formData.dueDate) {
      const dueDate = new Date(formData.dueDate);
      if (isNaN(dueDate.getTime())) {
        newErrors.dueDate = 'Invalid date format';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle input changes
  const handleInputChange = (
    e: React.ChangeEvent<
      HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
    >
  ) => {
    const { name, value } = e.target;

    if (name === 'labelIds') {
      return;
    }

    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Clear error for this field
    if (errors[name]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[name];
        return newErrors;
      });
    }
  };

  // Handle label selection
  const handleLabelToggle = (labelId: string) => {
    setFormData((prev) => ({
      ...prev,
      labelIds: prev.labelIds.includes(labelId)
        ? prev.labelIds.filter((id) => id !== labelId)
        : [...prev.labelIds, labelId],
    }));
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);
    setSubmitError('');

    try {
      // Prepare request payload
      const taskPayload = {
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        dueDate: formData.dueDate || undefined,
        priority: formData.priority || undefined,
        systemList: formData.systemList,
        projectId: formData.projectId || undefined,
      };

      // Create task via API
      const { data: createdTask } = await apiClient.post<TodoTask>(
        '/tasks',
        taskPayload
      );

      // Assign labels if selected
      if (formData.labelIds.length > 0) {
        try {
          for (const labelId of formData.labelIds) {
            await apiClient.post(`/tasks/${createdTask.id}/labels/${labelId}`);
          }
        } catch (labelError) {
          console.warn('Failed to assign some labels:', labelError);
        }
      }

      // Reset form and close modal
      setFormData({
        ...INITIAL_FORM_DATA,
        systemList: defaultSystemList,
        projectId: defaultProjectId,
      });
      setErrors({});
      setSubmitError('');

      // Notify parent component
      onTaskCreated?.(createdTask);

      // Trigger refresh on all task list pages
      await triggerRefresh();

      // Close modal
      onClose();
    } catch (err) {
      const errorMessage =
        err instanceof ApiError ? err.message : 'Failed to create task';
      setSubmitError(errorMessage);
      console.error('Failed to create task:', err);
    } finally {
      setIsLoading(false);
    }
  };

  // Handle form reset
  const handleCancel = () => {
    setFormData({
      ...INITIAL_FORM_DATA,
      systemList: defaultSystemList,
      projectId: defaultProjectId,
    });
    setErrors({});
    setSubmitError('');
    onClose();
  };

  if (!isOpen) {
    return null;
  }

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-40 bg-black/15 transition-opacity"
        onClick={handleBackdropClick}
      />

      {/* Modal Dialog */}
      <div
        ref={modalRef}
        className="fixed inset-0 z-50 flex items-center justify-center p-4"
      >
        <div className="w-full max-w-md rounded-lg bg-white shadow-lg">
          {/* Header */}
          <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
            <h2 className="text-lg font-semibold text-gray-900">
              Create New Task
            </h2>
            <button
              onClick={handleCancel}
              className="text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Close modal"
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

          {/* Form */}
          <form onSubmit={handleSubmit} className="px-6 py-4">
            {/* Submit Error Alert */}
            {submitError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-700">{submitError}</p>
              </div>
            )}

            {/* Task Name (Required) */}
            <div className="mb-4">
              <label
                htmlFor="name"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Task Name <span className="text-red-500">*</span>
              </label>
              <input
                ref={nameInputRef}
                id="name"
                name="name"
                type="text"
                value={formData.name}
                onChange={handleInputChange}
                placeholder="What do you need to do?"
                disabled={isLoading}
                maxLength={500}
                className={`w-full px-3 py-2 border rounded-lg text-sm font-medium outline-none transition-colors ${
                  errors.name
                    ? 'border-red-500 focus:ring-red-200'
                    : 'border-gray-200 focus:border-blue-500 focus:ring-2 focus:ring-blue-100'
                } disabled:bg-gray-50 disabled:text-gray-400`}
                aria-invalid={!!errors.name}
                aria-describedby={errors.name ? 'name-error' : undefined}
              />
              {errors.name && (
                <p id="name-error" className="mt-1 text-xs text-red-500">
                  {errors.name}
                </p>
              )}
              <p className="mt-1 text-xs text-gray-400">
                {formData.name.length}/500
              </p>
            </div>

            {/* Description */}
            <div className="mb-4">
              <label
                htmlFor="description"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Description
              </label>
              <textarea
                id="description"
                name="description"
                value={formData.description}
                onChange={handleInputChange}
                placeholder="Add more details..."
                disabled={isLoading}
                maxLength={4000}
                rows={3}
                className={`w-full px-3 py-2 border rounded-lg text-sm outline-none transition-colors resize-none ${
                  errors.description
                    ? 'border-red-500 focus:ring-red-200'
                    : 'border-gray-200 focus:border-blue-500 focus:ring-2 focus:ring-blue-100'
                } disabled:bg-gray-50 disabled:text-gray-400`}
                aria-invalid={!!errors.description}
                aria-describedby={
                  errors.description ? 'description-error' : undefined
                }
              />
              {errors.description && (
                <p
                  id="description-error"
                  className="mt-1 text-xs text-red-500"
                >
                  {errors.description}
                </p>
              )}
              <p className="mt-1 text-xs text-gray-400">
                {formData.description.length}/4000
              </p>
            </div>

            {/* Due Date */}
            <div className="mb-4">
              <label
                htmlFor="dueDate"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                Due Date
              </label>
              <input
                id="dueDate"
                name="dueDate"
                type="date"
                value={formData.dueDate}
                onChange={handleInputChange}
                disabled={isLoading}
                className={`w-full px-3 py-2 border rounded-lg text-sm outline-none transition-colors ${
                  errors.dueDate
                    ? 'border-red-500 focus:ring-red-200'
                    : 'border-gray-200 focus:border-blue-500 focus:ring-2 focus:ring-blue-100'
                } disabled:bg-gray-50 disabled:text-gray-400`}
                aria-invalid={!!errors.dueDate}
                aria-describedby={errors.dueDate ? 'dueDate-error' : undefined}
              />
              {errors.dueDate && (
                <p id="dueDate-error" className="mt-1 text-xs text-red-500">
                  {errors.dueDate}
                </p>
              )}
            </div>

            {/* Priority */}
            <div className="mb-4">
              <label
                htmlFor="priority"
                className="block text-sm font-medium text-gray-700 mb-2"
              >
                Priority
              </label>
              <div className="flex gap-2">
                {Object.values(Priority).map((p) => (
                  <button
                    key={p}
                    type="button"
                    onClick={() => setFormData((prev) => ({ ...prev, priority: prev.priority === p ? null : p }))}
                    disabled={isLoading}
                    className={`flex-1 py-2 px-3 rounded-lg text-sm font-medium transition-colors ${
                      formData.priority === p
                        ? getPriorityColor(p) + ' text-white'
                        : 'border-2 border-gray-200 text-gray-700 hover:border-gray-300'
                    } disabled:opacity-50`}
                  >
                    {p}
                  </button>
                ))}
              </div>
            </div>

            {/* System List */}
            <div className="mb-4">
              <label
                htmlFor="systemList"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                System List
              </label>
              <select
                id="systemList"
                name="systemList"
                value={formData.systemList}
                onChange={handleInputChange}
                disabled={isLoading}
                className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none transition-colors focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:bg-gray-50 disabled:text-gray-400"
              >
                {Object.values(SystemList).map((list) => (
                  <option key={list} value={list}>
                    {list}
                  </option>
                ))}
              </select>
            </div>

            {/* Project */}
            {projects.length > 0 && (
              <div className="mb-4">
                <label
                  htmlFor="projectId"
                  className="block text-sm font-medium text-gray-700 mb-1"
                >
                  Project
                </label>
                <select
                  id="projectId"
                  name="projectId"
                  value={formData.projectId}
                  onChange={handleInputChange}
                  disabled={isLoading}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm outline-none transition-colors focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:bg-gray-50 disabled:text-gray-400"
                >
                  <option value="">No project</option>
                  {projects.map((project) => (
                    <option key={project.id} value={project.id}>
                      {project.name}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {/* Labels */}
            {labels.length > 0 && (
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Labels
                </label>
                <div className="flex flex-wrap gap-2">
                  {labels.map((label) => (
                    <button
                      key={label.id}
                      type="button"
                      onClick={() => handleLabelToggle(label.id)}
                      disabled={isLoading}
                      className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
                        formData.labelIds.includes(label.id)
                          ? 'bg-blue-100 text-blue-700'
                          : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                      } disabled:opacity-50`}
                    >
                      {label.color && (
                        <span
                          className="inline-block w-2 h-2 rounded-full mr-1"
                          style={{ backgroundColor: label.color }}
                        />
                      )}
                      {label.name}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Form Actions */}
            <div className="flex gap-3 pt-4 border-t border-gray-200">
              <button
                type="button"
                onClick={handleCancel}
                disabled={isLoading}
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isLoading}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors disabled:opacity-50 flex items-center justify-center gap-2"
              >
                {isLoading && (
                  <svg
                    className="w-4 h-4 animate-spin"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    />
                  </svg>
                )}
                {isLoading ? 'Creating...' : 'Create Task'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}

/**
 * Get color class for priority level
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
