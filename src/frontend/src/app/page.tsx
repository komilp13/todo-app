'use client';

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-4 bg-gray-50">
      <div className="w-full max-w-md space-y-8 text-center">
        <div>
          <h1 className="text-4xl font-bold text-gray-900">GTD Todo</h1>
          <p className="mt-2 text-gray-600">
            Getting Things Done - Organize your tasks and projects
          </p>
        </div>

        <div className="space-y-4">
          <button
            className="w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            onClick={() => (window.location.href = '/login')}
          >
            Login
          </button>
          <button
            className="w-full px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
            onClick={() => (window.location.href = '/register')}
          >
            Register
          </button>
        </div>

        <div className="text-sm text-gray-500">
          <p>Frontend is loading...</p>
          <p className="mt-2">
            API Status:{' '}
            <span className="inline-block px-2 py-1 bg-yellow-100 text-yellow-800 rounded">
              Checking...
            </span>
          </p>
        </div>
      </div>
    </main>
  );
}
