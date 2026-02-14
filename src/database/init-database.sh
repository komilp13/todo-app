#!/bin/bash

# ============================================================================
# GTD Todo Application - Database Initialization Script
# ============================================================================
# This script initializes the PostgreSQL database for the GTD Todo application
# It executes all SQL scripts in the correct order:
#   1. 01-create-database.sql - Creates the database
#   2. 02-schema.sql - Creates all tables, indexes, and triggers
#   3. 03-seed-data.sql - Populates with development test data
#
# Requirements:
#   - PostgreSQL server running and accessible
#   - psql command-line tool available in PATH
#   - Superuser access to PostgreSQL (typically 'postgres' user)
#
# Usage:
#   ./init-database.sh                           # Use default settings
#   ./init-database.sh -h localhost -u postgres -p 5432

set -e  # Exit on error

# ============================================================================
# Configuration
# ============================================================================

# Default values
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_SUPERUSER="${DB_SUPERUSER:-postgres}"
DB_NAME="todo_app"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERBOSITY=1  # 0: quiet, 1: normal, 2: verbose

# ANSI color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ============================================================================
# Functions
# ============================================================================

print_header() {
    echo -e "${BLUE}===========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}===========================================${NC}"
}

print_step() {
    echo -e "${BLUE}→ $1${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ ERROR: $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ WARNING: $1${NC}"
}

print_info() {
    if [ $VERBOSITY -ge 1 ]; then
        echo -e "$1"
    fi
}

# Parse command-line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--host)
                DB_HOST="$2"
                shift 2
                ;;
            -u|--user)
                DB_SUPERUSER="$2"
                shift 2
                ;;
            -p|--port)
                DB_PORT="$2"
                shift 2
                ;;
            -v|--verbose)
                VERBOSITY=2
                shift
                ;;
            -q|--quiet)
                VERBOSITY=0
                shift
                ;;
            *)
                print_error "Unknown argument: $1"
                show_usage
                exit 1
                ;;
        esac
    done
}

show_usage() {
    cat << EOF
Usage: init-database.sh [OPTIONS]

Initialize the GTD Todo application database with schema and seed data.

OPTIONS:
    -h, --host HOST              PostgreSQL host (default: localhost)
    -u, --user USER              PostgreSQL superuser (default: postgres)
    -p, --port PORT              PostgreSQL port (default: 5432)
    -v, --verbose                Verbose output
    -q, --quiet                  Quiet output
    --help                       Show this help message

ENVIRONMENT VARIABLES:
    DB_HOST                      PostgreSQL host
    DB_PORT                      PostgreSQL port
    DB_SUPERUSER                 PostgreSQL superuser
    PGPASSWORD                   PostgreSQL password (for non-interactive auth)

EXAMPLE:
    ./init-database.sh -h localhost -u postgres -p 5432
    PGPASSWORD=mypassword ./init-database.sh -h prod-db.example.com -u postgres

EOF
}

check_postgres() {
    print_step "Checking PostgreSQL connectivity..."

    if ! command -v psql &> /dev/null; then
        print_error "psql command not found. Please install PostgreSQL client tools."
        exit 1
    fi

    if ! psql -h "$DB_HOST" -U "$DB_SUPERUSER" -p "$DB_PORT" -c "SELECT 1" &> /dev/null; then
        print_error "Cannot connect to PostgreSQL at $DB_HOST:$DB_PORT"
        print_info "Make sure PostgreSQL is running and accessible."
        print_info "You may need to set PGPASSWORD environment variable or use .pgpass file."
        exit 1
    fi

    print_success "PostgreSQL is accessible"
}

check_scripts() {
    print_step "Checking SQL script files..."

    local scripts=("01-create-database.sql" "02-schema.sql" "03-seed-data.sql")

    for script in "${scripts[@]}"; do
        if [ ! -f "$SCRIPT_DIR/$script" ]; then
            print_error "Script not found: $SCRIPT_DIR/$script"
            exit 1
        fi
        print_info "  ✓ Found $script"
    done

    print_success "All SQL scripts found"
}

execute_script() {
    local script_name="$1"
    local script_path="$SCRIPT_DIR/$script_name"

    print_step "Executing $script_name..."

    if [ $VERBOSITY -ge 2 ]; then
        psql -h "$DB_HOST" -U "$DB_SUPERUSER" -p "$DB_PORT" \
            -f "$script_path" \
            2>&1 | tee /tmp/db_init_$$.log
    else
        psql -h "$DB_HOST" -U "$DB_SUPERUSER" -p "$DB_PORT" \
            -f "$script_path" \
            > /tmp/db_init_$$.log 2>&1 || {
            print_error "Failed to execute $script_name"
            echo "Log output:"
            cat /tmp/db_init_$$.log
            exit 1
        }
    fi

    print_success "$script_name completed successfully"
    rm -f /tmp/db_init_$$.log
}

verify_database() {
    print_step "Verifying database initialization..."

    # Check if database exists
    local db_exists=$(psql -h "$DB_HOST" -U "$DB_SUPERUSER" -p "$DB_PORT" \
        -tc "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME'")

    if [ -z "$db_exists" ]; then
        print_error "Database '$DB_NAME' was not created"
        exit 1
    fi

    # Check if tables exist
    local table_count=$(psql -h "$DB_HOST" -U "$DB_SUPERUSER" -p "$DB_PORT" \
        -d "$DB_NAME" \
        -tc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'")

    if [ "$table_count" -lt 5 ]; then
        print_error "Not all tables were created (found $table_count tables, expected at least 5)"
        exit 1
    fi

    print_info "  ✓ Database '$DB_NAME' exists"
    print_info "  ✓ Tables created ($table_count tables found)"

    # Optional: Check if seed data exists
    local user_count=$(psql -h "$DB_HOST" -U "$DB_SUPERUSER" -p "$DB_PORT" \
        -d "$DB_NAME" \
        -tc "SELECT COUNT(*) FROM users")

    if [ "$user_count" -gt 0 ]; then
        print_info "  ✓ Seed data loaded ($user_count users found)"
    fi

    print_success "Database verification passed"
}

# ============================================================================
# Main Script
# ============================================================================

main() {
    parse_arguments "$@"

    print_header "GTD Todo Application - Database Initialization"
    print_info "Host: $DB_HOST"
    print_info "Port: $DB_PORT"
    print_info "User: $DB_SUPERUSER"
    print_info "Database: $DB_NAME"
    print_info ""

    # Execute initialization steps
    check_postgres
    check_scripts

    print_step "Starting database initialization..."
    execute_script "01-create-database.sql"
    execute_script "02-schema.sql"
    execute_script "03-seed-data.sql"

    # Verify everything worked
    verify_database

    print_header "✓ Database initialization completed successfully!"
    print_info ""
    print_info "Next steps:"
    print_info "  1. Run backend migrations (if using EF Core):"
    print_info "     cd src/backend && dotnet ef database update"
    print_info ""
    print_info "  2. Start the application:"
    print_info "     docker-compose up -d"
    print_info ""
    print_info "Test credentials (see seed data for details):"
    print_info "  Email: alice@example.com"
    print_info "  Email: bob@example.com"
    print_info ""
}

# Show help if requested
if [[ "$1" == "--help" ]] || [[ "$1" == "-h" ]] && [[ $# -eq 1 ]]; then
    show_usage
    exit 0
fi

# Run main function
main "$@"
