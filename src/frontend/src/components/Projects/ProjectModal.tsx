'use client';

import { useState, useEffect, useRef } from 'react';
import { apiClient, ApiError } from '@/services/apiClient';
import { useTaskRefreshContext } from '@/contexts/TaskRefreshContext';

interface ProjectToEdit {
  id: string;
  name: string;
  description?: string;
  dueDate?: string;
}

interface ProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  editingProject?: ProjectToEdit | null;
}

export default function ProjectModal({
  isOpen,
  onClose,
  editingProject,
}: ProjectModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [submitError, setSubmitError] = useState('');

  const { triggerRefresh } = useTaskRefreshContext();
  const nameInputRef = useRef<HTMLInputElement>(null);

  const isEditing = !!editingProject;

  // Populate form when opening
  useEffect(() => {
    if (isOpen) {
      if (editingProject) {
        setName(editingProject.name);
        setDescription(editingProject.description || '');
        setDueDate(editingProject.dueDate ? editingProject.dueDate.split('T')[0] : '');
      } else {
        setName('');
        setDescription('');
        setDueDate('');
      }
      setErrors({});
      setSubmitError('');
      setTimeout(() => nameInputRef.current?.focus(), 0);
    }
  }, [isOpen, editingProject]);

  // Escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen && !isLoading) {
        onClose();
      }
    };
    if (isOpen) {
      window.addEventListener('keydown', handleEscape);
      return () => window.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, isLoading, onClose]);

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget && !isLoading) {
      onClose();
    }
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!name.trim()) {
      newErrors.name = 'Project name is required';
    } else if (name.length > 200) {
      newErrors.name = 'Project name must be 200 characters or less';
    }
    if (description.length > 4000) {
      newErrors.description = 'Description must be 4000 characters or less';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    setIsLoading(true);
    setSubmitError('');

    try {
      const payload: Record<string, unknown> = {
        name: name.trim(),
      };
      if (description.trim()) {
        payload.description = description.trim();
      }
      if (dueDate) {
        payload.dueDate = dueDate;
      }

      if (isEditing) {
        await apiClient.put(`/projects/${editingProject!.id}`, payload);
      } else {
        await apiClient.post('/projects', payload);
      }

      triggerRefresh();
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        setSubmitError(err.message || 'Failed to save project');
      } else {
        setSubmitError('Failed to save project');
      }
    } finally {
      setIsLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-40 bg-black/15 transition-opacity"
        aria-hidden="true"
      />

      {/* Modal Container */}
      <div
        className="fixed inset-0 z-50 flex items-center justify-center p-4"
        onClick={handleBackdropClick}
      >
        <div
          className="relative w-full max-w-lg rounded-lg bg-white shadow-xl"
          role="dialog"
          aria-modal="true"
          aria-labelledby="project-modal-title"
          onPointerDown={(e) => e.stopPropagation()}
          onMouseDown={(e) => e.stopPropagation()}
          onClick={(e) => e.stopPropagation()}
        >
          <form onSubmit={handleSubmit}>
            {/* Header */}
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <h2 id="project-modal-title" className="text-lg font-semibold text-gray-900">
                {isEditing ? 'Edit project' : 'Add project'}
              </h2>
              <button
                type="button"
                onClick={onClose}
                disabled={isLoading}
                className="rounded-md p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors"
              >
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Body */}
            <div className="space-y-4 px-6 py-4">
              {submitError && (
                <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">
                  {submitError}
                </div>
              )}

              {/* Name */}
              <div>
                <label htmlFor="project-name" className="block text-sm font-medium text-gray-700 mb-1">
                  Name <span className="text-red-500">*</span>
                </label>
                <input
                  ref={nameInputRef}
                  id="project-name"
                  type="text"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    if (errors.name) setErrors((prev) => ({ ...prev, name: '' }));
                  }}
                  placeholder="Project name"
                  maxLength={200}
                  disabled={isLoading}
                  className={`w-full rounded-md border px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 focus:ring-1 focus:ring-blue-500 ${
                    errors.name ? 'border-red-300' : 'border-gray-300'
                  }`}
                />
                {errors.name && (
                  <p className="mt-1 text-xs text-red-600">{errors.name}</p>
                )}
              </div>

              {/* Description */}
              <div>
                <label htmlFor="project-description" className="block text-sm font-medium text-gray-700 mb-1">
                  Description
                </label>
                <textarea
                  id="project-description"
                  value={description}
                  onChange={(e) => {
                    setDescription(e.target.value);
                    if (errors.description) setErrors((prev) => ({ ...prev, description: '' }));
                  }}
                  placeholder="Add a description..."
                  rows={3}
                  maxLength={4000}
                  disabled={isLoading}
                  className={`w-full rounded-md border px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 focus:ring-1 focus:ring-blue-500 resize-none ${
                    errors.description ? 'border-red-300' : 'border-gray-300'
                  }`}
                />
                {errors.description && (
                  <p className="mt-1 text-xs text-red-600">{errors.description}</p>
                )}
              </div>

              {/* Due Date */}
              <div>
                <label htmlFor="project-due-date" className="block text-sm font-medium text-gray-700 mb-1">
                  Due date
                </label>
                <input
                  id="project-due-date"
                  type="date"
                  value={dueDate}
                  onChange={(e) => setDueDate(e.target.value)}
                  disabled={isLoading}
                  className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                />
              </div>
            </div>

            {/* Footer */}
            <div className="flex justify-end gap-3 border-t border-gray-200 px-6 py-4">
              <button
                type="button"
                onClick={onClose}
                disabled={isLoading}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isLoading}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {isLoading ? (
                  <span className="flex items-center gap-2">
                    <svg className="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                    </svg>
                    Saving...
                  </span>
                ) : isEditing ? (
                  'Save changes'
                ) : (
                  'Add project'
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
