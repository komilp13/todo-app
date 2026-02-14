import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter } from 'next/navigation';
import RegisterForm from './RegisterForm';
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

describe('RegisterForm', () => {
  const mockPush = jest.fn();
  const mockPost = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (apiClient.apiClient.post as jest.Mock) = mockPost;
  });

  it('renders all form fields', () => {
    render(<RegisterForm />);

    expect(screen.getByLabelText(/Display Name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Confirm Password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Sign Up/i })).toBeInTheDocument();
  });

  it('shows validation error for empty display name', async () => {
    render(<RegisterForm />);

    const submitButton = screen.getByRole('button', { name: /Sign Up/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Display name is required/i)).toBeInTheDocument();
    });
  });

  it('prevents form submission with validation errors', async () => {
    const user = userEvent.setup();
    render(<RegisterForm />);

    // Try to submit with invalid data
    const submitButton = screen.getByRole('button', { name: /Sign Up/i });
    await user.click(submitButton);

    // API should not be called due to validation
    await waitFor(() => {
      expect(mockPost).not.toHaveBeenCalled();
    });
  });

  it('successfully registers and redirects on valid submission', async () => {
    const user = userEvent.setup();
    mockPost.mockResolvedValue({
      data: { token: 'test-token-123' },
      status: 201,
    });

    render(<RegisterForm />);

    await user.type(screen.getByLabelText(/Display Name/i), 'Test User');
    await user.type(screen.getByLabelText(/^Email/i), 'newuser@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'ValidPassword123');
    await user.type(screen.getByLabelText(/Confirm Password/i), 'ValidPassword123');

    const submitButton = screen.getByRole('button', { name: /Sign Up/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockPost).toHaveBeenCalledWith(
        '/auth/register',
        {
          email: 'newuser@example.com',
          password: 'ValidPassword123',
          displayName: 'Test User',
        },
        { skipAuth: true }
      );
    });

    await waitFor(() => {
      expect(localStorage.getItem('authToken')).toBe('test-token-123');
      expect(mockPush).toHaveBeenCalledWith('/');
    });
  });

  it('shows error for duplicate email (409)', async () => {
    const user = userEvent.setup();
    const { ApiError } = await import('@/services/apiClient');
    mockPost.mockRejectedValue(
      new (ApiError as any)('Conflict', 409, { message: 'Email already registered' })
    );

    render(<RegisterForm />);

    await user.type(screen.getByLabelText(/Display Name/i), 'Test User');
    await user.type(screen.getByLabelText(/^Email/i), 'existing@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'ValidPassword123');
    await user.type(screen.getByLabelText(/Confirm Password/i), 'ValidPassword123');

    const submitButton = screen.getByRole('button', { name: /Sign Up/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Email already registered/i)).toBeInTheDocument();
    });
  });

  it('shows link to login page', () => {
    render(<RegisterForm />);

    const loginLink = screen.getByRole('link', { name: /Sign In/i });
    expect(loginLink).toHaveAttribute('href', '/login');
  });

  it('disables submit button while submitting', async () => {
    const user = userEvent.setup();
    mockPost.mockImplementation(
      () =>
        new Promise((resolve) =>
          setTimeout(() => resolve({ data: { token: 'test' }, status: 201 }), 1000)
        )
    );

    render(<RegisterForm />);

    await user.type(screen.getByLabelText(/Display Name/i), 'Test User');
    await user.type(screen.getByLabelText(/^Email/i), 'user@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'ValidPassword123');
    await user.type(screen.getByLabelText(/Confirm Password/i), 'ValidPassword123');

    const submitButton = screen.getByRole('button', { name: /Sign Up/i });
    await user.click(submitButton);

    expect(submitButton).toBeDisabled();
    expect(submitButton).toHaveTextContent(/Creating Account/i);
  });
});
