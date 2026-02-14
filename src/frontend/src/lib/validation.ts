import { z } from 'zod';

// Registration schema matching backend validation rules
export const registerSchema = z
  .object({
    displayName: z
      .string()
      .min(1, 'Display name is required')
      .max(100, 'Display name must not exceed 100 characters'),
    email: z
      .string()
      .min(1, 'Email is required')
      .email('Email must be a valid email address'),
    password: z
      .string()
      .min(1, 'Password is required')
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
      .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
      .regex(/[0-9]/, 'Password must contain at least one digit'),
    confirmPassword: z
      .string()
      .min(1, 'Confirm password is required'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

export type RegisterFormData = z.infer<typeof registerSchema>;

// Login schema
export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required')
    .email('Email must be a valid email address'),
  password: z
    .string()
    .min(1, 'Password is required'),
});

export type LoginFormData = z.infer<typeof loginSchema>;
