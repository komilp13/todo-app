'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import Link from 'next/link';
import { loginSchema, type LoginFormData } from '@/lib/validation';
import { apiClient } from '@/services/apiClient';
import { useAuth } from '@/contexts/AuthContext';
import { useApiHealth } from '@/hooks/useApiHealth';

export default function LoginForm() {
  const router = useRouter();
  const { login } = useAuth();
  const [apiError, setApiError] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { isHealthy, error: healthError } = useApiHealth();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsSubmitting(true);
    setApiError('');

    try {
      const response = await apiClient.post<any>('/auth/login', {
        email: data.email,
        password: data.password,
      }, { skipAuth: true });

      if (response.data && response.data.token) {
        // Use auth context login to store token and fetch user
        await login(response.data.token);
        // Redirect to Inbox
        router.push('/inbox');
      }
    } catch (error: any) {
      // Handle different error scenarios
      if (error.statusCode === 0) {
        // Network error - API is unreachable
        const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
        setApiError(
          `Cannot connect to the server at ${apiUrl}. Please ensure the backend is running.`
        );
        console.error('Network error - Backend unreachable:', apiUrl, error);
      } else if (error.statusCode === 401) {
        setApiError('Invalid email or password');
      } else if (error.statusCode === 400) {
        setApiError(
          error.details?.errors?.Email?.[0] ||
          error.details?.message ||
          'Please check your email and password'
        );
      } else {
        setApiError(
          error.details?.message || error.message || 'Login failed. Please try again.'
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="w-full max-w-md space-y-4">
      {(apiError || healthError) && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">
          {apiError || healthError}
        </div>
      )}

      <div>
        <label htmlFor="email" className="block text-sm font-medium text-gray-700">
          Email
        </label>
        <input
          id="email"
          type="email"
          placeholder="you@example.com"
          {...register('email')}
          className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-blue-500"
          disabled={isSubmitting}
        />
        {errors.email && (
          <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>
        )}
      </div>

      <div>
        <label htmlFor="password" className="block text-sm font-medium text-gray-700">
          Password
        </label>
        <input
          id="password"
          type="password"
          placeholder="••••••••"
          {...register('password')}
          className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-blue-500"
          disabled={isSubmitting}
        />
        {errors.password && (
          <p className="mt-1 text-sm text-red-600">{errors.password.message}</p>
        )}
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full rounded-md bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
      >
        {isSubmitting ? 'Signing In...' : 'Sign In'}
      </button>

      <p className="text-center text-sm text-gray-600">
        Don't have an account?{' '}
        <Link
          href="/register"
          className="font-medium text-blue-600 hover:text-blue-500"
        >
          Sign Up
        </Link>
      </p>
    </form>
  );
}
