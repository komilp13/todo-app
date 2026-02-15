/**
 * Tests for TaskRow component
 */

import { render, screen, fireEvent } from '@testing-library/react';
import TaskRow from './TaskRow';
import { TodoTask, Priority, TaskStatus, SystemList } from '@/types';

const mockTask: TodoTask = {
  id: '1',
  userId: 'user-1',
  name: 'Buy groceries',
  description: 'Milk, eggs, bread',
  priority: Priority.P2,
  status: TaskStatus.Open,
  systemList: SystemList.Inbox,
  sortOrder: 0,
  isArchived: false,
  dueDate: new Date().toISOString().split('T')[0],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

describe('TaskRow', () => {
  it('renders task name', () => {
    render(<TaskRow task={mockTask} />);
    expect(screen.getByText('Buy groceries')).toBeInTheDocument();
  });

  it('renders priority badge with correct color', () => {
    render(<TaskRow task={mockTask} />);
    const priorityBadge = screen.getByTitle('Priority: P2');
    expect(priorityBadge).toBeInTheDocument();
    expect(priorityBadge).toHaveTextContent('P2');
  });

  it('renders due date when present', () => {
    render(<TaskRow task={mockTask} />);
    expect(screen.getByText('Today')).toBeInTheDocument();
  });

  it('renders project name when provided', () => {
    render(<TaskRow task={mockTask} projectName="Home Renovation" />);
    expect(screen.getByText('Home Renovation')).toBeInTheDocument();
  });

  it('renders label chips when provided', () => {
    const labels = ['shopping', 'urgent'];
    render(
      <TaskRow
        task={mockTask}
        labelNames={labels}
        labelColors={{ shopping: '#ff0000', urgent: '#00ff00' }}
      />
    );
    expect(screen.getByText('shopping')).toBeInTheDocument();
    expect(screen.getByText('urgent')).toBeInTheDocument();
  });

  it('calls onComplete when checkbox is clicked', () => {
    const onComplete = jest.fn();
    render(<TaskRow task={mockTask} onComplete={onComplete} />);
    const checkbox = screen.getByRole('checkbox');
    fireEvent.click(checkbox);
    expect(onComplete).toHaveBeenCalledWith('1');
  });

  it('calls onClick when task row is clicked', () => {
    const onClick = jest.fn();
    render(<TaskRow task={mockTask} onClick={onClick} />);
    const taskName = screen.getByText('Buy groceries');
    fireEvent.click(taskName);
    expect(onClick).toHaveBeenCalledWith(mockTask);
  });

  it('shows strikethrough for completed tasks', () => {
    const completedTask = { ...mockTask, status: TaskStatus.Done };
    render(<TaskRow task={completedTask} />);
    const taskName = screen.getByText('Buy groceries');
    expect(taskName).toHaveClass('line-through');
  });

  it('renders checkbox unchecked for open tasks', () => {
    render(<TaskRow task={mockTask} />);
    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).not.toBeChecked();
  });

  it('renders checkbox checked for completed tasks', () => {
    const completedTask = { ...mockTask, status: TaskStatus.Done };
    render(<TaskRow task={completedTask} />);
    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toBeChecked();
  });

  it('does not render due date when not present', () => {
    const taskNoDueDate = { ...mockTask, dueDate: undefined };
    render(<TaskRow task={taskNoDueDate} />);
    expect(screen.queryByText('Today')).not.toBeInTheDocument();
  });

  it('applies fade-slide-out animation when isAnimatingOut is true', () => {
    const { container } = render(
      <TaskRow task={mockTask} isAnimatingOut={true} />
    );
    const taskRow = container.querySelector('.animate-fade-slide-out');
    expect(taskRow).toBeInTheDocument();
  });
});
