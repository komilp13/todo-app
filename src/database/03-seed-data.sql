-- ============================================================================
-- GTD Todo Application - Development Seed Data Script
-- ============================================================================
-- Populates the database with realistic test data for development
-- NOTE: These are placeholder password hashes. See README for real credentials.

SET search_path = public;

-- ============================================================================
-- Seed Users
-- ============================================================================
-- Password for both users: 'DemoPassword123' (hashed using PBKDF2)
-- DO NOT use these in production - generate real hashes with the password hashing service

INSERT INTO users (id, email, password_hash, password_salt, display_name, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440000', 'alice@example.com',
     'hashed_demo_password_alice', 'salt_alice', 'Alice Johnson',
     CURRENT_TIMESTAMP - INTERVAL '30 days', CURRENT_TIMESTAMP - INTERVAL '1 day'),
    ('550e8400-e29b-41d4-a716-446655440001', 'bob@example.com',
     'hashed_demo_password_bob', 'salt_bob', 'Bob Smith',
     CURRENT_TIMESTAMP - INTERVAL '20 days', CURRENT_TIMESTAMP - INTERVAL '2 days');

-- ============================================================================
-- Seed Projects for Alice
-- ============================================================================
INSERT INTO projects (id, user_id, name, description, due_date, status, sort_order, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440010',
     '550e8400-e29b-41d4-a716-446655440000',
     'Q1 Goals',
     'Quarterly objectives',
     CURRENT_TIMESTAMP + INTERVAL '90 days',
     0, 0, CURRENT_TIMESTAMP - INTERVAL '15 days', CURRENT_TIMESTAMP - INTERVAL '1 day'),
    ('550e8400-e29b-41d4-a716-446655440011',
     '550e8400-e29b-41d4-a716-446655440000',
     'Website Redesign',
     'Modernize company website',
     CURRENT_TIMESTAMP + INTERVAL '60 days',
     0, 1, CURRENT_TIMESTAMP - INTERVAL '10 days', CURRENT_TIMESTAMP - INTERVAL '2 days');

-- ============================================================================
-- Seed Labels for Alice
-- ============================================================================
INSERT INTO labels (id, user_id, name, color, created_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440020',
     '550e8400-e29b-41d4-a716-446655440000', 'Work', '#ff4040', CURRENT_TIMESTAMP - INTERVAL '25 days'),
    ('550e8400-e29b-41d4-a716-446655440021',
     '550e8400-e29b-41d4-a716-446655440000', 'Personal', '#4073ff', CURRENT_TIMESTAMP - INTERVAL '25 days'),
    ('550e8400-e29b-41d4-a716-446655440022',
     '550e8400-e29b-41d4-a716-446655440000', 'Urgent', '#ff9933', CURRENT_TIMESTAMP - INTERVAL '25 days'),
    ('550e8400-e29b-41d4-a716-446655440023',
     '550e8400-e29b-41d4-a716-446655440000', 'Reading', '#44bb00', CURRENT_TIMESTAMP - INTERVAL '25 days');

-- ============================================================================
-- Seed Tasks for Alice - Inbox
-- ============================================================================
INSERT INTO tasks (id, user_id, project_id, name, description, due_date, priority, status, system_list, sort_order, is_archived, completed_at, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440030',
     '550e8400-e29b-41d4-a716-446655440000',
     '550e8400-e29b-41d4-a716-446655440010',
     'Review Q1 budget',
     'Check quarterly spending',
     NULL, 2, 0, 0, 0, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '3 days', CURRENT_TIMESTAMP - INTERVAL '3 days'),
    ('550e8400-e29b-41d4-a716-446655440031',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Send client proposal',
     'Email proposal to Acme Corp',
     NULL, 1, 0, 0, 1, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '2 days', CURRENT_TIMESTAMP - INTERVAL '2 days'),
    ('550e8400-e29b-41d4-a716-446655440032',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Update dependencies',
     NULL,
     NULL, 3, 0, 0, 2, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '1 day', CURRENT_TIMESTAMP - INTERVAL '1 day');

-- ============================================================================
-- Seed Tasks for Alice - Next
-- ============================================================================
INSERT INTO tasks (id, user_id, project_id, name, description, due_date, priority, status, system_list, sort_order, is_archived, completed_at, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440033',
     '550e8400-e29b-41d4-a716-446655440000',
     '550e8400-e29b-41d4-a716-446655440011',
     'Complete project kickoff',
     'Schedule team meeting',
     NULL, 1, 0, 1, 0, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP - INTERVAL '5 days'),
    ('550e8400-e29b-41d4-a716-446655440034',
     '550e8400-e29b-41d4-a716-446655440000',
     '550e8400-e29b-41d4-a716-446655440011',
     'Write technical specification',
     NULL,
     NULL, 2, 0, 1, 1, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '4 days', CURRENT_TIMESTAMP - INTERVAL '4 days'),
    ('550e8400-e29b-41d4-a716-446655440035',
     '550e8400-e29b-41d4-a716-446655440000',
     '550e8400-e29b-41d4-a716-446655440011',
     'Design homepage mockup',
     NULL,
     NULL, 3, 0, 1, 2, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '3 days', CURRENT_TIMESTAMP - INTERVAL '3 days'),
    ('550e8400-e29b-41d4-a716-446655440036',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Fix login bug',
     'Users unable to reset password',
     NULL, 1, 0, 1, 3, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '2 days', CURRENT_TIMESTAMP - INTERVAL '2 days'),
    ('550e8400-e29b-41d4-a716-446655440037',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Write unit tests',
     NULL,
     NULL, 2, 0, 1, 4, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '1 day', CURRENT_TIMESTAMP - INTERVAL '1 day');

-- ============================================================================
-- Seed Tasks for Alice - Upcoming
-- ============================================================================
INSERT INTO tasks (id, user_id, project_id, name, description, due_date, priority, status, system_list, sort_order, is_archived, completed_at, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440038',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Q1 performance reviews',
     'Schedule 1-on-1s',
     CURRENT_TIMESTAMP + INTERVAL '5 days',
     2, 0, 2, 0, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '7 days', CURRENT_TIMESTAMP - INTERVAL '7 days'),
    ('550e8400-e29b-41d4-a716-446655440039',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Team lunch planning',
     'Decide on restaurant',
     CURRENT_TIMESTAMP + INTERVAL '3 days',
     4, 0, 2, 1, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP - INTERVAL '5 days'),
    ('550e8400-e29b-41d4-a716-446655440040',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Submit expense report',
     NULL,
     CURRENT_TIMESTAMP + INTERVAL '2 days',
     3, 0, 2, 2, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '3 days', CURRENT_TIMESTAMP - INTERVAL '3 days');

-- ============================================================================
-- Seed Tasks for Alice - Someday
-- ============================================================================
INSERT INTO tasks (id, user_id, project_id, name, description, due_date, priority, status, system_list, sort_order, is_archived, completed_at, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440041',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Learn Rust programming',
     'Online course',
     NULL, 4, 0, 3, 0, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '10 days', CURRENT_TIMESTAMP - INTERVAL '10 days'),
    ('550e8400-e29b-41d4-a716-446655440042',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Plan team offsite',
     'Annual retreat',
     NULL, 3, 0, 3, 1, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '8 days', CURRENT_TIMESTAMP - INTERVAL '8 days');

-- ============================================================================
-- Seed Archived Tasks for Alice
-- ============================================================================
INSERT INTO tasks (id, user_id, project_id, name, description, due_date, priority, status, system_list, sort_order, is_archived, completed_at, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440043',
     '550e8400-e29b-41d4-a716-446655440000',
     '550e8400-e29b-41d4-a716-446655440010',
     'Deploy v2.1 release',
     NULL,
     NULL, 1, 1, 0, 0, TRUE,
     CURRENT_TIMESTAMP - INTERVAL '5 days',
     CURRENT_TIMESTAMP - INTERVAL '12 days', CURRENT_TIMESTAMP - INTERVAL '5 days'),
    ('550e8400-e29b-41d4-a716-446655440044',
     '550e8400-e29b-41d4-a716-446655440000',
     NULL,
     'Document API endpoints',
     NULL,
     NULL, 2, 1, 1, 0, TRUE,
     CURRENT_TIMESTAMP - INTERVAL '2 days',
     CURRENT_TIMESTAMP - INTERVAL '10 days', CURRENT_TIMESTAMP - INTERVAL '2 days');

-- ============================================================================
-- Seed Task-Label Associations for Alice
-- ============================================================================
INSERT INTO task_labels (task_id, label_id)
VALUES
    ('550e8400-e29b-41d4-a716-446655440030', '550e8400-e29b-41d4-a716-446655440020'), -- Review Q1 budget / Work
    ('550e8400-e29b-41d4-a716-446655440030', '550e8400-e29b-41d4-a716-446655440022'), -- Review Q1 budget / Urgent
    ('550e8400-e29b-41d4-a716-446655440031', '550e8400-e29b-41d4-a716-446655440020'), -- Send client proposal / Work
    ('550e8400-e29b-41d4-a716-446655440033', '550e8400-e29b-41d4-a716-446655440020'), -- Complete project kickoff / Work
    ('550e8400-e29b-41d4-a716-446655440034', '550e8400-e29b-41d4-a716-446655440020'), -- Write technical spec / Work
    ('550e8400-e29b-41d4-a716-446655440035', '550e8400-e29b-41d4-a716-446655440020'), -- Design homepage / Work
    ('550e8400-e29b-41d4-a716-446655440038', '550e8400-e29b-41d4-a716-446655440020'), -- Q1 performance reviews / Work
    ('550e8400-e29b-41d4-a716-446655440038', '550e8400-e29b-41d4-a716-446655440022'), -- Q1 performance reviews / Urgent
    ('550e8400-e29b-41d4-a716-446655440039', '550e8400-e29b-41d4-a716-446655440021'), -- Team lunch planning / Personal
    ('550e8400-e29b-41d4-a716-446655440041', '550e8400-e29b-41d4-a716-446655440021'), -- Learn Rust / Personal
    ('550e8400-e29b-41d4-a716-446655440041', '550e8400-e29b-41d4-a716-446655440023'), -- Learn Rust / Reading
    ('550e8400-e29b-41d4-a716-446655440043', '550e8400-e29b-41d4-a716-446655440021'), -- Deploy v2.1 / Personal
    ('550e8400-e29b-41d4-a716-446655440036', '550e8400-e29b-41d4-a716-446655440020'), -- Fix login bug / Work
    ('550e8400-e29b-41d4-a716-446655440036', '550e8400-e29b-41d4-a716-446655440022'), -- Fix login bug / Urgent
    ('550e8400-e29b-41d4-a716-446655440032', '550e8400-e29b-41d4-a716-446655440020'), -- Update dependencies / Work
    ('550e8400-e29b-41d4-a716-446655440037', '550e8400-e29b-41d4-a716-446655440020'); -- Write unit tests / Work

-- ============================================================================
-- Seed Tasks for Bob (Minimal)
-- ============================================================================
INSERT INTO tasks (id, user_id, project_id, name, description, due_date, priority, status, system_list, sort_order, is_archived, completed_at, created_at, updated_at)
VALUES
    ('550e8400-e29b-41d4-a716-446655440050',
     '550e8400-e29b-41d4-a716-446655440001',
     NULL,
     'Buy groceries',
     'Milk, bread, eggs',
     NULL, 3, 0, 0, 0, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '1 day', CURRENT_TIMESTAMP - INTERVAL '1 day'),
    ('550e8400-e29b-41d4-a716-446655440051',
     '550e8400-e29b-41d4-a716-446655440001',
     NULL,
     'Call dentist',
     'Schedule checkup',
     CURRENT_TIMESTAMP + INTERVAL '7 days',
     3, 0, 0, 1, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '1 day', CURRENT_TIMESTAMP - INTERVAL '1 day'),
    ('550e8400-e29b-41d4-a716-446655440052',
     '550e8400-e29b-41d4-a716-446655440001',
     NULL,
     'Finish book',
     'Read Chapter 5',
     NULL, 4, 0, 3, 0, FALSE, NULL,
     CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP - INTERVAL '5 days');

-- ============================================================================
-- End of Seed Data Script
-- ============================================================================
