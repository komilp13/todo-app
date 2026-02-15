/**
 * QuickAddTaskInput Component
 * Inline task creation at the top of task lists
 * Shows "+" icon with input field, Enter to submit
 * Optimistic UI - new task appears immediately
 * Input stays focused for rapid entry
 */

'use client';

import { useState, useRef, useEffect } from 'react';
import { SystemList, Priority } from '@/types';

interface QuickAddTaskInputProps {
  systemList: SystemList;
  projectId?: string;
  onTaskCreated?: (taskName: string) => void;
  onError?: (error: string) => void;
}

export default function QuickAddTaskInput({
  systemList,
  projectId,
  onTaskCreated,
  onError,
}: QuickAddTaskInputProps) {
  const [taskName, setTaskName] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  // Auto-focus input on mount
  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Trim and validate input
    const trimmedName = taskName.trim();
    if (!trimmedName) {
      onError?.('Task name cannot be empty');
      return;
    }

    setIsLoading(true);

    try {
      // Notify parent to create task (parent will handle API call)
      onTaskCreated?.(trimmedName);

      // Clear input and keep focus for rapid entry
      setTaskName('');
      inputRef.current?.focus();
    } catch (err) {
      console.error('Failed to create task:', err);
      onError?.(
        err instanceof Error ? err.message : 'Failed to create task'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    // Submit on Enter
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e as unknown as React.FormEvent);
    }

    // Clear on Escape
    if (e.key === 'Escape') {
      setTaskName('');
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="mb-4 flex items-center gap-2 rounded-lg border border-gray-200 bg-white px-3 py-2.5 hover:border-gray-300 transition-colors"
    >
      {/* Plus Icon */}
      <svg
        className="w-5 h-5 text-gray-400 flex-shrink-0"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M12 4v16m8-8H4"
        />
      </svg>

      {/* Input Field */}
      <input
        ref={inputRef}
        type="text"
        value={taskName}
        onChange={(e) => setTaskName(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder="Add a task..."
        disabled={isLoading}
        className="flex-1 bg-transparent text-sm font-medium text-gray-900 placeholder-gray-400 outline-none disabled:text-gray-400"
        aria-label="Quick add task input"
      />

      {/* Optional: Submit Button (for accessibility) */}
      <button
        type="submit"
        disabled={isLoading || !taskName.trim()}
        className="px-2 py-1 text-xs font-medium text-gray-500 hover:text-blue-600 disabled:text-gray-300 transition-colors"
        aria-label="Create task"
      >
        {isLoading ? (
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
        ) : (
          'Add'
        )}
      </button>
    </form>
  );
}
