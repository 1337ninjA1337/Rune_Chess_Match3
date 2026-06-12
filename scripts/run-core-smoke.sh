#!/usr/bin/env bash
# Build and run the pure C# core verification suite (tools/CoreSmoke).
#
# Single source of truth used by both local development and CI so the core
# package always has an executable verification path, regardless of whether the
# Unity Editor is available. Requires the .NET 8 SDK on PATH (see
# .github/workflows/core-smoke.yml for the pinned CI version).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="${repo_root}/tools/CoreSmoke/CoreSmoke.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "error: dotnet SDK not found on PATH. Install the .NET 8 SDK to run CoreSmoke." >&2
  exit 127
fi

echo "Running CoreSmoke (${project})..."
# --nologo keeps output focused on the smoke result. CoreSmoke throws (non-zero
# exit) on any failed assertion, so a clean exit means every check passed.
dotnet run --project "${project}" --configuration Release --nologo
