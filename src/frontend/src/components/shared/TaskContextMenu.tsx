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
import ConfirmationModal from './ConfirmationModal';

interface TaskContextMenuProps {
  task: TodoTask;
  position: { x: number; y: number };
  onClose: () => void;
  onTaskMoved?: () => void;
  onTaskDeleted?: () => void;
  onEdit?: () => void;
}

export default function TaskContextMenu({
  task,
  position,
  onClose,
  onTaskMoved,
  onTaskDeleted,
  onEdit,
}: TaskContextMenuProps) {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
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
      document.addEventListener('mousedown', handleClickOutside);
      document.addEventListener('keydown', handleEscape);
    }, 100);

    return () => {
      clearTimeout(timer);
      document.removeEventListener('mousedown', handleClickOutside);
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

      show(`Task moved to ${newList}`, { type: 'success', duration: 3000 });
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

  // Handle delete
  const handleDeleteClick = () => {
    setShowDeleteConfirm(true);
  };

  const handleDeleteConfirm = async () => {
    if (isDeleting) return;

    setIsDeleting(true);

    try {
      await apiClient.delete(`/tasks/${task.id}`);

      show('Task deleted', { type: 'success' });
      setShowDeleteConfirm(false);
      onClose();
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

  // Handle edit - open detail panel
  const handleEdit = () => {
    onEdit?.();
    onClose();
  };

  // Get available system lists (exclude current list)
  const availableLists = Object.values(SystemList).filter(
    (list) => list !== task.systemList
  );

  // Calculate menu position (ensure it stays within viewport)
  const menuStyle: React.CSSProperties = {
    position: 'fixed',
    left: position.x,
    top: position.y,
    zIndex: 50,
  };

  return (
    <>
      <div
        ref={menuRef}
        style={menuStyle}
        className="w-56 bg-white rounded-lg shadow-lg border border-gray-200 py-1 text-sm"
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
            {list}
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
          disabled={isMoving || isDeleting}
          className="w-full text-left px-3 py-2 text-red-600 hover:bg-red-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          🗑️ Delete
        </button>
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
