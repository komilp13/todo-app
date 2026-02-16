-- ============================================================================
-- GTD Todo Application - Migrate Upcoming Tasks to Inbox
-- ============================================================================
-- Upcoming is now a computed view (shows tasks with due dates), not a system list
-- that tasks can be manually moved into. This migration moves all existing tasks
-- with system_list = 2 (Upcoming) to system_list = 0 (Inbox).
--
-- System list enum values: Inbox=0, Next=1, Upcoming=2, Someday=3

SET search_path = public;

UPDATE tasks
SET system_list = 0,
    updated_at = NOW()
WHERE system_list = 2;
