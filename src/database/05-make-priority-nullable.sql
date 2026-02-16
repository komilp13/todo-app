-- ============================================================================
-- GTD Todo Application - Make Priority Column Nullable
-- ============================================================================
-- Priority is now an optional field. New tasks will have NULL priority by default.
-- Existing tasks with P4 are left as-is (they were intentionally or historically set).
--
-- Priority enum values: P1=1, P2=2, P3=3, P4=4, NULL=no priority

SET search_path = public;

-- Remove the NOT NULL constraint and default value from the priority column
ALTER TABLE tasks
    ALTER COLUMN priority DROP NOT NULL,
    ALTER COLUMN priority DROP DEFAULT;
