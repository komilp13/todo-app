'use client';

import { createContext, useContext, ReactNode, useState, useCallback } from 'react';

interface ProjectToEdit {
  id: string;
  name: string;
  description?: string;
  dueDate?: string;
}

interface ProjectModalContextType {
  isOpen: boolean;
  editingProject: ProjectToEdit | null;
  openCreateModal: () => void;
  openEditModal: (project: ProjectToEdit) => void;
  closeModal: () => void;
}

const ProjectModalContext = createContext<ProjectModalContextType | undefined>(
  undefined
);

export function ProjectModalProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);
  const [editingProject, setEditingProject] = useState<ProjectToEdit | null>(null);

  const openCreateModal = useCallback(() => {
    setEditingProject(null);
    setIsOpen(true);
  }, []);

  const openEditModal = useCallback((project: ProjectToEdit) => {
    setEditingProject(project);
    setIsOpen(true);
  }, []);

  const closeModal = useCallback(() => {
    setIsOpen(false);
    setEditingProject(null);
  }, []);

  return (
    <ProjectModalContext.Provider
      value={{ isOpen, editingProject, openCreateModal, openEditModal, closeModal }}
    >
      {children}
    </ProjectModalContext.Provider>
  );
}

export function useProjectModalContext() {
  const context = useContext(ProjectModalContext);
  if (!context) {
    throw new Error(
      'useProjectModalContext must be used within ProjectModalProvider'
    );
  }
  return context;
}
