import type { Metadata } from 'next';
import '../styles/globals.css';
import { AuthProvider } from '@/contexts/AuthContext';

export const metadata: Metadata = {
  title: 'GTD Todo - Getting Things Done',
  description:
    'A web-based Getting Things Done (GTD) todo app with multi-user support, system lists, projects, and labels.',
  keywords: ['todo', 'gtd', 'productivity', 'tasks', 'getting-things-done'],
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
