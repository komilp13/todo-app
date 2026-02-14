import { useEffect, useState } from 'react';

const SIDEBAR_COLLAPSE_KEY = 'sidebar-collapsed';

/**
 * useSidebarState Hook
 * Manages sidebar collapse state with localStorage persistence
 * Only used on desktop (>= 1024px)
 */
export function useSidebarState() {
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isLoaded, setIsLoaded] = useState(false);

  // Load initial state from localStorage
  useEffect(() => {
    if (typeof window === 'undefined') return;

    try {
      const saved = localStorage.getItem(SIDEBAR_COLLAPSE_KEY);
      if (saved !== null) {
        setIsCollapsed(JSON.parse(saved));
      }
    } catch (error) {
      console.warn('Failed to load sidebar state from localStorage:', error);
    }

    setIsLoaded(true);
  }, []);

  // Save to localStorage whenever state changes
  useEffect(() => {
    if (!isLoaded || typeof window === 'undefined') return;

    try {
      localStorage.setItem(SIDEBAR_COLLAPSE_KEY, JSON.stringify(isCollapsed));
    } catch (error) {
      console.warn('Failed to save sidebar state to localStorage:', error);
    }
  }, [isCollapsed, isLoaded]);

  const toggleCollapse = () => {
    setIsCollapsed(!isCollapsed);
  };

  return { isCollapsed, setIsCollapsed, toggleCollapse, isLoaded };
}
