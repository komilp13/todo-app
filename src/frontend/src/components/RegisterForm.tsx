'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import Link from 'next/link';
import { registerSchema, type RegisterFormData } from '@/lib/validation';
import { apiClient } from '@/services/apiClient';
import { useAuth } from '@/contexts/AuthContext';
import { useApiHealth } from '@/hooks/useApiHealth';

export default function RegisterForm() {
  const router = useRouter();
  const { login } = useAuth();
  const [apiError, setApiError] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { isHealthy, error: healthError } = useApiHealth();

  const {
    register,
    handleSubmit,
    formState: { errors },
    setError,
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    setIsSubmitting(true);
    setApiError('');

    try {
      const response = await apiClient.post<any>('/auth/register', {
        email: data.email,
        password: data.password,
        displayName: data.displayName,
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
      } else if (error.statusCode === 409) {
        setError('email', {
          type: 'manual',
          message: 'Email already registered',
        });
      } else if (error.statusCode === 400 && error.details?.errors) {
        // Display first validation error
        const firstError = Object.values(error.details.errors)[0] as string[];
        if (firstError && firstError[0]) {
          setApiError(firstError[0]);
        }
      } else {
        setApiError(
          error.details?.message || error.message || 'Registration failed. Please try again.'
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
        <label
          htmlFor="displayName"
          className="block text-sm font-medium text-gray-700"
        >
          Display Name
        </label>
        <input
          id="displayName"
          type="text"
          placeholder="Your Name"
          {...register('displayName')}
          className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-blue-500"
          disabled={isSubmitting}
        />
        {errors.displayName && (
          <p className="mt-1 text-sm text-red-600">{errors.displayName.message}</p>
        )}
      </div>

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
        <label
          htmlFor="password"
          className="block text-sm font-medium text-gray-700"
        >
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
        <p className="mt-1 text-xs text-gray-500">
          At least 8 characters with uppercase, lowercase, and number
        </p>
      </div>

      <div>
        <label
          htmlFor="confirmPassword"
          className="block text-sm font-medium text-gray-700"
        >
          Confirm Password
        </label>
        <input
          id="confirmPassword"
          type="password"
          placeholder="••••••••"
          {...register('confirmPassword')}
          className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-blue-500"
          disabled={isSubmitting}
        />
        {errors.confirmPassword && (
          <p className="mt-1 text-sm text-red-600">{errors.confirmPassword.message}</p>
        )}
      </div>

      <button
        type="submit"
        disabled={isSubmitting}
        className="w-full rounded-md bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
      >
        {isSubmitting ? 'Creating Account...' : 'Sign Up'}
      </button>

      <p className="text-center text-sm text-gray-600">
        Already have an account?{' '}
        <Link
          href="/login"
          className="font-medium text-blue-600 hover:text-blue-500"
        >
          Sign In
        </Link>
      </p>
    </form>
  );
}
