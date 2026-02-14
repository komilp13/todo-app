/**
 * Centralized API Client for the GTD Todo Application
 * Handles JWT token injection, error handling, and base URL configuration
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

interface FetchOptions extends RequestInit {
  skipAuth?: boolean;
}

interface ApiResponse<T> {
  data: T;
  status: number;
}

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  /**
   * Get JWT token from localStorage
   */
  private getToken(): string | null {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem('authToken');
  }

  /**
   * Build headers with JWT token if available
   */
  private getHeaders(options: FetchOptions = {}): HeadersInit {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    if (options.headers instanceof Headers) {
      options.headers.forEach((value, key) => {
        headers[key] = value;
      });
    } else if (options.headers) {
      Object.assign(headers, options.headers);
    }

    if (!options.skipAuth) {
      const token = this.getToken();
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
    }

    return headers;
  }

  /**
   * Generic fetch wrapper with error handling
   */
  private async request<T>(
    endpoint: string,
    options: FetchOptions = {}
  ): Promise<{ data: T; status: number }> {
    const url = `${this.baseUrl}${endpoint}`;

    try {
      const response = await fetch(url, {
        ...options,
        headers: this.getHeaders(options),
      });

      const data = await response.json().catch(() => ({}));

      if (!response.ok) {
        if (response.status === 401) {
          // Unauthorized - clear token and redirect to login
          localStorage.removeItem('authToken');
          if (typeof window !== 'undefined') {
            window.location.href = '/login';
          }
        }

        throw new ApiError(
          data.message || 'API request failed',
          response.status,
          data
        );
      }

      return { data: data as T, status: response.status };
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }

      throw new ApiError(
        'Network error or invalid JSON response',
        0,
        error
      );
    }
  }

  /**
   * GET request
   */
  get<T>(endpoint: string, options?: FetchOptions): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'GET',
    });
  }

  /**
   * POST request
   */
  post<T>(
    endpoint: string,
    body?: unknown,
    options?: FetchOptions
  ): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  /**
   * PUT request
   */
  put<T>(
    endpoint: string,
    body?: unknown,
    options?: FetchOptions
  ): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  /**
   * PATCH request
   */
  patch<T>(
    endpoint: string,
    body?: unknown,
    options?: FetchOptions
  ): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PATCH',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  /**
   * DELETE request
   */
  delete<T>(endpoint: string, options?: FetchOptions): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'DELETE',
    });
  }
}

/**
 * Custom error class for API errors
 */
export class ApiError extends Error {
  public readonly statusCode: number;
  public readonly details: unknown;

  constructor(message: string, statusCode: number, details?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.details = details;
  }
}

// Export singleton instance
export const apiClient = new ApiClient();
