'use client';

import { useEffect, useState, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { TodoTask, LabelItem, GetLabelsResponse, SystemList } from '@/types';
import { apiClient, ApiError } from '@/services/apiClient';
import TaskList from '@/components/Tasks/TaskList';
import TaskDetailPanel from '@/components/Tasks/TaskDetailPanel';
import { useTaskRefresh } from '@/hooks/useTaskRefresh';
import { useTaskRefreshContext } from '@/contexts/TaskRefreshContext';
import ToastContainer from '@/components/Toast/ToastContainer';
import { useToast } from '@/hooks/useToast';

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

export default function LabelDetailPage() {
  const params = useParams();
  const router = useRouter();
  const labelId = params.id as string;

  const [label, setLabel] = useState<LabelItem | null>(null);
  const [tasks, setTasks] = useState<TodoTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [refreshCounter, setRefreshCounter] = useState(0);
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());

  const { triggerRefresh } = useTaskRefreshContext();
  const { toasts, show, dismiss } = useToast();

  // Register refresh callback
  useTaskRefresh(`label-${labelId}`, useCallback(() => {
    setRefreshCounter(prev => prev + 1);
  }, []));

  // Fetch label and tasks
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setNotFound(false);

      try {
        // Fetch all labels and find the one we need (no single-label endpoint)
        const [labelsRes, tasksRes] = await Promise.all([
          apiClient.get<GetLabelsResponse>('/labels'),
          apiClient.get<{ tasks: TodoTask[]; totalCount: number }>(`/tasks?labelId=${labelId}`),
        ]);

        const foundLabel = labelsRes.data.labels.find(l => l.id === labelId);
        if (!foundLabel) {
          setNotFound(true);
          return;
        }

        setLabel(foundLabel);
        setTasks(tasksRes.data.tasks);
      } catch (err) {
        if (err instanceof ApiError && err.statusCode === 404) {
          setNotFound(true);
        } else {
          console.error('Failed to fetch label:', err);
        }
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [labelId, refreshCounter]);

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
          <div className="h-8 w-48 rounded bg-gray-200" />
          <div className="h-4 w-32 rounded bg-gray-200" />
        </div>
        <div className="mt-8 space-y-3">
          {[1, 2, 3].map(i => (
            <div key={i} className="h-12 animate-pulse rounded-lg bg-gray-100" />
          ))}
        </div>
      </div>
    );
  }

  if (notFound || !label) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <div className="text-6xl mb-4">🏷</div>
        <h2 className="text-xl font-semibold text-gray-900">Label not found</h2>
        <p className="mt-2 text-sm text-gray-500">
          This label doesn&apos;t exist or you don&apos;t have access to it.
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

  return (
    <>
      <div className="space-y-6">
        {/* Label Header */}
        <div className="flex items-center gap-3">
          <div
            className="h-4 w-4 rounded-full"
            style={{ backgroundColor: label.color || '#9ca3af' }}
          />
          <h1 className="text-3xl font-bold text-gray-900">{label.name}</h1>
          <span className="text-sm text-gray-500">
            {label.taskCount} {label.taskCount === 1 ? 'task' : 'tasks'}
          </span>
        </div>

        {/* Task Sections grouped by System List */}
        {Object.keys(tasksByList).length === 0 && !loading ? (
          <div className="space-y-4">
            <TaskList
              systemList={SystemList.Inbox}
              labelId={labelId}
              onTaskClick={handleTaskClick}
              onTaskMoved={handleTaskChanged}
              onTaskDeleted={handleTaskChanged}
              refresh={refreshCounter}
              emptyMessage="No tasks with this label"
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
                      labelId={labelId}
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
        onTaskMoved={handleTaskChanged}
      />

      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </>
  );
}
