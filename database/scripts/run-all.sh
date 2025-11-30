#!/bin/bash
# =============================================
# Script: run-all.sh
# Description: Executes all SQL scripts in order
# Author: Solis Team
# Date: 2025-11-30
# =============================================

set -e

# Default parameters
DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-solis_pdv}
DB_USER=${DB_USER:-solis_user}
DB_PASSWORD=${DB_PASSWORD:-solis2024}
USE_DOCKER=${USE_DOCKER:-false}
SKIP_SEED=${SKIP_SEED:-false}

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

function print_success { echo -e "${GREEN}$1${NC}"; }
function print_error { echo -e "${RED}$1${NC}"; }
function print_info { echo -e "${CYAN}$1${NC}"; }
function print_warning { echo -e "${YELLOW}$1${NC}"; }

print_info "=========================================="
print_info "  Solis API - Database Setup Script"
print_info "=========================================="
echo ""

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Get SQL files (exclude 99-rollback)
SQL_FILES=()
for file in "$SCRIPT_DIR"/*.sql; do
    filename=$(basename "$file")
    if [[ ! $filename == 99-* ]]; then
        if [[ $SKIP_SEED == "true" && $filename == 04-seed-* ]]; then
            continue
        fi
        SQL_FILES+=("$file")
    fi
done

# Sort files
IFS=$'\n' SQL_FILES=($(sort <<<"${SQL_FILES[*]}"))
unset IFS

TOTAL_FILES=${#SQL_FILES[@]}

if [[ $SKIP_SEED == "true" ]]; then
    print_warning "Skipping seed scripts (04-*)"
fi

print_info "Found $TOTAL_FILES SQL scripts to execute"
echo ""

# Execute each script
EXECUTED_COUNT=0
FAILED_COUNT=0

for SQL_FILE in "${SQL_FILES[@]}"; do
    FILENAME=$(basename "$SQL_FILE")
    EXECUTED_COUNT=$((EXECUTED_COUNT + 1))
    
    print_info "[$EXECUTED_COUNT/$TOTAL_FILES] Executing: $FILENAME"
    
    if [[ $USE_DOCKER == "true" ]]; then
        # Execute via Docker
        if docker exec -i solis-postgres psql -U "$DB_USER" -d "$DB_NAME" < "$SQL_FILE" > /dev/null 2>&1; then
            print_success "  ✓ Success"
        else
            print_error "  ✗ Failed"
            FAILED_COUNT=$((FAILED_COUNT + 1))
        fi
    else
        # Execute via psql (requires psql in PATH)
        export PGPASSWORD="$DB_PASSWORD"
        if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -q < "$SQL_FILE" > /dev/null 2>&1; then
            print_success "  ✓ Success"
        else
            print_error "  ✗ Failed"
            FAILED_COUNT=$((FAILED_COUNT + 1))
        fi
        unset PGPASSWORD
    fi
    
    echo ""
done

# Summary
print_info "=========================================="
print_info "  Execution Summary"
print_info "=========================================="
print_success "Executed: $EXECUTED_COUNT scripts"

if [[ $FAILED_COUNT -gt 0 ]]; then
    print_error "Failed: $FAILED_COUNT scripts"
    exit 1
else
    print_success "All scripts executed successfully!"
    echo ""
    print_info "Demo tenant credentials:"
    print_info "  Admin:    admin@demo.com / Admin@123"
    print_info "  Manager:  manager@demo.com / Manager@123"
    print_info "  Operator: operator@demo.com / Operator@123"
    exit 0
fi
