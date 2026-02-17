'use client';

import { useEffect, useState, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { TodoTask, ProjectItem, GetProjectsResponse, SystemList, TaskStatus, ProjectStatus } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import TaskList from '@/components/Tasks/TaskList';
import TaskDetailPanel from '@/components/Tasks/TaskDetailPanel';
import ConfirmationModal from '@/components/shared/ConfirmationModal';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import { useTaskRefreshContext } from '@/contexts/TaskRefreshContext';
import { useToast } from '@/hooks/useToast';
import { useProjectModalContext } from '@/contexts/ProjectModalContext';
import ToastContainer from '@/components/Toast/ToastContainer';

interface ProjectResponse {
  id: string;
  name: string;
  description?: string;
  dueDate?: string;
  status: string;
  sortOrder: number;
  totalTaskCount: number;
  completedTaskCount: number;
  completionPercentage: number;
  createdAt: string;
  updatedAt: string;
}

const SYSTEM_LIST_ORDER: SystemList[] = [
  SystemList.Inbox,
  SystemList.Next,
  SystemList.Upcoming,
  SystemList.Someday,
];

const SYSTEM_LIST_LABELS: Record<SystemList, string> = {
  [SystemList.Inbox]: 'Inbox',
  [SystemList.Next]: 'Next',
  [SystemList.Upcoming]: 'Upcoming',
  [SystemList.Someday]: 'Someday',
};

export default function ProjectDetailPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = params.id as string;

  const [project, setProject] = useState<ProjectResponse | null>(null);
  const [tasks, setTasks] = useState<TodoTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [refreshCounter, setRefreshCounter] = useState(0);
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
  const [showCompleteConfirm, setShowCompleteConfirm] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [editingName, setEditingName] = useState(false);
  const [editingDescription, setEditingDescription] = useState(false);
  const [nameValue, setNameValue] = useState('');
  const [descriptionValue, setDescriptionValue] = useState('');

  const { triggerRefresh } = useTaskRefreshContext();
  const { toasts, show, dismiss } = useToast();
  const { openEditModal } = useProjectModalContext();

  // Register refresh callback
  useTaskRefresh(`project-${projectId}`, useCallback(() => {
    setRefreshCounter(prev => prev + 1);
  }, []));

  // Fetch project and tasks
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setNotFound(false);

      try {
        const [projectRes, tasksRes] = await Promise.all([
          apiClient.get<ProjectResponse>(`/projects/${projectId}`),
          apiClient.get<{ tasks: TodoTask[]; totalCount: number }>(`/tasks?projectId=${projectId}`),
        ]);

        setProject(projectRes.data);
        setTasks(tasksRes.data.tasks);
      } catch (err) {
        if (err instanceof ApiError && err.statusCode === 404) {
          setNotFound(true);
        } else {
          console.error('Failed to fetch project:', err);
        }
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [projectId, refreshCounter]);

  const handleTaskClick = (task: TodoTask) => {
    setSelectedTaskId(task.id);
  };

  const handleClosePanel = () => {
    setSelectedTaskId(null);
  };

  const handleTaskChanged = () => {
    setSelectedTaskId(null);
    setRefreshCounter(prev => prev + 1);
    triggerRefresh();
  };

  const handleTaskUpdated = () => {
    setRefreshCounter(prev => prev + 1);
    triggerRefresh();
  };

  const handleCompleteProject = async () => {
    try {
      await apiClient.patch(`/projects/${projectId}/complete`);
      setRefreshCounter(prev => prev + 1);
      triggerRefresh();
      show('Project completed', { type: 'success' });
    } catch (err) {
      console.error('Failed to complete project:', err);
      show('Failed to complete project', { type: 'error' });
    } finally {
      setShowCompleteConfirm(false);
    }
  };

  const handleReopenProject = async () => {
    try {
      await apiClient.patch(`/projects/${projectId}/reopen`);
      setRefreshCounter(prev => prev + 1);
      triggerRefresh();
      show('Project reopened', { type: 'success' });
    } catch (err) {
      console.error('Failed to reopen project:', err);
      show('Failed to reopen project', { type: 'error' });
    }
  };

  const handleDeleteProject = async () => {
    try {
      await apiClient.delete(`/projects/${projectId}`);
      triggerRefresh();
      router.push('/inbox');
    } catch (err) {
      console.error('Failed to delete project:', err);
      show('Failed to delete project', { type: 'error' });
      setShowDeleteConfirm(false);
    }
  };

  const handleStartEditName = () => {
    if (!project) return;
    setNameValue(project.name);
    setEditingName(true);
  };

  const handleSaveName = async () => {
    if (!project || !nameValue.trim()) return;
    if (nameValue.trim() === project.name) {
      setEditingName(false);
      return;
    }
    try {
      await apiClient.put(`/projects/${projectId}`, { name: nameValue.trim() });
      setRefreshCounter(prev => prev + 1);
      triggerRefresh();
    } catch (err) {
      console.error('Failed to update project name:', err);
      show('Failed to update project name', { type: 'error' });
    } finally {
      setEditingName(false);
    }
  };

  const handleStartEditDescription = () => {
    if (!project) return;
    setDescriptionValue(project.description || '');
    setEditingDescription(true);
  };

  const handleSaveDescription = async () => {
    if (!project) return;
    const newDesc = descriptionValue.trim() || null;
    if (newDesc === (project.description || null)) {
      setEditingDescription(false);
      return;
    }
    try {
      await apiClient.put(`/projects/${projectId}`, { name: project.name, description: newDesc });
      setRefreshCounter(prev => prev + 1);
      triggerRefresh();
    } catch (err) {
      console.error('Failed to update project description:', err);
      show('Failed to update project description', { type: 'error' });
    } finally {
      setEditingDescription(false);
    }
  };

  const toggleSection = (list: string) => {
    setCollapsedSections(prev => {
      const next = new Set(prev);
      if (next.has(list)) {
        next.delete(list);
      } else {
        next.add(list);
      }
      return next;
    });
  };

  // Group tasks by system list
  const tasksByList = SYSTEM_LIST_ORDER.reduce((acc, list) => {
    const listTasks = tasks.filter(t => t.systemList === list && !t.isArchived);
    if (listTasks.length > 0) {
      acc[list] = listTasks;
    }
    return acc;
  }, {} as Record<SystemList, TodoTask[]>);

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="animate-pulse space-y-3">
          <div className="h-8 w-64 rounded bg-gray-200" />
          <div className="h-4 w-96 rounded bg-gray-200" />
          <div className="h-3 w-full max-w-md rounded bg-gray-200" />
        </div>
        <div className="mt-8 space-y-3">
          {[1, 2, 3].map(i => (
            <div key={i} className="h-12 animate-pulse rounded-lg bg-gray-100" />
          ))}
        </div>
      </div>
    );
  }

  if (notFound || !project) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <div className="text-6xl mb-4">📁</div>
        <h2 className="text-xl font-semibold text-gray-900">Project not found</h2>
        <p className="mt-2 text-sm text-gray-500">
          This project doesn&apos;t exist or you don&apos;t have access to it.
        </p>
        <button
          onClick={() => router.push('/inbox')}
          className="mt-4 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          Go to Inbox
        </button>
      </div>
    );
  }

  const isCompleted = project.status === 'completed';
  const openTaskCount = project.totalTaskCount - project.completedTaskCount;

  return (
    <>
      <div className="space-y-6">
        {/* Project Header */}
        <div>
          <div className="flex items-start justify-between">
            <div className="flex-1">
              {editingName ? (
                <input
                  type="text"
                  value={nameValue}
                  onChange={(e) => setNameValue(e.target.value)}
                  onBlur={handleSaveName}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleSaveName();
                    if (e.key === 'Escape') setEditingName(false);
                  }}
                  autoFocus
                  className="w-full text-3xl font-bold text-gray-900 bg-transparent border-b-2 border-blue-500 outline-none"
                />
              ) : (
                <h1
                  className="text-3xl font-bold text-gray-900 cursor-pointer hover:text-blue-700 transition-colors"
                  onClick={handleStartEditName}
                  title="Click to edit"
                >
                  {project.name}
                  {isCompleted && (
                    <span className="ml-3 inline-flex items-center rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-800">
                      Completed
                    </span>
                  )}
                </h1>
              )}
              {editingDescription ? (
                <textarea
                  value={descriptionValue}
                  onChange={(e) => setDescriptionValue(e.target.value)}
                  onBlur={handleSaveDescription}
                  onKeyDown={(e) => {
                    if (e.key === 'Escape') setEditingDescription(false);
                  }}
                  autoFocus
                  rows={2}
                  className="mt-2 w-full text-sm text-gray-600 bg-transparent border-b-2 border-blue-500 outline-none resize-none"
                  placeholder="Add a description..."
                />
              ) : (
                <p
                  className="mt-2 text-sm text-gray-600 cursor-pointer hover:text-blue-700 transition-colors"
                  onClick={handleStartEditDescription}
                  title="Click to edit"
                >
                  {project.description || 'Add a description...'}
                </p>
              )}
              {project.dueDate && (
                <p className="mt-1 text-xs text-gray-500">
                  Due: {new Date(project.dueDate).toLocaleDateString()}
                </p>
              )}
            </div>

            {/* Action buttons */}
            <div className="ml-4 flex items-center gap-2">
              <button
                onClick={() => openEditModal({
                  id: project.id,
                  name: project.name,
                  description: project.description,
                  dueDate: project.dueDate,
                })}
                className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
              >
                Edit
              </button>
              {isCompleted ? (
                <button
                  onClick={handleReopenProject}
                  className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Reopen project
                </button>
              ) : (
                <button
                  onClick={() => setShowCompleteConfirm(true)}
                  className="rounded-lg border border-green-300 bg-green-50 px-3 py-1.5 text-sm font-medium text-green-700 hover:bg-green-100 transition-colors"
                >
                  Complete project
                </button>
              )}
              <button
                onClick={() => setShowDeleteConfirm(true)}
                className="rounded-lg border border-red-300 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 transition-colors"
              >
                Delete
              </button>
            </div>
          </div>

          {/* Progress Bar */}
          {project.totalTaskCount > 0 && (
            <div className="mt-4">
              <div className="flex items-center justify-between text-sm">
                <span className="text-gray-600">
                  {project.completedTaskCount} of {project.totalTaskCount} tasks done — {project.completionPercentage}%
                </span>
                <span className="text-gray-500">
                  {openTaskCount} open
                </span>
              </div>
              <div className="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-gray-200">
                <div
                  className="h-full rounded-full bg-blue-500 transition-all duration-300"
                  style={{ width: `${project.completionPercentage}%` }}
                />
              </div>
            </div>
          )}
        </div>

        {/* Task Sections grouped by System List */}
        {Object.keys(tasksByList).length === 0 && !loading ? (
          <div className="space-y-4">
            {/* Quick add even when empty */}
            <TaskList
              systemList={SystemList.Inbox}
              projectId={projectId}
              onTaskClick={handleTaskClick}
              onTaskMoved={handleTaskChanged}
              onTaskDeleted={handleTaskChanged}
              refresh={refreshCounter}
              emptyMessage="No tasks in this project yet. Add one above!"
            />
          </div>
        ) : (
          <div className="space-y-6">
            {SYSTEM_LIST_ORDER.map(list => {
              const listTasks = tasksByList[list];
              if (!listTasks || listTasks.length === 0) return null;

              const isCollapsed = collapsedSections.has(list);

              return (
                <div key={list}>
                  {/* Section Header */}
                  <button
                    onClick={() => toggleSection(list)}
                    className="mb-2 flex w-full items-center gap-2 text-left"
                  >
                    <svg
                      className={`h-4 w-4 text-gray-400 transition-transform ${isCollapsed ? '' : 'rotate-90'}`}
                      fill="none"
                      viewBox="0 0 24 24"
                      strokeWidth={2}
                      stroke="currentColor"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                    </svg>
                    <span className="text-sm font-semibold text-gray-700">
                      {SYSTEM_LIST_LABELS[list]}
                    </span>
                    <span className="text-sm text-gray-400">
                      ({listTasks.length})
                    </span>
                  </button>

                  {/* Task List for this section */}
                  {!isCollapsed && (
                    <TaskList
                      systemList={list}
                      projectId={projectId}
                      onTaskClick={handleTaskClick}
                      onTaskMoved={handleTaskChanged}
                      onTaskDeleted={handleTaskChanged}
                      refresh={refreshCounter}
                      emptyMessage=""
                    />
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Task Detail Panel */}
      <TaskDetailPanel
        isOpen={!!selectedTaskId}
        taskId={selectedTaskId}
        onClose={handleClosePanel}
        onTaskDeleted={handleTaskChanged}
        onTaskUpdated={handleTaskUpdated}
        onTaskMoved={handleTaskChanged}
      />

      {/* Complete Project Confirmation */}
      <ConfirmationModal
        isOpen={showCompleteConfirm}
        onCancel={() => setShowCompleteConfirm(false)}
        onConfirm={handleCompleteProject}
        title="Complete project"
        message={`Mark "${project.name}" as completed? This will not complete the tasks in the project.`}
        confirmLabel="Complete"
      />

      {/* Delete Project Confirmation */}
      <ConfirmationModal
        isOpen={showDeleteConfirm}
        onCancel={() => setShowDeleteConfirm(false)}
        onConfirm={handleDeleteProject}
        title="Delete project"
        message={`This will permanently delete "${project.name}". Tasks will not be deleted but will no longer belong to any project.`}
        confirmLabel="Delete"
        isDanger
      />

      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </>
  );
}
