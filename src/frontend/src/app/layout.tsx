import type { Metadata } from 'next';
import '../styles/globals.css';
import { AuthProvider } from '@/contexts/AuthContext';
import { SidebarProvider } from '@/contexts/SidebarContext';
import { TaskCreateModalProvider } from '@/contexts/TaskCreateModalContext';
import { ProjectModalProvider } from '@/contexts/ProjectModalContext';
import { TaskRefreshProvider } from '@/contexts/TaskRefreshContext';
import AuthenticatedLayout from '@/components/AuthenticatedLayout';

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
      <body className="h-screen overflow-hidden">
        <AuthProvider>
          <SidebarProvider>
            <TaskCreateModalProvider>
              <ProjectModalProvider>
                <TaskRefreshProvider>
                  <AuthenticatedLayout>{children}</AuthenticatedLayout>
                </TaskRefreshProvider>
              </ProjectModalProvider>
            </TaskCreateModalProvider>
          </SidebarProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
