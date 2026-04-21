#!/usr/bin/env bash
set -euo pipefail

# ── Colours ──────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
ok()   { echo -e "${GREEN}[ok]${NC}    $*"; }
warn() { echo -e "${YELLOW}[warn]${NC}  $*"; }
fail() { echo -e "${RED}[fail]${NC}  $*"; exit 1; }

echo ""
echo "  TheCookBook — local setup"
echo "  ─────────────────────────"
echo ""

# ── Prerequisites ─────────────────────────────────────────────────────────────
echo "Checking prerequisites..."

if command -v docker &>/dev/null && docker info &>/dev/null; then
    ok "Docker $(docker --version | awk '{print $3}' | tr -d ',')"
else
    fail "Docker is not running. Install Docker Desktop and start it, then re-run this script."
fi

if docker compose version &>/dev/null; then
    ok "Docker Compose $(docker compose version --short)"
else
    fail "Docker Compose plugin not found. Update Docker Desktop or install the compose plugin."
fi

if command -v dotnet &>/dev/null; then
    DOTNET_VER=$(dotnet --version)
    DOTNET_MAJOR=$(echo "$DOTNET_VER" | cut -d. -f1)
    if [ "$DOTNET_MAJOR" -ge 10 ]; then
        ok ".NET SDK $DOTNET_VER"
    else
        warn ".NET SDK $DOTNET_VER found, but .NET 10+ is required. Some commands may fail."
    fi
else
    warn ".NET SDK not found — needed to build/test outside Docker. Install from https://dot.net"
fi

echo ""

# ── .env file ─────────────────────────────────────────────────────────────────
echo "Checking environment file..."

if [ -f .env ]; then
    ok ".env already exists — skipping copy"
else
    cp .env.example .env
    ok "Copied .env.example → .env"
    echo ""
    warn "Open .env and set strong passwords for DB_PASSWORD and PG_PASSWORD before continuing."
    echo "      SQL Server requires: min 8 chars, uppercase, lowercase, digit, and symbol."
    echo ""
    read -rp "      Press Enter once you've updated .env, or Ctrl+C to exit and do it now... "
fi

echo ""

# ── Validate .env has been changed from defaults ──────────────────────────────
if grep -q 'ChangeThisPassword_123!' .env; then
    warn "Default passwords detected in .env. The stack will still start, but change them for any shared environment."
fi

# ── Start the stack ───────────────────────────────────────────────────────────
echo "Starting the stack (dev mode with hot reload)..."
echo ""

docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d

echo ""
ok "Stack is starting."
echo ""
echo "  Services:"
echo "    App  →  http://localhost:8081"
echo "    API  →  http://localhost:8080"
echo "    Grafana  →  http://localhost:3000"
echo ""
echo "  Useful commands:"
echo "    Logs:   docker compose -f docker-compose.yml -f docker-compose.dev.yml logs -f"
echo "    Stop:   docker compose -f docker-compose.yml -f docker-compose.dev.yml down"
echo "    Tests:  dotnet test"
echo ""
