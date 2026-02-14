'use client';

import {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
  useCallback,
} from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { apiClient } from '@/services/apiClient';

export interface User {
  id: string;
  email: string;
  displayName: string;
  createdAt?: string;
}

export interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (token: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Validate token and fetch current user
  const validateToken = useCallback(async () => {
    try {
      // Check if token exists in localStorage
      if (typeof window === 'undefined') return;

      const token = localStorage.getItem('authToken');
      if (!token) {
        setUser(null);
        setIsLoading(false);
        return;
      }

      // Call GET /api/auth/me to validate token and get user data
      const response = await apiClient.get<User>('/auth/me');
      setUser(response.data);
      setIsLoading(false);
    } catch (error: any) {
      // Token is invalid or expired
      if (error.statusCode === 401 || error.statusCode === 404) {
        localStorage.removeItem('authToken');
        setUser(null);
        // Redirect to login only if not already there
        if (pathname && pathname !== '/login' && pathname !== '/register') {
          router.push('/login');
        }
      }
      setIsLoading(false);
    }
  }, [pathname, router]);

  // On mount, validate existing token
  useEffect(() => {
    validateToken();
  }, [validateToken]);

  // Login: store token and fetch user data
  const login = useCallback(async (token: string) => {
    localStorage.setItem('authToken', token);
    try {
      const response = await apiClient.get<User>('/auth/me');
      setUser(response.data);
    } catch (error) {
      // If token is invalid, clear it
      localStorage.removeItem('authToken');
      setUser(null);
      throw error;
    }
  }, []);

  // Logout: clear token and reset state
  const logout = useCallback(async () => {
    localStorage.removeItem('authToken');
    setUser(null);
    router.push('/login');
  }, [router]);

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// Hook to use auth context
export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
