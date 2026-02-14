import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import AuthenticatedLayout from './AuthenticatedLayout';
import { useAuth } from '@/contexts/AuthContext';

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

// Mock AppShell component
jest.mock('./AppShell', () => {
  return function MockAppShell({ children }: { children: React.ReactNode }) {
    return <div data-testid="app-shell">{children}</div>;
  };
});

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

describe('AuthenticatedLayout Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading state while authentication is being checked', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: true,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(
      <AuthenticatedLayout>
        <div>Content</div>
      </AuthenticatedLayout>
    );

    expect(screen.getByText('Loading...')).toBeInTheDocument();
    // Check for loading spinner (div with animate-spin class)
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('renders AppShell for authenticated users', () => {
    mockUseAuth.mockReturnValue({
      user: {
        id: '123',
        email: 'test@example.com',
        displayName: 'Test User',
      },
      isAuthenticated: true,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    render(
      <AuthenticatedLayout>
        <div>Content</div>
      </AuthenticatedLayout>
    );

    expect(screen.getByTestId('app-shell')).toBeInTheDocument();
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('renders children without AppShell for unauthenticated users', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    render(
      <AuthenticatedLayout>
        <div>Login Page</div>
      </AuthenticatedLayout>
    );

    expect(screen.getByText('Login Page')).toBeInTheDocument();
    expect(screen.queryByTestId('app-shell')).not.toBeInTheDocument();
  });

  it('does not render loading spinner for authenticated users', () => {
    mockUseAuth.mockReturnValue({
      user: {
        id: '123',
        email: 'test@example.com',
        displayName: 'Test User',
      },
      isAuthenticated: true,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    render(
      <AuthenticatedLayout>
        <div>Content</div>
      </AuthenticatedLayout>
    );

    expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
  });

  it('does not render AppShell for unauthenticated users during loading', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: true,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { rerender } = render(
      <AuthenticatedLayout>
        <div>Content</div>
      </AuthenticatedLayout>
    );

    expect(screen.queryByTestId('app-shell')).not.toBeInTheDocument();
  });

  it('transitions from loading to authenticated view', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: true,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { rerender } = render(
      <AuthenticatedLayout>
        <div>Content</div>
      </AuthenticatedLayout>
    );

    expect(screen.getByText('Loading...')).toBeInTheDocument();

    // Update mock to authenticated state
    mockUseAuth.mockReturnValue({
      user: {
        id: '123',
        email: 'test@example.com',
        displayName: 'Test User',
      },
      isAuthenticated: true,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    rerender(
      <AuthenticatedLayout>
        <div>Content</div>
      </AuthenticatedLayout>
    );

    expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
    expect(screen.getByTestId('app-shell')).toBeInTheDocument();
  });
});
