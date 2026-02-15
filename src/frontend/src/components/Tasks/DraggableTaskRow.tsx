/**
 * DraggableTaskRow Component
 * Wraps TaskRow with drag-and-drop functionality using dnd-kit
 * Provides drag handle, visual feedback, and drop indicator
 */

import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { TodoTask } from '@/types';
import TaskRow from './TaskRow';

interface DraggableTaskRowProps {
  task: TodoTask;
  projectName?: string;
  labelNames?: string[];
  labelColors?: Record<string, string>;
  onComplete?: (taskId: string) => void;
  onClick?: (task: TodoTask) => void;
  isAnimatingOut?: boolean;
  isDragDisabled?: boolean;
}

export default function DraggableTaskRow({
  task,
  projectName,
  labelNames,
  labelColors,
  onComplete,
  onClick,
  isAnimatingOut = false,
  isDragDisabled = false,
}: DraggableTaskRowProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
    isOver,
  } = useSortable({
    id: task.id,
    disabled: isDragDisabled,
    animateLayoutChanges: () => true,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 1000 : 'auto',
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`relative group ${
        isDragging ? 'opacity-50' : ''
      } ${isOver ? 'bg-blue-50 rounded-lg' : ''}`}
    >
      {/* Drop indicator line */}
      {isOver && (
        <div className="absolute -top-0.5 left-0 right-0 h-0.5 bg-blue-400"></div>
      )}

      <div className="flex gap-2 items-start">
        {/* Drag Handle */}
        {!isDragDisabled && (
          <div
            {...attributes}
            {...listeners}
            className="flex items-center justify-center w-5 h-full cursor-grab active:cursor-grabbing opacity-0 group-hover:opacity-100 transition-opacity"
            title="Drag to reorder"
          >
            <svg
              className="w-4 h-4 text-gray-400"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" />
            </svg>
          </div>
        )}

        {/* Task Content */}
        <div className="flex-1">
          <TaskRow
            task={task}
            projectName={projectName}
            labelNames={labelNames}
            labelColors={labelColors}
            onComplete={onComplete}
            onClick={onClick}
            isAnimatingOut={isAnimatingOut}
          />
        </div>
      </div>
    </div>
  );
}
