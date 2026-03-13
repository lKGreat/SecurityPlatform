#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FRONTEND_DIR="$ROOT_DIR/src/frontend/Atlas.WebApp"

# ── .NET 10 SDK ────────────────────────────────────────────────
REQUIRED_DOTNET_MAJOR="10"

if ! command -v dotnet >/dev/null 2>&1 \
   || [[ "$(dotnet --version 2>/dev/null | cut -d. -f1)" != "$REQUIRED_DOTNET_MAJOR" ]]; then
  echo "Installing dotnet-sdk-10.0 …"
  sudo apt-get update -qq
  sudo apt-get install -y -qq dotnet-sdk-10.0
fi

DOTNET_VER="$(dotnet --version)"
DOTNET_MAJOR="${DOTNET_VER%%.*}"
if [[ "$DOTNET_MAJOR" != "$REQUIRED_DOTNET_MAJOR" ]]; then
  echo "dotnet-sdk-10.0 installation failed. Got version: $DOTNET_VER" >&2
  exit 1
fi
echo "dotnet OK: $DOTNET_VER"

# ── Node.js 22 ────────────────────────────────────────────────
if ! command -v node >/dev/null 2>&1; then
  echo "Node.js is required but not found." >&2
  exit 1
fi

NODE_MAJOR="$(node -p "process.versions.node.split('.')[0]")"
if [[ "$NODE_MAJOR" != "22" ]]; then
  echo "Node.js 22 is required. Current version: $(node -v)" >&2
  exit 1
fi
echo "node OK: $(node -v)"

# ── Dependency restore ─────────────────────────────────────────
dotnet restore "$ROOT_DIR/Atlas.SecurityPlatform.slnx"
npm install --prefix "$FRONTEND_DIR"
