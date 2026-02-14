import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import SidebarHeader from './SidebarHeader';
import { useAuth } from '@/contexts/AuthContext';

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

// Mock UserProfileMenu
jest.mock('../UserProfileMenu', () => {
  return function MockUserProfileMenu({
    displayName,
    email,
  }: {
    displayName: string;
    email: string;
  }) {
    return (
      <div data-testid="user-profile-menu">
        {displayName} - {email}
      </div>
    );
  };
});

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

  it('renders user profile menu when user is authenticated', () => {
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
    expect(screen.getByTestId('user-profile-menu')).toBeInTheDocument();
  });

  it('does not render user profile menu when not authenticated', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    render(<SidebarHeader />);
    expect(screen.queryByTestId('user-profile-menu')).not.toBeInTheDocument();
  });

  it('shows loading placeholder while loading', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: true,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(<SidebarHeader />);
    const placeholder = container.querySelector('.animate-pulse');
    expect(placeholder).toBeInTheDocument();
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
    expect(header).toHaveClass('px-4', 'py-4');
  });

  it('has app title with blue color', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(<SidebarHeader />);
    const title = container.querySelector('.text-blue-600');
    expect(title).toBeInTheDocument();
    expect(title).toHaveTextContent('GTD Todo');
  });

  it('has subtitle with gray color', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: jest.fn(),
    });

    const { container } = render(<SidebarHeader />);
    const subtitle = container.querySelector('.text-gray-500');
    expect(subtitle).toBeInTheDocument();
    expect(subtitle).toHaveTextContent('Getting Things Done');
  });
});
