import { NextRequest, NextResponse } from 'next/server';

// Routes that don't require authentication
const publicRoutes = ['/login', '/register'];

// Prefixes that shouldn't be protected
const publicPrefixes = ['/_next', '/static'];

export function middleware(request: NextRequest) {
  const pathname = request.nextUrl.pathname;

  // Allow public prefixes (Next.js internals, static files)
  if (publicPrefixes.some((prefix) => pathname.startsWith(prefix))) {
    return NextResponse.next();
  }

  // Allow API routes to pass through (they handle their own auth)
  if (pathname.startsWith('/api')) {
    return NextResponse.next();
  }

  // Note: Since we're using localStorage for tokens (client-side),
  // we can't fully enforce authentication in middleware.
  // The AuthContext on the client will handle:
  // - Redirecting unauthenticated users from protected routes to /login
  // - Redirecting authenticated users away from /login and /register
  // - Token validation via GET /api/auth/me on app load

  // Middleware just allows all traffic to pass through
  return NextResponse.next();
}

export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     */
    '/((?!_next/static|_next/image|favicon.ico).*)',
  ],
};
