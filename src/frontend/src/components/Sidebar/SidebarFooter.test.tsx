import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import SidebarFooter from './SidebarFooter';
import { useAuth } from '@/contexts/AuthContext';

// Mock the useAuth hook
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}));

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

describe('SidebarFooter Component', () => {
  const mockLogout = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    mockLogout.mockResolvedValue(undefined);
  });

  it('renders logout button', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    render(<SidebarFooter />);
    const logoutButton = screen.getByRole('button', { name: /logout/i });
    expect(logoutButton).toBeInTheDocument();
  });

  it('calls logout function when logout button is clicked', async () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    render(<SidebarFooter />);
    const logoutButton = screen.getByRole('button', { name: /logout/i });

    fireEvent.click(logoutButton);

    await waitFor(() => {
      expect(mockLogout).toHaveBeenCalled();
    });
  });

  it('has proper styling for logout button', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    render(<SidebarFooter />);
    const logoutButton = screen.getByRole('button', { name: /logout/i });
    expect(logoutButton).toHaveClass('w-full', 'rounded-lg', 'bg-gray-100', 'px-4', 'py-2');
  });

  it('has border-top separator', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    const { container } = render(<SidebarFooter />);
    const footer = container.firstChild as HTMLElement;
    expect(footer).toHaveClass('border-t', 'border-gray-200');
  });

  it('has proper padding', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    const { container } = render(<SidebarFooter />);
    const footer = container.firstChild as HTMLElement;
    expect(footer).toHaveClass('px-4', 'py-4');
  });

  it('button text is "Logout"', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    render(<SidebarFooter />);
    expect(screen.getByRole('button')).toHaveTextContent('Logout');
  });

  it('button has hover effect', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    render(<SidebarFooter />);
    const logoutButton = screen.getByRole('button', { name: /logout/i });
    expect(logoutButton).toHaveClass('hover:bg-gray-200');
  });

  it('button has transition effect', () => {
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
    });

    render(<SidebarFooter />);
    const logoutButton = screen.getByRole('button', { name: /logout/i });
    expect(logoutButton).toHaveClass('transition-colors');
  });
});
