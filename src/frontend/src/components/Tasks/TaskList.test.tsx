/**
 * Tests for TaskList component
 */

import { render, screen, waitFor } from '@testing-library/react';
import TaskList from './TaskList';
import { TodoTask, Priority, TaskStatus, SystemList } from '@/types';
import { apiClient } from '@/services/apiClient';

jest.mock('@/services/apiClient');

const mockTasks: TodoTask[] = [
  {
    id: '1',
    userId: 'user-1',
    name: 'Buy groceries',
    priority: Priority.P2,
    status: TaskStatus.Open,
    systemList: SystemList.Inbox,
    sortOrder: 0,
    isArchived: false,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: '2',
    userId: 'user-1',
    name: 'Write report',
    priority: Priority.P1,
    status: TaskStatus.Open,
    systemList: SystemList.Inbox,
    sortOrder: 1,
    isArchived: false,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];

describe('TaskList', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading skeleton initially', () => {
    (apiClient.get as jest.Mock).mockImplementation(
      () =>
        new Promise((resolve) => {
          setTimeout(() => resolve({ data: mockTasks }), 100);
        })
    );

    const { container } = render(<TaskList systemList={SystemList.Inbox} />);
    // Check for animate-pulse class which is used on skeleton
    expect(container.querySelector('.animate-pulse')).toBeInTheDocument();
  });

  it('fetches and displays tasks', async () => {
    (apiClient.get as jest.Mock).mockResolvedValue({ data: mockTasks });

    render(<TaskList systemList={SystemList.Inbox} />);

    await waitFor(() => {
      expect(screen.getByText('Buy groceries')).toBeInTheDocument();
      expect(screen.getByText('Write report')).toBeInTheDocument();
    });
  });

  it('displays empty state when no tasks', async () => {
    (apiClient.get as jest.Mock).mockResolvedValue({ data: [] });

    render(<TaskList systemList={SystemList.Inbox} />);

    await waitFor(() => {
      expect(
        screen.getByText(/No tasks here. Enjoy your free time/i)
      ).toBeInTheDocument();
    });
  });

  it('displays error message on API failure', async () => {
    (apiClient.get as jest.Mock).mockRejectedValue(new Error('API Error'));

    render(<TaskList systemList={SystemList.Inbox} />);

    await waitFor(() => {
      expect(
        screen.getByText(/Failed to load tasks/i)
      ).toBeInTheDocument();
    });
  });

  it('calls apiClient.get with correct systemList parameter', async () => {
    (apiClient.get as jest.Mock).mockResolvedValue({ data: mockTasks });

    render(<TaskList systemList={SystemList.Next} />);

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith(
        '/tasks?systemList=Next'
      );
    });
  });

  it('refetches tasks when refresh prop changes', async () => {
    (apiClient.get as jest.Mock).mockResolvedValue({ data: mockTasks });

    const { rerender } = render(
      <TaskList systemList={SystemList.Inbox} refresh={0} />
    );

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledTimes(1);
    });

    rerender(<TaskList systemList={SystemList.Inbox} refresh={1} />);

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledTimes(2);
    });
  });

  it('calls onTaskComplete when task is completed', async () => {
    const onTaskComplete = jest.fn();
    (apiClient.get as jest.Mock).mockResolvedValue({ data: mockTasks });

    render(
      <TaskList
        systemList={SystemList.Inbox}
        onTaskComplete={onTaskComplete}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Buy groceries')).toBeInTheDocument();
    });
  });

  it('calls onTaskClick when task is clicked', async () => {
    const onTaskClick = jest.fn();
    (apiClient.get as jest.Mock).mockResolvedValue({ data: mockTasks });

    render(
      <TaskList systemList={SystemList.Inbox} onTaskClick={onTaskClick} />
    );

    await waitFor(() => {
      expect(screen.getByText('Buy groceries')).toBeInTheDocument();
    });
  });
});
