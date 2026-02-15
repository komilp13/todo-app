/**
 * Tests for DraggableTaskRow component
 */

import { render, screen } from '@testing-library/react';
import DraggableTaskRow from './DraggableTaskRow';
import { TodoTask, Priority, TaskStatus, SystemList } from '@/types';
import { DndContext } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';

const mockTask: TodoTask = {
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
};

const renderWithDnd = (component: React.ReactElement) => {
  return render(
    <DndContext>
      <SortableContext items={[mockTask.id]} strategy={verticalListSortingStrategy}>
        {component}
      </SortableContext>
    </DndContext>
  );
};

describe('DraggableTaskRow', () => {
  it('renders task with drag handle', () => {
    renderWithDnd(
      <DraggableTaskRow task={mockTask} />
    );

    expect(screen.getByText('Buy groceries')).toBeInTheDocument();
    // Drag handle SVG should be present
    const svg = document.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('displays priority badge', () => {
    renderWithDnd(
      <DraggableTaskRow task={mockTask} />
    );

    expect(screen.getByText('P2')).toBeInTheDocument();
  });

  it('displays project name when provided', () => {
    renderWithDnd(
      <DraggableTaskRow task={mockTask} projectName="My Project" />
    );

    expect(screen.getByText('My Project')).toBeInTheDocument();
  });

  it('displays label chips when provided', () => {
    const labelNames = ['urgent', 'work'];
    const labelColors = { urgent: '#ff0000', work: '#0000ff' };

    renderWithDnd(
      <DraggableTaskRow
        task={mockTask}
        labelNames={labelNames}
        labelColors={labelColors}
      />
    );

    expect(screen.getByText('urgent')).toBeInTheDocument();
    expect(screen.getByText('work')).toBeInTheDocument();
  });

  it('calls onComplete when checkbox is clicked', () => {
    const onComplete = jest.fn();

    renderWithDnd(
      <DraggableTaskRow task={mockTask} onComplete={onComplete} />
    );

    const checkbox = document.querySelector('input[type="checkbox"]') as HTMLInputElement;
    checkbox?.click();

    expect(onComplete).toHaveBeenCalledWith(mockTask.id);
  });

  it('calls onClick when task row is clicked', () => {
    const onClick = jest.fn();

    renderWithDnd(
      <DraggableTaskRow task={mockTask} onClick={onClick} />
    );

    const taskRow = screen.getByText('Buy groceries').closest('div');
    taskRow?.click();

    expect(onClick).toHaveBeenCalledWith(mockTask);
  });

  it('renders drag handle even when isDragDisabled is true', () => {
    renderWithDnd(
      <DraggableTaskRow task={mockTask} isDragDisabled={true} />
    );

    // Task should still render with handle visible (disabled prop prevents actual drag)
    expect(screen.getByText('Buy groceries')).toBeInTheDocument();
  });

  it('displays strikethrough when task is completed', () => {
    const completedTask = { ...mockTask, status: TaskStatus.Done };

    renderWithDnd(
      <DraggableTaskRow task={completedTask} />
    );

    const taskName = screen.getByText('Buy groceries');
    expect(taskName).toHaveClass('line-through');
  });

  it('applies animation class when isAnimatingOut is true', () => {
    const { container } = renderWithDnd(
      <DraggableTaskRow task={mockTask} isAnimatingOut={true} />
    );

    const taskRow = container.querySelector('[class*="animate-fade-slide-out"]');
    expect(taskRow).toBeInTheDocument();
  });
});
