#!/bin/bash
###############################################################################
# Solis API - Test Suite (Bash)
# Testa todos os endpoints principais: Auth, Dynamic CRUD
###############################################################################

set -e

# Configuration
API_URL="${API_URL:-http://localhost:5287}"
TENANT="${TENANT:-demo}"
TOKEN=""
TESTS_PASSED=0
TESTS_FAILED=0

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

###############################################################################
# Helper Functions
###############################################################################

print_header() {
    echo -e "\n${CYAN}========================================${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}========================================${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
    ((TESTS_PASSED++))
}

print_failure() {
    echo -e "${RED}✗ $1${NC}"
    if [ -n "$2" ]; then
        echo -e "${YELLOW}  Details: $2${NC}"
    fi
    ((TESTS_FAILED++))
}

api_test() {
    local method=$1
    local endpoint=$2
    local body=$3
    local expected_status=${4:-200}
    local test_name=$5
    
    local headers=(-H "X-Tenant-Subdomain: $TENANT" -H "Content-Type: application/json")
    
    if [ -n "$TOKEN" ]; then
        headers+=(-H "Authorization: Bearer $TOKEN")
    fi
    
    local curl_cmd=(curl -s -w "\n%{http_code}" -X "$method" "$API_URL$endpoint" "${headers[@]}")
    
    if [ -n "$body" ]; then
        curl_cmd+=(-d "$body")
    fi
    
    local response=$(${curl_cmd[@]})
    local http_code=$(echo "$response" | tail -n1)
    local body_response=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq "$expected_status" ]; then
        print_success "$test_name"
        echo "$body_response"
    else
        print_failure "$test_name" "Status: $http_code, Expected: $expected_status. Response: $body_response"
        echo ""
    fi
}

###############################################################################
# Main Test Suite
###############################################################################

echo -e "${MAGENTA}"
cat << "EOF"

╔═══════════════════════════════════════╗
║      SOLIS API - TEST SUITE          ║
╚═══════════════════════════════════════╝

EOF
echo -e "${NC}"
echo "API: $API_URL"
echo "Tenant: $TENANT"

# ====================
# 1. HEALTH CHECK
# ====================

print_header "1. Health Check"
api_test "GET" "/api/health" "" 200 "Health endpoint responds" > /dev/null

# ====================
# 2. AUTHENTICATION
# ====================

print_header "2. Authentication Tests"

login_response=$(api_test "POST" "/api/auth/login" '{"email":"admin@demo.com","password":"Admin@123"}' 200 "Login with admin credentials")

TOKEN=$(echo "$login_response" | jq -r '.token // empty')
if [ -n "$TOKEN" ]; then
    echo -e "${GRAY}  Token: ${TOKEN:0:50}...${NC}"
fi

api_test "POST" "/api/auth/login" '{"email":"admin@demo.com","password":"WrongPassword"}' 401 "Login with invalid password fails" > /dev/null

# ====================
# 3. DYNAMIC CRUD - USER
# ====================

print_header "3. Dynamic CRUD - User Entity"

api_test "GET" "/api/dynamic/user/_metadata" "" 200 "Get user metadata" > /dev/null

users_response=$(api_test "GET" "/api/dynamic/user?page=1&pageSize=10" "" 200 "List users with pagination")
user_count=$(echo "$users_response" | jq -r '.data | length')
total_count=$(echo "$users_response" | jq -r '.pagination.totalCount')
echo -e "${GRAY}  Found $user_count users, Total: $total_count${NC}"

# Get first user
if [ "$user_count" -gt 0 ]; then
    user_id=$(echo "$users_response" | jq -r '.data[0].id')
    api_test "GET" "/api/dynamic/user/$user_id" "" 200 "Get user by ID" > /dev/null
fi

# Create new user
random_num=$RANDOM
new_user_response=$(api_test "POST" "/api/dynamic/user" "{\"name\":\"Test User $random_num\",\"email\":\"testuser$random_num@demo.com\",\"password\":\"Test@123\",\"role\":\"operator\"}" 201 "Create new user")

created_user_id=$(echo "$new_user_response" | jq -r '.id // empty')
if [ -n "$created_user_id" ]; then
    echo -e "${GRAY}  Created user ID: $created_user_id${NC}"
    
    # Update user
    api_test "PUT" "/api/dynamic/user/$created_user_id" '{"name":"Updated Test User"}' 204 "Update user" > /dev/null
    
    # Delete user
    api_test "DELETE" "/api/dynamic/user/$created_user_id" "" 204 "Delete user (soft delete)" > /dev/null
fi

# Search users
api_test "GET" "/api/dynamic/user?search=admin" "" 200 "Search users by name/email" > /dev/null

# ====================
# 4. TAX REGIME
# ====================

print_header "4. Dynamic CRUD - Tax Regime Entity"

api_test "GET" "/api/dynamic/tax_regime/_metadata" "" 200 "Get tax_regime metadata" > /dev/null

tax_regimes_response=$(api_test "GET" "/api/dynamic/tax_regime" "" 200 "List tax regimes")
tax_regime_count=$(echo "$tax_regimes_response" | jq -r '.data | length')
echo -e "${GRAY}  Found $tax_regime_count tax regimes${NC}"

# ====================
# 5. SPECIAL TAX REGIME
# ====================

print_header "5. Dynamic CRUD - Special Tax Regime Entity"

api_test "GET" "/api/dynamic/special_tax_regime/_metadata" "" 200 "Get special_tax_regime metadata" > /dev/null

special_tax_regimes_response=$(api_test "GET" "/api/dynamic/special_tax_regime" "" 200 "List special tax regimes")
special_count=$(echo "$special_tax_regimes_response" | jq -r '.data | length')
echo -e "${GRAY}  Found $special_count special tax regimes${NC}"

# ====================
# 6. COMPANY
# ====================

print_header "6. Dynamic CRUD - Company Entity"

api_test "GET" "/api/dynamic/company/_metadata" "" 200 "Get company metadata" > /dev/null

companies_response=$(api_test "GET" "/api/dynamic/company" "" 200 "List companies")
company_count=$(echo "$companies_response" | jq -r '.data | length')
echo -e "${GRAY}  Found $company_count companies${NC}"

if [ "$company_count" -gt 0 ]; then
    company_id=$(echo "$companies_response" | jq -r '.data[0].id')
    api_test "GET" "/api/dynamic/company/$company_id" "" 200 "Get company by ID" > /dev/null
    api_test "GET" "/api/dynamic/company/$company_id/options/tax_regime_id" "" 200 "Get tax regime options" > /dev/null
fi

# ====================
# 7. FIELD OPTIONS
# ====================

print_header "7. Field Options Tests"

if [ "$user_count" -gt 0 ]; then
    user_id=$(echo "$users_response" | jq -r '.data[0].id')
    role_options=$(api_test "GET" "/api/dynamic/user/$user_id/options/role" "" 200 "Get role options (static)")
    role_count=$(echo "$role_options" | jq -r '. | length')
    echo -e "${GRAY}  Available roles: $role_count options${NC}"
fi

# ====================
# 8. ERROR HANDLING
# ====================

print_header "8. Error Handling Tests"

api_test "GET" "/api/dynamic/nonexistent" "" 404 "Non-existent entity returns 404" > /dev/null
api_test "GET" "/api/dynamic/user/00000000-0000-0000-0000-000000000000" "" 404 "Non-existent user ID returns 404" > /dev/null
api_test "POST" "/api/dynamic/user" '{"name":"Incomplete User"}' 400 "Create user without required fields fails" > /dev/null

# Request without token
TOKEN=""
api_test "GET" "/api/dynamic/user" "" 401 "Request without token fails" > /dev/null

# ====================
# SUMMARY
# ====================

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}           TEST SUMMARY${NC}"
echo -e "${CYAN}========================================${NC}"
echo -e "Total Tests: $((TESTS_PASSED + TESTS_FAILED))"
echo -e "${GREEN}Passed: $TESTS_PASSED${NC}"
echo -e "${RED}Failed: $TESTS_FAILED${NC}"

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "\n${GREEN}✓ ALL TESTS PASSED!${NC}\n"
    exit 0
else
    echo -e "\n${RED}✗ SOME TESTS FAILED${NC}\n"
    exit 1
fi
