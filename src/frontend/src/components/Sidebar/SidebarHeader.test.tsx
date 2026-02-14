import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import SidebarHeader from './SidebarHeader';
import { useAuth } from '@/contexts/AuthContext';

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

describe('SidebarHeader Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders application logo and name', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    render(<SidebarHeader />);
    expect(screen.getByText('GTD Todo')).toBeInTheDocument();
    expect(screen.getByText('Getting Things Done')).toBeInTheDocument();
  });

  it('renders user information when user is authenticated', () => {
    mockUseAuth.mockReturnValue({
      user: {
        id: '123',
        email: 'john@example.com',
        displayName: 'John Doe',
      },
      isAuthenticated: true,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    render(<SidebarHeader />);
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('john@example.com')).toBeInTheDocument();
  });

  it('does not render user card when user is not authenticated', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(<SidebarHeader />);
    const userCard = container.querySelector('.rounded-lg.bg-gray-50');
    expect(userCard).not.toBeInTheDocument();
  });

  it('displays user display name and email in card format', () => {
    mockUseAuth.mockReturnValue({
      user: {
        id: '456',
        email: 'jane@example.com',
        displayName: 'Jane Smith',
      },
      isAuthenticated: true,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(<SidebarHeader />);
    const userCard = container.querySelector('.rounded-lg.bg-gray-50');
    expect(userCard).toBeInTheDocument();
    expect(userCard).toHaveClass('p-3');
  });

  it('has border-bottom separator', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(<SidebarHeader />);
    const header = container.firstChild as HTMLElement;
    expect(header).toHaveClass('border-b', 'border-gray-200');
  });

  it('has proper padding', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(<SidebarHeader />);
    const header = container.firstChild as HTMLElement;
    expect(header).toHaveClass('px-6', 'py-6');
  });
});
