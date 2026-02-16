/**
 * ArchiveTaskRow Component
 * Displays a completed/archived task with:
 * - Strikethrough styling
 * - Completion date ("Completed 2 days ago")
 * - Original system list badge
 * - No checkbox (or disabled checked checkbox)
 */

import { TodoTask } from '@/types';
import { getPriorityColor } from '@/utils/dateFormatter';
import { formatSystemList, formatPriority } from '@/utils/enumFormatter';

interface ArchiveTaskRowProps {
  task: TodoTask;
  onClick?: (task: TodoTask) => void;
}

export default function ArchiveTaskRow({ task, onClick }: ArchiveTaskRowProps) {
  const priorityColor = task.priority ? getPriorityColor(task.priority) : '';

  // Format completion date
  const formatCompletionDate = (dateString: string | undefined): string => {
    if (!dateString) return 'Completed';

    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffMinutes = Math.floor(diffMs / (1000 * 60));

    if (diffMinutes < 1) {
      return 'Completed just now';
    } else if (diffMinutes < 60) {
      return `Completed ${diffMinutes} minute${diffMinutes !== 1 ? 's' : ''} ago`;
    } else if (diffHours < 24) {
      return `Completed ${diffHours} hour${diffHours !== 1 ? 's' : ''} ago`;
    } else if (diffDays === 0) {
      return 'Completed today';
    } else if (diffDays === 1) {
      return 'Completed yesterday';
    } else if (diffDays < 7) {
      return `Completed ${diffDays} days ago`;
    } else if (diffDays < 30) {
      const weeks = Math.floor(diffDays / 7);
      return `Completed ${weeks} week${weeks !== 1 ? 's' : ''} ago`;
    } else if (diffDays < 365) {
      const months = Math.floor(diffDays / 30);
      return `Completed ${months} month${months !== 1 ? 's' : ''} ago`;
    } else {
      const years = Math.floor(diffDays / 365);
      return `Completed ${years} year${years !== 1 ? 's' : ''} ago`;
    }
  };

  const completionText = formatCompletionDate(task.completedAt);

  return (
    <div
      onClick={() => onClick?.(task)}
      className="group flex items-start gap-3 rounded-lg border border-transparent px-3 py-2.5 hover:border-gray-200 hover:bg-gray-50 cursor-pointer transition-all"
    >
      {/* Checked Checkbox (disabled) */}
      <input
        type="checkbox"
        checked={true}
        disabled={true}
        className="mt-1 h-5 w-5 rounded border-gray-300 text-green-600 opacity-60 cursor-pointer"
        aria-label={`Completed task: ${task.name}`}
      />

      {/* Task Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline gap-2 flex-wrap">
          {/* Task Name (always strikethrough) */}
          <p className="text-sm font-medium text-gray-400 line-through">
            {task.name}
          </p>

          {/* Priority Badge (only shown when priority is set) */}
          {task.priority && (
            <div
              className="inline-flex items-center h-5 px-2 rounded text-xs font-semibold text-white flex-shrink-0 opacity-60"
              style={{ backgroundColor: priorityColor }}
              title={`Priority: ${formatPriority(task.priority)}`}
            >
              {formatPriority(task.priority)}
            </div>
          )}
        </div>

        {/* Metadata row: completion date, original system list */}
        <div className="mt-2 flex flex-wrap items-center gap-2">
          {/* Completion Date */}
          <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-700">
            {completionText}
          </span>

          {/* Original System List Badge */}
          <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">
            {formatSystemList(task.systemList)}
          </span>
        </div>
      </div>
    </div>
  );
}
