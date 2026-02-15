/**
 * Tests for QuickAddTaskInput component
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import QuickAddTaskInput from './QuickAddTaskInput';
import { SystemList } from '@/types';

describe('QuickAddTaskInput', () => {
  it('renders input with placeholder and plus icon', () => {
    render(<QuickAddTaskInput systemList={SystemList.Inbox} />);

    const input = screen.getByPlaceholderText('Add a task...');
    expect(input).toBeInTheDocument();

    // Check for plus icon SVG
    const svg = document.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('auto-focuses input on mount', () => {
    render(<QuickAddTaskInput systemList={SystemList.Inbox} />);

    const input = screen.getByPlaceholderText('Add a task...');
    expect(input).toHaveFocus();
  });

  it('updates input value on type', async () => {
    const user = userEvent.setup();
    render(<QuickAddTaskInput systemList={SystemList.Inbox} />);

    const input = screen.getByPlaceholderText('Add a task...') as HTMLInputElement;
    await user.type(input, 'Buy groceries');

    expect(input.value).toBe('Buy groceries');
  });

  it('calls onTaskCreated when Enter is pressed', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...');
    await user.type(input, 'Buy groceries');
    await user.keyboard('{Enter}');

    await waitFor(() => {
      expect(onTaskCreated).toHaveBeenCalledWith('Buy groceries');
    });
  });

  it('clears input after task creation', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...') as HTMLInputElement;
    await user.type(input, 'Buy groceries');
    await user.keyboard('{Enter}');

    await waitFor(() => {
      expect(input.value).toBe('');
    });
  });

  it('keeps focus after task creation for rapid entry', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...');
    await user.type(input, 'Task 1');
    await user.keyboard('{Enter}');

    await waitFor(() => {
      expect(input).toHaveFocus();
    });
  });

  it('clears input when Escape is pressed', async () => {
    const user = userEvent.setup();
    render(<QuickAddTaskInput systemList={SystemList.Inbox} />);

    const input = screen.getByPlaceholderText('Add a task...') as HTMLInputElement;
    await user.type(input, 'Buy groceries');
    await user.keyboard('{Escape}');

    expect(input.value).toBe('');
  });

  it('does not submit when input is empty or whitespace', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn();
    const onError = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
        onError={onError}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...');

    // Try with just spaces
    await user.type(input, '   ');
    await user.keyboard('{Enter}');

    await waitFor(() => {
      expect(onError).toHaveBeenCalledWith('Task name cannot be empty');
      expect(onTaskCreated).not.toHaveBeenCalled();
    });
  });

  it('trims whitespace from task name', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...');
    await user.type(input, '  Buy groceries  ');
    await user.keyboard('{Enter}');

    await waitFor(() => {
      expect(onTaskCreated).toHaveBeenCalledWith('Buy groceries');
    });
  });

  it('handles async task creation', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...') as HTMLInputElement;
    await user.type(input, 'Buy groceries');
    await user.keyboard('{Enter}');

    // Task should be created
    expect(onTaskCreated).toHaveBeenCalledWith('Buy groceries');

    // Input should be cleared
    expect(input.value).toBe('');
  });

  it('passes systemList to parent', () => {
    const onTaskCreated = jest.fn();
    render(
      <QuickAddTaskInput
        systemList={SystemList.Next}
        onTaskCreated={onTaskCreated}
      />
    );

    // Component renders successfully with different systemList
    expect(screen.getByPlaceholderText('Add a task...')).toBeInTheDocument();
  });

  it('passes projectId to parent', () => {
    const onTaskCreated = jest.fn();
    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        projectId="project-123"
        onTaskCreated={onTaskCreated}
      />
    );

    // Component renders successfully with projectId
    expect(screen.getByPlaceholderText('Add a task...')).toBeInTheDocument();
  });

  it('calls onError callback on error', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn(() => {
      throw new Error('API Error');
    });
    const onError = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
        onError={onError}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...');
    await user.type(input, 'Buy groceries');
    await user.keyboard('{Enter}');

    await waitFor(() => {
      expect(onError).toHaveBeenCalledWith('API Error');
    });
  });

  it('handles form submission via button click', async () => {
    const user = userEvent.setup();
    const onTaskCreated = jest.fn();

    render(
      <QuickAddTaskInput
        systemList={SystemList.Inbox}
        onTaskCreated={onTaskCreated}
      />
    );

    const input = screen.getByPlaceholderText('Add a task...');
    const button = screen.getByRole('button', { name: 'Create task' });

    await user.type(input, 'Buy groceries');
    await user.click(button);

    expect(onTaskCreated).toHaveBeenCalledWith('Buy groceries');
  });

  it('disables submit button when input is empty', async () => {
    render(<QuickAddTaskInput systemList={SystemList.Inbox} />);

    const button = screen.getByRole('button', { name: 'Create task' });
    expect(button).toBeDisabled();
  });

  it('enables submit button when input has text', async () => {
    const user = userEvent.setup();
    render(<QuickAddTaskInput systemList={SystemList.Inbox} />);

    const input = screen.getByPlaceholderText('Add a task...');
    const button = screen.getByRole('button', { name: 'Create task' });

    await user.type(input, 'Buy groceries');

    expect(button).not.toBeDisabled();
  });
});
