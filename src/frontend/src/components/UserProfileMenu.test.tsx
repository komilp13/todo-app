import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import UserProfileMenu from './UserProfileMenu';
import { useAuth } from '@/contexts/AuthContext';

// Mock useAuth
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

// Mock UserAvatar
jest.mock('./UserAvatar', () => {
  return function MockUserAvatar({ displayName }: { displayName: string }) {
    return <div data-testid="user-avatar">{displayName[0]}</div>;
  };
});

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;
const mockLogout = jest.fn();

describe('UserProfileMenu Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockLogout.mockResolvedValue(undefined);
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });
  });

  it('renders user profile button with avatar', () => {
    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    expect(screen.getByTestId('user-avatar')).toBeInTheDocument();
    expect(screen.getByText('John Doe')).toBeInTheDocument();
  });

  it('displays user email', () => {
    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    expect(screen.getByText('john@example.com')).toBeInTheDocument();
  });

  it('truncates long display names', () => {
    const { container } = render(
      <UserProfileMenu
        displayName="John Alexander Christopher Doe"
        email="john@example.com"
      />
    );
    const nameElement = container.querySelector('.truncate');
    expect(nameElement).toBeInTheDocument();
  });

  it('truncates long emails', () => {
    const { container } = render(
      <UserProfileMenu
        displayName="John Doe"
        email="john.alexander.christopher.doe@verylongemail.com"
      />
    );
    const emailElements = container.querySelectorAll('.truncate');
    expect(emailElements.length).toBeGreaterThan(1);
  });

  it('opens dropdown menu on button click', async () => {
    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    const button = screen.getByRole('button', { name: /John Doe/i });

    fireEvent.click(button);

    // Dropdown should now be visible
    const menuItems = screen.getAllByText('John Doe');
    expect(menuItems.length).toBeGreaterThan(1); // One in button, one in dropdown
  });

  it('closes dropdown menu on button click when open', async () => {
    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    const button = screen.getByRole('button', { name: /John Doe/i });

    // Open menu
    fireEvent.click(button);
    await waitFor(() => {
      expect(screen.getByRole('menu')).toBeInTheDocument();
    });

    // Close menu
    fireEvent.click(button);
    await waitFor(() => {
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    });
  });

  it('closes dropdown on outside click', async () => {
    const { container } = render(
      <UserProfileMenu displayName="John Doe" email="john@example.com" />
    );
    const button = screen.getByRole('button', { name: /John Doe/i });

    // Open menu
    fireEvent.click(button);
    await waitFor(() => {
      expect(screen.getByRole('menu')).toBeInTheDocument();
    });

    // Click outside
    fireEvent.mouseDown(container);
    await waitFor(() => {
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    });
  });

  it('calls logout function on logout button click', async () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    const button = screen.getByRole('button', { name: /John Doe/i });

    // Open menu
    fireEvent.click(button);

    // Click logout
    const logoutButton = screen.getByRole('menuitem', { name: /logout/i });
    fireEvent.click(logoutButton);

    await waitFor(() => {
      expect(mockLogout).toHaveBeenCalled();
    });
  });

  it('renders logout button with red styling', async () => {
    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    const button = screen.getByRole('button', { name: /John Doe/i });

    fireEvent.click(button);

    const logoutButton = screen.getByRole('menuitem', { name: /logout/i });
    expect(logoutButton).toHaveClass('text-red-600');
  });

  it('displays chevron icon', () => {
    const { container } = render(
      <UserProfileMenu displayName="John Doe" email="john@example.com" />
    );
    const svg = container.querySelector('svg');
    expect(svg).toBeInTheDocument();
  });

  it('rotates chevron when menu is open', async () => {
    const { container } = render(
      <UserProfileMenu displayName="John Doe" email="john@example.com" />
    );
    const button = screen.getByRole('button', { name: /John Doe/i });

    // Initially closed
    let svg = container.querySelector('svg');
    expect(svg).not.toHaveClass('rotate-180');

    // Open menu
    fireEvent.click(button);
    await waitFor(() => {
      svg = container.querySelector('svg');
      expect(svg).toHaveClass('rotate-180');
    });
  });

  it('has proper aria attributes', () => {
    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    const button = screen.getByRole('button', { name: /John Doe/i });

    expect(button).toHaveAttribute('aria-haspopup', 'menu');
    expect(button).toHaveAttribute('aria-expanded', 'false');
  });

  it('updates aria-expanded when menu opens', async () => {
    render(<UserProfileMenu displayName="John Doe" email="john@example.com" />);
    const button = screen.getByRole('button', { name: /John Doe/i });

    expect(button).toHaveAttribute('aria-expanded', 'false');

    fireEvent.click(button);

    await waitFor(() => {
      expect(button).toHaveAttribute('aria-expanded', 'true');
    });
  });
});
