# Database Initialization Scripts

This directory contains PostgreSQL scripts to initialize the GTD Todo application database.

## Overview

The database initialization process consists of three SQL scripts executed in order:

1. **01-create-database.sql** - Creates the PostgreSQL database
2. **02-schema.sql** - Creates tables, indexes, constraints, and triggers
3. **03-seed-data.sql** - Populates the database with development test data

A parent shell script **init-database.sh** orchestrates the execution of all three scripts.

## Prerequisites

- PostgreSQL 12+ installed and running
- `psql` command-line client available in PATH
- Superuser access to PostgreSQL (typically `postgres` user)
- For Linux/macOS: Bash shell
- For Windows: Git Bash, WSL, or Windows Subsystem for PostgreSQL

## Quick Start

### Option 1: Using the initialization script (Recommended)

```bash
cd src/database
./init-database.sh
```

This will:
- Create the `todo_app` database
- Create all tables, indexes, and triggers
- Load development seed data

### Option 2: Manual execution with psql

```bash
# Create database
psql -h localhost -U postgres -p 5432 -f 01-create-database.sql

# Create schema
psql -h localhost -U postgres -p 5432 -d todo_app -f 02-schema.sql

# Load seed data
psql -h localhost -U postgres -p 5432 -d todo_app -f 03-seed-data.sql
```

### Option 3: Using docker-compose

If PostgreSQL is running in a Docker container:

```bash
# Copy scripts into container and execute
docker-compose exec postgres psql -U postgres -f /scripts/01-create-database.sql
docker-compose exec postgres psql -U postgres -d todo_app -f /scripts/02-schema.sql
docker-compose exec postgres psql -U postgres -d todo_app -f /scripts/03-seed-data.sql
```

## Script Details

### 01-create-database.sql

Creates the PostgreSQL database with UTF-8 encoding and enables the UUID extension.

**Actions:**
- Drops existing `todo_app` database (if present)
- Creates new `todo_app` database
- Enables `uuid-ossp` extension for UUID support
- Sets timezone to UTC

**Execution time:** ~1-2 seconds

### 02-schema.sql

Creates the complete database schema including:
- 5 main tables: `users`, `projects`, `labels`, `tasks`, `task_labels`
- Indexes for query performance
- Foreign key constraints
- Check constraints for data validation
- Triggers for automatic `updated_at` timestamp management

**Tables created:**
- **users** - Application users (authentication & ownership)
- **projects** - User-created projects grouping tasks
- **labels** - User-created labels for task categorization
- **tasks** - Core task entities with GTD system lists
- **task_labels** - Many-to-many relationship between tasks and labels

**Features:**
- UUID primary keys (compatible with EF Core)
- TIMESTAMP WITH TIME ZONE for UTC support
- Automatic timestamp updates via triggers
- Comprehensive data validation via constraints
- Detailed table and column comments for documentation

**Execution time:** ~2-3 seconds

### 03-seed-data.sql

Populates the database with realistic development test data:
- 2 test users (Alice and Bob)
- 2 projects for Alice
- 4 labels for Alice
- 15+ tasks across all system lists (Inbox, Next, Upcoming, Someday)
- 2 archived/completed tasks for Alice
- Task-label associations
- 3 minimal tasks for Bob

**Test Users:**
- **Alice Johnson** (alice@example.com) - Full feature test data
- **Bob Smith** (bob@example.com) - Minimal test data

**Note:** Password hashes in seed data are placeholders. See "Authentication" section below.

**Execution time:** ~2-3 seconds

## Usage Examples

### Basic initialization with default settings

```bash
./init-database.sh
```

### Custom PostgreSQL host/port

```bash
./init-database.sh -h prod-db.example.com -u postgres -p 5432
```

### With password (non-interactive)

```bash
export PGPASSWORD=your_password
./init-database.sh -h myhost -u postgres
```

### Verbose output for debugging

```bash
./init-database.sh --verbose
```

### Quiet mode (minimal output)

```bash
./init-database.sh --quiet
```

## Database Schema

### Users Table
```
- id (UUID, PK)
- email (unique)
- password_hash
- password_salt
- display_name
- created_at, updated_at
```

### Projects Table
```
- id (UUID, PK)
- user_id (FK → users)
- name, description
- due_date, status, sort_order
- created_at, updated_at
```

### Labels Table
```
- id (UUID, PK)
- user_id (FK → users)
- name (unique per user)
- color (hex)
- created_at
```

### Tasks Table
```
- id (UUID, PK)
- user_id (FK → users)
- project_id (FK → projects, nullable)
- name, description
- due_date, priority (1-4), status (0-1), system_list (0-3)
- sort_order, is_archived, completed_at
- created_at, updated_at
```

### Task-Labels Join Table
```
- task_id (UUID, FK → tasks)
- label_id (UUID, FK → labels)
- Composite PK (task_id, label_id)
```

## Authentication

**IMPORTANT:** The seed data uses placeholder password hashes for development only.

### Generating Real Password Hashes

To use real credentials, hash passwords with PBKDF2-HMAC-SHA256 (100,000+ iterations):

#### Using .NET Backend
```csharp
// In your C# application
var passwordHashingService = new PasswordHashingService();
var (hash, salt) = passwordHashingService.HashPassword("MySecurePassword123");

// Then update the database:
// UPDATE users SET password_hash = hash, password_salt = salt WHERE email = 'user@example.com'
```

#### Using OpenSSL (Linux/macOS)
```bash
# Generate a salt
salt=$(openssl rand -hex 16)

# Hash password with PBKDF2 (100,000 iterations)
hash=$(echo -n "YourPassword123" | openssl pkeyutl -sign -inkey <(openssl pkey -generate) | base64)

# Then insert into database
```

#### Using Python
```python
import hashlib
import secrets

password = "YourPassword123"
salt = secrets.token_hex(16)
hash_obj = hashlib.pbkdf2_hmac('sha256', password.encode(), salt.encode(), 100000)
password_hash = hash_obj.hex()

print(f"Hash: {password_hash}")
print(f"Salt: {salt}")
```

## Resetting the Database

To start fresh and reinitialize:

### Drop and recreate
```bash
# Drop database
psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS todo_app;"

# Reinitialize
./init-database.sh
```

### Or manually
```sql
-- Connect as superuser
DROP DATABASE IF EXISTS todo_app;
-- Then run initialization script
```

## Troubleshooting

### Connection refused
```
Error: could not connect to server: Connection refused
```
**Solution:** Ensure PostgreSQL is running
```bash
# Check if service is running
psql -h localhost -U postgres -c "SELECT 1"

# If using Docker
docker-compose up -d postgres
```

### Permission denied
```
Error: FATAL:  authentication failed for user "postgres"
```
**Solution:** Verify PostgreSQL credentials or use password file
```bash
# Set password in environment
export PGPASSWORD=your_password

# Or create .pgpass file (~/.pgpass)
# localhost:5432:*:postgres:your_password
chmod 600 ~/.pgpass
```

### Database already exists
```
Error: database "todo_app" already exists
```
**Solution:** Drop the existing database first (or modify 01-create-database.sql)
```bash
psql -U postgres -c "DROP DATABASE IF EXISTS todo_app;"
./init-database.sh
```

### psql command not found
```
Error: command not found: psql
```
**Solution:** Install PostgreSQL client tools

- **Ubuntu/Debian:** `sudo apt-get install postgresql-client`
- **macOS:** `brew install postgresql`
- **Windows:** Install PostgreSQL or use Windows Subsystem for Linux

## Integration with Entity Framework Core

After running these scripts, you can integrate with EF Core migrations:

```bash
# Apply any pending migrations
cd src/backend
dotnet ef database update

# The migration tracker table (__EFMigrationsHistory) is automatically created
```

## Development Workflow

1. **Initial setup:**
   ```bash
   ./init-database.sh
   ```

2. **Make schema changes in EF Core:**
   ```bash
   cd src/backend
   dotnet ef migrations add YourMigration
   dotnet ef database update
   ```

3. **Reset database during development:**
   ```bash
   # Drop and reinitialize
   psql -U postgres -c "DROP DATABASE IF EXISTS todo_app;"
   cd src/database
   ./init-database.sh
   ```

4. **Refresh seed data:**
   ```bash
   # Run only the seed script (after database exists)
   psql -U postgres -d todo_app -f src/database/03-seed-data.sql
   ```

## Production Considerations

⚠️ **Do NOT use these scripts directly in production. Instead:**

1. **Use EF Core migrations** for schema management
2. **Never include seed data** in production initialization
3. **Backup the database** before any schema changes
4. **Use strong passwords** and secure credential management
5. **Encrypt sensitive data** at rest
6. **Enable SSL/TLS** for database connections
7. **Implement connection pooling** (PgBouncer, pgpool)
8. **Monitor database performance** and logs
9. **Regular backup and recovery** testing

## Additional Resources

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [UUID in PostgreSQL](https://www.postgresql.org/docs/current/uuid.html)
- [PBKDF2 Password Hashing](https://en.wikipedia.org/wiki/PBKDF2)

## Support

For issues or questions about the database scripts:

1. Check the troubleshooting section above
2. Review PostgreSQL logs: `docker logs postgres` (if using Docker)
3. Run with verbose flag: `./init-database.sh --verbose`
4. Consult PostgreSQL documentation for specific errors
