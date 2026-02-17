'use client';

import { useState, useEffect, useRef } from 'react';
import { apiClient, ApiError } from '@/services/apiClient';
import { useTaskRefreshContext } from '@/contexts/TaskRefreshContext';

interface LabelToEdit {
  id: string;
  name: string;
  color?: string;
}

interface LabelModalProps {
  isOpen: boolean;
  onClose: () => void;
  editingLabel?: LabelToEdit | null;
}

const COLOR_PRESETS = [
  '#ef4444', // red
  '#f97316', // orange
  '#eab308', // yellow
  '#22c55e', // green
  '#14b8a6', // teal
  '#3b82f6', // blue
  '#8b5cf6', // violet
  '#ec4899', // pink
  '#6b7280', // gray
];

export default function LabelModal({
  isOpen,
  onClose,
  editingLabel,
}: LabelModalProps) {
  const [name, setName] = useState('');
  const [color, setColor] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [submitError, setSubmitError] = useState('');

  const { triggerRefresh } = useTaskRefreshContext();
  const nameInputRef = useRef<HTMLInputElement>(null);

  const isEditing = !!editingLabel;

  // Populate form when opening
  useEffect(() => {
    if (isOpen) {
      if (editingLabel) {
        setName(editingLabel.name);
        setColor(editingLabel.color || '');
      } else {
        setName('');
        setColor('');
      }
      setErrors({});
      setSubmitError('');
      setTimeout(() => nameInputRef.current?.focus(), 0);
    }
  }, [isOpen, editingLabel]);

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
      newErrors.name = 'Label name is required';
    } else if (name.length > 100) {
      newErrors.name = 'Label name must be 100 characters or less';
    }
    if (color && !/^#[0-9a-fA-F]{6}$/.test(color)) {
      newErrors.color = 'Color must be a valid hex code (e.g. #ff4040)';
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
      if (color) {
        payload.color = color;
      }

      if (isEditing) {
        await apiClient.put(`/labels/${editingLabel!.id}`, payload);
      } else {
        await apiClient.post('/labels', payload);
      }

      triggerRefresh();
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.statusCode === 409) {
          setErrors({ name: 'A label with this name already exists' });
        } else {
          setSubmitError(err.message || 'Failed to save label');
        }
      } else {
        setSubmitError('Failed to save label');
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
          className="relative w-full max-w-md rounded-lg bg-white shadow-xl"
          role="dialog"
          aria-modal="true"
          aria-labelledby="label-modal-title"
          onPointerDown={(e) => e.stopPropagation()}
          onMouseDown={(e) => e.stopPropagation()}
          onClick={(e) => e.stopPropagation()}
        >
          <form onSubmit={handleSubmit}>
            {/* Header */}
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <h2 id="label-modal-title" className="text-lg font-semibold text-gray-900">
                {isEditing ? 'Edit label' : 'Add label'}
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
                <label htmlFor="label-name" className="block text-sm font-medium text-gray-700 mb-1">
                  Name <span className="text-red-500">*</span>
                </label>
                <input
                  ref={nameInputRef}
                  id="label-name"
                  type="text"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    if (errors.name) setErrors((prev) => ({ ...prev, name: '' }));
                  }}
                  placeholder="Label name"
                  maxLength={100}
                  disabled={isLoading}
                  className={`w-full rounded-md border px-3 py-2 text-sm outline-none transition-colors focus:border-blue-500 focus:ring-1 focus:ring-blue-500 ${
                    errors.name ? 'border-red-300' : 'border-gray-300'
                  }`}
                />
                {errors.name && (
                  <p className="mt-1 text-xs text-red-600">{errors.name}</p>
                )}
              </div>

              {/* Color */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Color
                </label>
                {/* Preset colors */}
                <div className="flex flex-wrap gap-2 mb-3">
                  {COLOR_PRESETS.map((preset) => (
                    <button
                      key={preset}
                      type="button"
                      onClick={() => {
                        setColor(preset);
                        if (errors.color) setErrors((prev) => ({ ...prev, color: '' }));
                      }}
                      className={`h-7 w-7 rounded-full border-2 transition-all ${
                        color === preset
                          ? 'border-gray-800 scale-110'
                          : 'border-transparent hover:border-gray-300'
                      }`}
                      style={{ backgroundColor: preset }}
                      title={preset}
                    />
                  ))}
                  {/* Clear color button */}
                  <button
                    type="button"
                    onClick={() => setColor('')}
                    className={`h-7 w-7 rounded-full border-2 transition-all flex items-center justify-center ${
                      !color
                        ? 'border-gray-800 scale-110'
                        : 'border-gray-300 hover:border-gray-400'
                    }`}
                    title="No color"
                  >
                    <svg className="h-4 w-4 text-gray-400" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                    </svg>
                  </button>
                </div>
                {/* Custom hex input */}
                <div className="flex items-center gap-2">
                  <div
                    className="h-8 w-8 shrink-0 rounded-md border border-gray-300"
                    style={{ backgroundColor: color || '#e5e7eb' }}
                  />
                  <input
                    id="label-color"
                    type="text"
                    value={color}
                    onChange={(e) => {
                      setColor(e.target.value);
                      if (errors.color) setErrors((prev) => ({ ...prev, color: '' }));
                    }}
                    placeholder="#000000"
                    maxLength={7}
                    disabled={isLoading}
                    className={`w-full rounded-md border px-3 py-1.5 text-sm outline-none transition-colors focus:border-blue-500 focus:ring-1 focus:ring-blue-500 ${
                      errors.color ? 'border-red-300' : 'border-gray-300'
                    }`}
                  />
                </div>
                {errors.color && (
                  <p className="mt-1 text-xs text-red-600">{errors.color}</p>
                )}
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
                  'Add label'
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
