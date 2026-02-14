import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter } from 'next/navigation';
import LoginForm from './LoginForm';
import * as apiClient from '@/services/apiClient';

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}));

// Mock apiClient
jest.mock('@/services/apiClient', () => ({
  apiClient: {
    post: jest.fn(),
  },
  ApiError: class extends Error {
    statusCode: number;
    details: any;
    constructor(message: string, statusCode: number, details?: any) {
      super(message);
      this.statusCode = statusCode;
      this.details = details;
    }
  },
}));

describe('LoginForm', () => {
  const mockPush = jest.fn();
  const mockPost = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (apiClient.apiClient.post as jest.Mock) = mockPost;
  });

  it('renders all form fields', () => {
    render(<LoginForm />);

    expect(screen.getByLabelText(/^Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Sign In/i })).toBeInTheDocument();
  });

  it('shows validation error for empty email', async () => {
    render(<LoginForm />);

    const submitButton = screen.getByRole('button', { name: /Sign In/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Email is required/i)).toBeInTheDocument();
    });
  });

  it('prevents form submission with validation errors', async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    // Try to submit with invalid data (empty fields)
    const submitButton = screen.getByRole('button', { name: /Sign In/i });
    await user.click(submitButton);

    // API should not be called due to validation
    await waitFor(() => {
      expect(mockPost).not.toHaveBeenCalled();
    });
  });

  it('successfully logs in and redirects on valid submission', async () => {
    const user = userEvent.setup();
    mockPost.mockResolvedValue({
      data: { token: 'test-token-123' },
      status: 200,
    });

    render(<LoginForm />);

    await user.type(screen.getByLabelText(/^Email/i), 'user@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'TestPassword123');

    const submitButton = screen.getByRole('button', { name: /Sign In/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockPost).toHaveBeenCalledWith(
        '/auth/login',
        {
          email: 'user@example.com',
          password: 'TestPassword123',
        },
        { skipAuth: true }
      );
    });

    await waitFor(() => {
      expect(localStorage.getItem('authToken')).toBe('test-token-123');
      expect(mockPush).toHaveBeenCalledWith('/');
    });
  });

  it('shows error for invalid credentials (401)', async () => {
    const user = userEvent.setup();
    const { ApiError } = await import('@/services/apiClient');
    mockPost.mockRejectedValue(
      new (ApiError as any)('Unauthorized', 401, { message: 'Invalid email or password' })
    );

    render(<LoginForm />);

    await user.type(screen.getByLabelText(/^Email/i), 'user@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'WrongPassword');

    const submitButton = screen.getByRole('button', { name: /Sign In/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Invalid email or password/i)).toBeInTheDocument();
    });
  });

  it('shows link to registration page', () => {
    render(<LoginForm />);

    const registerLink = screen.getByRole('link', { name: /Sign Up/i });
    expect(registerLink).toHaveAttribute('href', '/register');
  });

  it('disables submit button while submitting', async () => {
    const user = userEvent.setup();
    mockPost.mockImplementation(
      () =>
        new Promise((resolve) =>
          setTimeout(() => resolve({ data: { token: 'test' }, status: 200 }), 1000)
        )
    );

    render(<LoginForm />);

    await user.type(screen.getByLabelText(/^Email/i), 'user@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'TestPassword123');

    const submitButton = screen.getByRole('button', { name: /Sign In/i });
    await user.click(submitButton);

    expect(submitButton).toBeDisabled();
    expect(submitButton).toHaveTextContent(/Signing In/i);
  });
});
