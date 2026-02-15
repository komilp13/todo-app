/**
 * TaskRefreshContext
 * Manages task list refresh callbacks
 * Allows pages to register refresh functions that get triggered when a task is created
 */

'use client';

import { createContext, useContext, ReactNode, useCallback, useRef } from 'react';

interface TaskRefreshContextType {
  registerRefreshCallback: (pageId: string, callback: () => void | Promise<void>) => void;
  unregisterRefreshCallback: (pageId: string) => void;
  triggerRefresh: () => Promise<void>;
}

const TaskRefreshContext = createContext<TaskRefreshContextType | undefined>(
  undefined
);

export function TaskRefreshProvider({ children }: { children: ReactNode }) {
  const callbacksRef = useRef<Map<string, () => void | Promise<void>>>(new Map());

  const registerRefreshCallback = useCallback(
    (pageId: string, callback: () => void | Promise<void>) => {
      callbacksRef.current.set(pageId, callback);
    },
    []
  );

  const unregisterRefreshCallback = useCallback((pageId: string) => {
    callbacksRef.current.delete(pageId);
  }, []);

  const triggerRefresh = useCallback(async () => {
    const callbacks = Array.from(callbacksRef.current.values());

    // Execute all registered callbacks in parallel
    await Promise.all(
      callbacks.map((callback) => {
        try {
          return Promise.resolve(callback());
        } catch (error) {
          console.error('Error in task refresh callback:', error);
          return Promise.resolve();
        }
      })
    );
  }, []);

  return (
    <TaskRefreshContext.Provider
      value={{
        registerRefreshCallback,
        unregisterRefreshCallback,
        triggerRefresh,
      }}
    >
      {children}
    </TaskRefreshContext.Provider>
  );
}

export function useTaskRefreshContext() {
  const context = useContext(TaskRefreshContext);
  if (!context) {
    throw new Error(
      'useTaskRefreshContext must be used within TaskRefreshProvider'
    );
  }
  return context;
}
