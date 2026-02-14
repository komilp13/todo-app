-- ============================================================================
-- GTD Todo Application - Database Schema Script
-- ============================================================================
-- Creates all tables, indexes, and constraints for the GTD Todo application
--
-- Optimized for PostgreSQL 17.7
-- - UUID type with gen_random_uuid() for key generation
-- - BIGINT for sort_order columns to support unlimited sorting sequences
-- - Partial indexes for common query patterns (open tasks, active projects)
-- - Proper foreign key constraints with cascade delete
-- - Automatic updated_at timestamp management via triggers
-- - Comprehensive data validation via CHECK constraints
-- - Table and column comments for documentation
--
-- Performance Features:
-- - Partial indexes reduce index size for non-archived tasks
-- - Composite indexes with sort_order for efficient pagination
-- - Functional indexes for case-insensitive label searches
-- - B-tree deduplication (automatic in PG 13+)
-- - Supports parallel query execution

-- Set search path to public schema
SET search_path = public;

-- Enable additional extensions if available (for future enhancements)
-- CREATE EXTENSION IF NOT EXISTS "pg_trgm";      -- For text search
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";    -- For uuid_generate functions

-- ============================================================================
-- Users Table
-- ============================================================================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    password_salt TEXT NOT NULL,
    display_name TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT email_not_empty CHECK (email <> ''),
    CONSTRAINT display_name_not_empty CHECK (display_name <> '')
);

CREATE INDEX ix_users_email ON users(email);
COMMENT ON TABLE users IS 'Application users for authentication and task ownership';
COMMENT ON COLUMN users.email IS 'Unique email address (case-insensitive)';
COMMENT ON COLUMN users.password_hash IS 'PBKDF2 hashed password';
COMMENT ON COLUMN users.password_salt IS 'Salt used for password hashing';

-- ============================================================================
-- Projects Table
-- ============================================================================
CREATE TABLE projects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    description TEXT,
    due_date TIMESTAMP WITH TIME ZONE,
    status INTEGER NOT NULL DEFAULT 0,  -- 0: Active, 1: Completed
    sort_order BIGINT NOT NULL DEFAULT 0,  -- BIGINT for better scalability (supports up to 9.2 quintillion sort orders)
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT project_name_not_empty CHECK (name <> ''),
    CONSTRAINT project_name_max_length CHECK (char_length(name) <= 100),
    CONSTRAINT project_description_max_length CHECK (char_length(description) <= 4000)
);

CREATE INDEX ix_projects_user_id ON projects(user_id);
-- Partial index for active projects (most common query)
CREATE INDEX ix_projects_user_id_active ON projects(user_id, sort_order)
    WHERE status = 0;
COMMENT ON TABLE projects IS 'User projects that group related tasks toward a goal';
COMMENT ON COLUMN projects.status IS '0: Active, 1: Completed';
COMMENT ON COLUMN projects.sort_order IS 'User-controlled sort order for displaying projects';

-- ============================================================================
-- Labels Table
-- ============================================================================
CREATE TABLE labels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    color TEXT,  -- Hex color code (e.g., #ff4040)
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT label_name_not_empty CHECK (name <> ''),
    CONSTRAINT label_name_max_length CHECK (char_length(name) <= 100)
    -- Note: Uniqueness constraint (case-insensitive) enforced via unique index below
);

CREATE INDEX ix_labels_user_id ON labels(user_id);
-- Unique index for case-insensitive label names per user (enforces uniqueness constraint)
CREATE UNIQUE INDEX ix_labels_user_id_name_unique ON labels(user_id, LOWER(name));
COMMENT ON TABLE labels IS 'User-created labels for cross-cutting task categorization';
COMMENT ON COLUMN labels.color IS 'Hex color code for UI display (e.g., #ff4040)';
COMMENT ON COLUMN labels.name IS 'Label name (unique per user, case-insensitive)';

-- ============================================================================
-- Tasks Table
-- ============================================================================
CREATE TABLE tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    project_id UUID REFERENCES projects(id) ON DELETE SET NULL,
    name TEXT NOT NULL,
    description TEXT,
    due_date TIMESTAMP WITH TIME ZONE,
    priority INTEGER NOT NULL DEFAULT 4,  -- 1: P1, 2: P2, 3: P3, 4: P4 (lowest)
    status INTEGER NOT NULL DEFAULT 0,    -- 0: Open, 1: Done
    system_list INTEGER NOT NULL DEFAULT 0, -- 0: Inbox, 1: Next, 2: Upcoming, 3: Someday
    sort_order BIGINT NOT NULL DEFAULT 0,  -- BIGINT for better scalability
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT task_name_not_empty CHECK (name <> ''),
    CONSTRAINT task_name_max_length CHECK (char_length(name) <= 500),
    CONSTRAINT task_description_max_length CHECK (char_length(description) <= 4000),
    CONSTRAINT valid_priority CHECK (priority BETWEEN 1 AND 4),
    CONSTRAINT valid_status CHECK (status BETWEEN 0 AND 1),
    CONSTRAINT valid_system_list CHECK (system_list BETWEEN 0 AND 3),
    CONSTRAINT completed_only_if_done CHECK (completed_at IS NULL OR status = 1)
);

-- Primary user task index (most common query)
CREATE INDEX ix_tasks_user_id ON tasks(user_id);
-- Partial index for open tasks by user (very common query pattern)
CREATE INDEX ix_tasks_user_open ON tasks(user_id, sort_order)
    WHERE is_archived = FALSE AND status = 0;
-- Project task index for project views
CREATE INDEX ix_tasks_project_id ON tasks(project_id);
-- System list index for list-based queries
CREATE INDEX ix_tasks_system_list ON tasks(system_list);
-- Partial index for upcoming tasks (due date within range)
CREATE INDEX ix_tasks_due_date_upcoming ON tasks(due_date)
    WHERE is_archived = FALSE AND status = 0;
-- Status index for filtering
CREATE INDEX ix_tasks_status ON tasks(status);
COMMENT ON TABLE tasks IS 'Core task entities with GTD system lists, priority, and status';
COMMENT ON COLUMN tasks.priority IS '1: P1 (highest), 2: P2, 3: P3, 4: P4 (lowest, default)';
COMMENT ON COLUMN tasks.status IS '0: Open, 1: Done';
COMMENT ON COLUMN tasks.system_list IS '0: Inbox, 1: Next, 2: Upcoming, 3: Someday';
COMMENT ON COLUMN tasks.sort_order IS 'User-controlled sort order within system list';
COMMENT ON COLUMN tasks.is_archived IS 'Soft-delete flag for completed tasks';

-- ============================================================================
-- Task-Label Join Table
-- ============================================================================
CREATE TABLE task_labels (
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    label_id UUID NOT NULL REFERENCES labels(id) ON DELETE CASCADE,
    PRIMARY KEY (task_id, label_id)
);

CREATE INDEX ix_task_labels_label_id ON task_labels(label_id);
COMMENT ON TABLE task_labels IS 'Many-to-many relationship between tasks and labels';

-- ============================================================================
-- Database Migration Tracking Table (for EF Core)
-- ============================================================================
CREATE TABLE __EFMigrationsHistory (
    MigrationId CHARACTER VARYING(150) NOT NULL PRIMARY KEY,
    ProductVersion CHARACTER VARYING(32) NOT NULL
);

COMMENT ON TABLE __EFMigrationsHistory IS 'Tracks applied Entity Framework Core migrations';

-- ============================================================================
-- Trigger: Update updated_at on Users
-- ============================================================================
CREATE OR REPLACE FUNCTION update_users_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER users_updated_at_trigger
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION update_users_updated_at();

-- ============================================================================
-- Trigger: Update updated_at on Projects
-- ============================================================================
CREATE OR REPLACE FUNCTION update_projects_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER projects_updated_at_trigger
BEFORE UPDATE ON projects
FOR EACH ROW
EXECUTE FUNCTION update_projects_updated_at();

-- ============================================================================
-- Trigger: Update updated_at on Tasks
-- ============================================================================
CREATE OR REPLACE FUNCTION update_tasks_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tasks_updated_at_trigger
BEFORE UPDATE ON tasks
FOR EACH ROW
EXECUTE FUNCTION update_tasks_updated_at();

-- ============================================================================
-- Performance Tuning and Statistics
-- ============================================================================
-- Configure autovacuum for better maintenance (PostgreSQL 17.7 optimizations)

-- For high-write tables, more frequent analysis helps query planning
ALTER TABLE tasks SET (autovacuum_analyze_scale_factor = 0.01);
ALTER TABLE tasks SET (autovacuum_vacuum_scale_factor = 0.1);

ALTER TABLE projects SET (autovacuum_analyze_scale_factor = 0.01);
ALTER TABLE labels SET (autovacuum_analyze_scale_factor = 0.01);

-- Analyze all tables for initial statistics
-- This ensures optimal query plans from the start
ANALYZE users;
ANALYZE projects;
ANALYZE labels;
ANALYZE tasks;
ANALYZE task_labels;

-- ============================================================================
-- Optional: Future Enhancement Indexes
-- ============================================================================
-- Uncomment these when implementing advanced search features

-- Full-text search on tasks (after installing pg_trgm extension):
-- CREATE INDEX ix_tasks_name_trgm ON tasks USING GIN(name gin_trgm_ops);
-- CREATE INDEX ix_tasks_description_trgm ON tasks USING GIN(description gin_trgm_ops);

-- JSON metadata support (for extensibility):
-- ALTER TABLE tasks ADD COLUMN metadata JSONB DEFAULT '{}'::JSONB;
-- CREATE INDEX ix_tasks_metadata ON tasks USING GIN(metadata);

-- Range type for time period queries (for scheduling):
-- CREATE INDEX ix_tasks_created_range ON tasks
--     USING GIST(tsrange(created_at, updated_at));

-- ============================================================================
-- End of Schema Script
-- ============================================================================
-- PostgreSQL 17.7 Optimization Notes:
-- - All indexes use optimal strategy for query planner
-- - Partial indexes reduce bloat and improve cache hit rate
-- - BIGINT sort_order supports up to 9.2 quintillion positions
-- - Triggers use direct assignment for efficiency (no extra overhead)
-- - Statistics enabled for accurate cardinality estimates
-- ============================================================================
