-- ============================================================================
-- GTD Todo Application - Database Schema Script
-- ============================================================================
-- Creates all tables, indexes, and constraints for the GTD Todo application

-- Set search path to public schema
SET search_path = public;

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
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT project_name_not_empty CHECK (name <> ''),
    CONSTRAINT project_name_max_length CHECK (char_length(name) <= 100),
    CONSTRAINT project_description_max_length CHECK (char_length(description) <= 4000)
);

CREATE INDEX ix_projects_user_id ON projects(user_id);
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
    CONSTRAINT label_name_max_length CHECK (char_length(name) <= 100),
    CONSTRAINT unique_label_per_user UNIQUE (user_id, LOWER(name))
);

CREATE INDEX ix_labels_user_id ON labels(user_id);
CREATE INDEX ix_labels_user_id_name_unique ON labels(user_id, LOWER(name)) WHERE LOWER(name) IS NOT NULL;
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
    sort_order INTEGER NOT NULL DEFAULT 0,
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

CREATE INDEX ix_tasks_user_id ON tasks(user_id);
CREATE INDEX ix_tasks_project_id ON tasks(project_id);
CREATE INDEX ix_tasks_system_list ON tasks(system_list);
CREATE INDEX ix_tasks_status ON tasks(status);
CREATE INDEX ix_tasks_due_date ON tasks(due_date);
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
-- End of Schema Script
-- ============================================================================
