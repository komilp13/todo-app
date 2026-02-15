/**
 * TaskRow Component
 * Displays a single task in a list with all relevant information
 * Supports right-click context menu for quick actions (Story 5.5.1)
 */

'use client';

import { useState } from 'react';
import { createPortal } from 'react-dom';
import { TodoTask, TaskStatus } from '@/types';
import {
  formatRelativeDate,
  getPriorityColor,
  isOverdue,
} from '@/utils/dateFormatter';
import { formatSystemList, formatPriority } from '@/utils/enumFormatter';
import TaskContextMenu from '@/components/shared/TaskContextMenu';

interface TaskRowProps {
  task: TodoTask;
  projectName?: string;
  labelNames?: string[];
  labelColors?: Record<string, string>;
  onComplete?: (taskId: string) => void;
  onClick?: (task: TodoTask) => void;
  onTaskMoved?: () => void;
  onTaskDeleted?: () => void;
  isAnimatingOut?: boolean;
  showSystemList?: boolean;
}

export default function TaskRow({
  task,
  projectName,
  labelNames = [],
  labelColors = {},
  onComplete,
  onClick,
  onTaskMoved,
  onTaskDeleted,
  isAnimatingOut = false,
  showSystemList = false,
}: TaskRowProps) {
  const [showContextMenu, setShowContextMenu] = useState(false);
  const [contextMenuPosition, setContextMenuPosition] = useState({ x: 0, y: 0 });

  const handleCheckboxChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    e.stopPropagation();
    if (onComplete) {
      onComplete(task.id);
    }
  };

  const handleContextMenu = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setContextMenuPosition({ x: e.clientX, y: e.clientY });
    setShowContextMenu(true);
  };

  const handleCloseContextMenu = () => {
    setShowContextMenu(false);
  };

  const handleEdit = () => {
    onClick?.(task);
  };

  const priorityColor = getPriorityColor(task.priority);
  const relativeDueDate = formatRelativeDate(task.dueDate);
  const isDueOverdue = isOverdue(task.dueDate);

  return (
    <>
      <div
        onClick={() => onClick?.(task)}
        onContextMenu={handleContextMenu}
        className={`group flex items-start gap-3 rounded-lg border border-transparent px-3 py-2.5 hover:border-gray-200 hover:bg-gray-50 cursor-pointer transition-all ${
          isAnimatingOut
            ? 'animate-fade-slide-out opacity-0'
            : 'opacity-100'
        }`}
      >
      {/* Checkbox */}
      <input
        type="checkbox"
        checked={task.status === TaskStatus.Done}
        onChange={handleCheckboxChange}
        className="mt-1 h-5 w-5 cursor-pointer rounded border-gray-300 text-blue-600 focus:ring-blue-500"
        aria-label={`Complete task: ${task.name}`}
      />

      {/* Task Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline gap-2 flex-wrap">
          {/* Task Name */}
          <p
            className={`text-sm font-medium ${
              task.status === TaskStatus.Done
                ? 'line-through text-gray-400'
                : 'text-gray-900'
            }`}
          >
            {task.name}
          </p>

          {/* Priority Badge */}
          <div
            className="inline-flex items-center h-5 px-2 rounded text-xs font-semibold text-white flex-shrink-0"
            style={{ backgroundColor: priorityColor }}
            title={`Priority: ${formatPriority(task.priority)}`}
          >
            {formatPriority(task.priority)}
          </div>
        </div>

        {/* Metadata row: due date, system list, project, labels */}
        <div className="mt-2 flex flex-wrap items-center gap-2">
          {/* Due Date */}
          {task.dueDate && (
            <span
              className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                isDueOverdue
                  ? 'bg-red-100 text-red-700'
                  : 'bg-gray-100 text-gray-600'
              }`}
            >
              {relativeDueDate}
            </span>
          )}

          {/* System List Badge (for cross-list views like Upcoming) */}
          {showSystemList && (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-purple-50 text-purple-700 border border-purple-200">
              {formatSystemList(task.systemList)}
            </span>
          )}

          {/* Project Chip */}
          {projectName && (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-50 text-blue-700">
              {projectName}
            </span>
          )}

          {/* Label Chips */}
          {labelNames.map((labelName) => (
            <span
              key={labelName}
              className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium text-gray-700"
              style={{
                backgroundColor: labelColors[labelName]
                  ? `${labelColors[labelName]}20`
                  : '#f3f4f6',
                borderLeft: `3px solid ${labelColors[labelName] || '#d1d5db'}`,
              }}
            >
              {labelName}
            </span>
          ))}
        </div>
      </div>
    </div>

    {/* Context Menu - Rendered in portal to avoid drag handler interference */}
    {showContextMenu && typeof window !== 'undefined' && createPortal(
      <TaskContextMenu
        task={task}
        position={contextMenuPosition}
        onClose={handleCloseContextMenu}
        onTaskMoved={onTaskMoved}
        onTaskDeleted={onTaskDeleted}
        onEdit={handleEdit}
      />,
      document.body
    )}
  </>
  );
}
