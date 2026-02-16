/**
 * TaskContextMenu Component
 * Right-click context menu for task rows
 * Provides quick actions: Move to (system lists), Complete, Delete, Edit
 */

'use client';

import { useState, useEffect, useRef } from 'react';
import { SystemList, TodoTask } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import { useToast } from '@/hooks/useToast';
import { formatSystemList } from '@/utils/enumFormatter';

interface TaskContextMenuProps {
  task: TodoTask;
  position: { x: number; y: number };
  onClose: () => void;
  onTaskMoved?: () => void;
  onDeleteRequest?: () => void;
  onEdit?: () => void;
}

export default function TaskContextMenu({
  task,
  position,
  onClose,
  onTaskMoved,
  onDeleteRequest,
  onEdit,
}: TaskContextMenuProps) {
  const [isMoving, setIsMoving] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const { show } = useToast();

  // Close on outside click or escape
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    };

    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    // Add small delay to prevent immediate close on right-click
    const timer = setTimeout(() => {
      // Use 'click' instead of 'mousedown' to allow onClick handlers to fire first
      document.addEventListener('click', handleClickOutside);
      document.addEventListener('keydown', handleEscape);
    }, 100);

    return () => {
      clearTimeout(timer);
      document.removeEventListener('click', handleClickOutside);
      document.removeEventListener('keydown', handleEscape);
    };
  }, [onClose]);

  // Handle move to different system list
  const handleMoveToList = async (newList: SystemList) => {
    if (isMoving || task.systemList === newList) {
      onClose();
      return;
    }

    setIsMoving(true);

    try {
      await apiClient.put(`/tasks/${task.id}`, {
        systemList: newList,
      });

      show(`Task moved to ${formatSystemList(newList)}`, { type: 'success', duration: 3000 });
      onClose();
      onTaskMoved?.();
    } catch (err) {
      console.error('Failed to move task:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to move task', { type: 'error' });
      } else {
        show('Failed to move task', { type: 'error' });
      }
    } finally {
      setIsMoving(false);
    }
  };

  // Handle complete task
  const handleComplete = async () => {
    if (isMoving) return;

    setIsMoving(true);

    try {
      await apiClient.patch(`/tasks/${task.id}/complete`);

      show('Task completed', { type: 'success' });
      onClose();
      onTaskMoved?.(); // Use the same callback to refresh
    } catch (err) {
      console.error('Failed to complete task:', err);
      if (err instanceof ApiError) {
        show(err.message || 'Failed to complete task', { type: 'error' });
      } else {
        show('Failed to complete task', { type: 'error' });
      }
    } finally {
      setIsMoving(false);
    }
  };

  // Handle delete - close context menu and notify parent to show confirmation
  const handleDeleteClick = () => {
    onClose();
    onDeleteRequest?.();
  };

  // Handle edit - open detail panel
  const handleEdit = () => {
    onEdit?.();
    onClose();
  };

  // Get available system lists (exclude current list and Upcoming, which is a computed view)
  const availableLists = Object.values(SystemList).filter(
    (list) => list !== task.systemList && list !== SystemList.Upcoming
  );

  // Calculate menu position (ensure it stays within viewport)
  const menuStyle: React.CSSProperties = {
    position: 'fixed',
    left: position.x,
    top: position.y,
    zIndex: 50,
  };

  return (
    <div
      ref={menuRef}
      style={menuStyle}
      className="w-56 bg-white rounded-lg shadow-lg border border-gray-200 py-1 text-sm"
      // Stop pointer/mouse events from bubbling through React's tree to dnd-kit listeners.
      // Portal preserves React tree bubbling, so without this, dnd-kit's onPointerDown
      // on the parent DraggableTaskRow intercepts clicks and prevents onClick from firing.
      onPointerDown={(e) => e.stopPropagation()}
      onMouseDown={(e) => e.stopPropagation()}
      onClick={(e) => e.stopPropagation()}
    >
      {/* Move to submenu */}
      <div className="px-3 py-1.5 text-xs font-semibold text-gray-500 uppercase tracking-wide">
        Move to
      </div>
      {availableLists.map((list) => (
        <button
          key={list}
          onClick={() => handleMoveToList(list)}
          disabled={isMoving}
          className="w-full text-left px-3 py-2 hover:bg-gray-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {formatSystemList(list)}
        </button>
      ))}

      {/* Divider */}
      <div className="border-t border-gray-200 my-1" />

      {/* Complete */}
      <button
        onClick={handleComplete}
        disabled={isMoving}
        className="w-full text-left px-3 py-2 hover:bg-gray-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        ✓ Complete
      </button>

      {/* Edit */}
      <button
        onClick={handleEdit}
        disabled={isMoving}
        className="w-full text-left px-3 py-2 hover:bg-gray-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        ✏️ Edit
      </button>

      {/* Divider */}
      <div className="border-t border-gray-200 my-1" />

      {/* Delete */}
      <button
        onClick={handleDeleteClick}
        disabled={isMoving}
        className="w-full text-left px-3 py-2 text-red-600 hover:bg-red-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        🗑️ Delete
      </button>
    </div>
  );
}
