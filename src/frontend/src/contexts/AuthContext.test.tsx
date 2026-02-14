import { render, screen, waitFor } from '@testing-library/react';
import { useAuth, AuthProvider } from './AuthContext';
import * as apiClient from '@/services/apiClient';
import { useRouter } from 'next/navigation';

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  usePathname: jest.fn(() => '/'),
}));

// Mock apiClient
jest.mock('@/services/apiClient', () => ({
  apiClient: {
    get: jest.fn(),
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

// Test component that uses AuthContext
function TestComponent() {
  const { user, isAuthenticated, isLoading, login, logout } = useAuth();
  return (
    <div>
      <div data-testid="loading">{isLoading ? 'loading' : 'loaded'}</div>
      <div data-testid="authenticated">{isAuthenticated ? 'true' : 'false'}</div>
      <div data-testid="user">{user ? user.email : 'no-user'}</div>
      <button onClick={() => login('test-token')}>Login</button>
      <button onClick={() => logout()}>Logout</button>
    </div>
  );
}

describe('AuthContext', () => {
  const mockPush = jest.fn();
  const mockGet = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (apiClient.apiClient.get as jest.Mock) = mockGet;
  });

  it('initializes without user when no token exists', async () => {
    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    // Should eventually finish loading and show no user
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded');
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
    });
  });

  it('sets user when token is valid', async () => {
    const mockUser = { id: '123', email: 'user@example.com', displayName: 'Test User' };
    mockGet.mockResolvedValueOnce({ data: mockUser, status: 200 });

    localStorage.setItem('authToken', 'valid-token');

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
      expect(screen.getByTestId('user')).toHaveTextContent('user@example.com');
    });
  });

  it('clears token and redirects when token is invalid (401)', async () => {
    const { ApiError } = await import('@/services/apiClient');
    mockGet.mockRejectedValueOnce(new (ApiError as any)('Unauthorized', 401));

    localStorage.setItem('authToken', 'invalid-token');

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(localStorage.getItem('authToken')).toBeNull();
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
    });
  });

  it('clears token when token is expired (404)', async () => {
    const { ApiError } = await import('@/services/apiClient');
    mockGet.mockRejectedValueOnce(new (ApiError as any)('Not Found', 404));

    localStorage.setItem('authToken', 'expired-token');

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(localStorage.getItem('authToken')).toBeNull();
    });
  });

  it('handles login with valid token', async () => {
    const mockUser = { id: '456', email: 'newuser@example.com', displayName: 'New User' };
    mockGet.mockResolvedValueOnce({ data: mockUser, status: 200 });

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded');
    });

    // Click login button
    const loginButton = screen.getByText('Login');
    loginButton.click();

    await waitFor(() => {
      expect(localStorage.getItem('authToken')).toBe('test-token');
      expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
    });
  });

  it('throws error when login is called outside provider', () => {
    // Suppress error output for this test
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation();

    expect(() => {
      render(<TestComponent />);
    }).toThrow('useAuth must be used within an AuthProvider');

    consoleSpy.mockRestore();
  });

  it('handles logout', async () => {
    const mockUser = { id: '789', email: 'user@example.com', displayName: 'Test' };
    mockGet.mockResolvedValue({ data: mockUser, status: 200 });

    localStorage.setItem('authToken', 'valid-token');

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    // Wait for auth to load
    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
    });

    // Click logout
    const logoutButton = screen.getByText('Logout');
    logoutButton.click();

    await waitFor(() => {
      expect(localStorage.getItem('authToken')).toBeNull();
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('shows loading state during auth validation', async () => {
    const mockUser = { id: '123', email: 'user@example.com', displayName: 'Test User' };
    mockGet.mockImplementationOnce(
      () =>
        new Promise((resolve) =>
          setTimeout(() => resolve({ data: mockUser, status: 200 }), 100)
        )
    );

    localStorage.setItem('authToken', 'valid-token');

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    expect(screen.getByTestId('loading')).toHaveTextContent('loading');

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded');
    });
  });

  it('does not redirect to login when pathname is /login or /register', async () => {
    const { ApiError } = await import('@/services/apiClient');
    mockGet.mockRejectedValueOnce(new (ApiError as any)('Unauthorized', 401));

    localStorage.setItem('authToken', 'invalid-token');

    // Mock usePathname to return /login
    const { usePathname } = require('next/navigation');
    usePathname.mockReturnValueOnce('/login');

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(mockPush).not.toHaveBeenCalled();
    });
  });
});
