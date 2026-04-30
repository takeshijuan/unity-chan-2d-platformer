#!/usr/bin/env bash
# Unity 6.3 LTS local compile check (Personal license — runs locally only)
# Usage: bash tools/ci/unity-compile-check.sh [--verbose]
# Exit: 0=success, 1=compile error, 2=editor not found, 3=project version not found,
#       4=editor lock detected, 5=invalid version format
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
VERSION_FILE="$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt"
VERBOSE="${1:-}"

if [ ! -f "$VERSION_FILE" ]; then
    echo "ERROR: $VERSION_FILE not found. Is this a Unity project?" >&2
    exit 3
fi

# Strip BOM + CRLF before parsing (Windows checkout / cross-platform safety)
UNITY_VERSION="$(sed '1s/^\xEF\xBB\xBF//' "$VERSION_FILE" | awk '/^m_EditorVersion:/ {print $2}' | tr -d '\r')"

# Validate version format to prevent path traversal via malicious ProjectVersion.txt
if [[ ! $UNITY_VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+[a-z][0-9]+$ ]]; then
    echo "ERROR: Invalid Unity version format: '$UNITY_VERSION'" >&2
    echo "Expected pattern: <major>.<minor>.<patch><release-stage><build> (e.g., 6000.3.13f1)" >&2
    exit 5
fi

UNITY_EDITOR="${UNITY_EDITOR_PATH:-/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity}"

if [ ! -x "$UNITY_EDITOR" ]; then
    echo "ERROR: Unity $UNITY_VERSION Editor not executable at $UNITY_EDITOR" >&2
    if [ -d "/Applications/Unity/Hub/Editor" ]; then
        INSTALLED="$(ls /Applications/Unity/Hub/Editor/ 2>/dev/null | tr '\n' ' ')"
        echo "Installed versions: ${INSTALLED:-(none)}" >&2
    else
        echo "Unity Hub not found at /Applications/Unity/Hub/Editor/" >&2
    fi
    echo "" >&2
    echo "Install Unity $UNITY_VERSION:" >&2
    echo "  1. Download Unity Hub: https://unity.com/download" >&2
    echo "  2. Hub -> Installs -> Add -> select $UNITY_VERSION" >&2
    echo "Or set UNITY_EDITOR_PATH=/path/to/Unity.app/Contents/MacOS/Unity" >&2
    exit 2
fi

# Detect concurrent Editor instance (Library/ lock)
if [ -f "$PROJECT_ROOT/Library/Locks/Packages.lock" ] || \
   [ -f "$PROJECT_ROOT/Temp/UnityLockfile" ]; then
    echo "ERROR: Project is locked (concurrent Editor instance detected)." >&2
    if [ -n "${CI:-}" ]; then
        echo "On CI: clean checkout, or delete Library/ and Temp/ before retry." >&2
    else
        echo "Locally: close Unity Editor (Cmd+Q on macOS) and retry." >&2
    fi
    exit 4
fi

# Cross-platform mktemp (BSD vs GNU compatibility)
# Note: macOS BSD mktemp requires X's at end of basename — no suffix after XXXXXX
COMPILE_LOG="$(mktemp "${TMPDIR:-/tmp}/unity-compile-check.XXXXXX")"
trap "rm -f '$COMPILE_LOG'" EXIT  # Always cleanup, even on failure

echo "Running Unity compile check (version: $UNITY_VERSION)..."
echo "Project: $PROJECT_ROOT"
[ "$VERBOSE" = "--verbose" ] && echo "Log: $COMPILE_LOG"

# Unity 6.3 LTS requires the full -batchmode -nographics -quit combo
# Using -quit alone may not exit cleanly in some patch versions
set +e
"$UNITY_EDITOR" -batchmode -nographics \
  -projectPath "$PROJECT_ROOT" \
  -logFile "$COMPILE_LOG" \
  -quit
EXIT_CODE=$?
set -e

# Detect compile errors via exit code OR log pattern (false-negative mitigation)
# Note: empty project (0 .cs files) exits 0 -- intentional, "no code = no errors"
# Note: Warnings (CS####) are ignored by design (Errors only block)
if [ $EXIT_CODE -ne 0 ] || grep -qE 'error CS[0-9]+' "$COMPILE_LOG"; then
    # Preserve log on failure (override trap)
    PRESERVED_LOG="$COMPILE_LOG"
    trap - EXIT
    echo "FAIL: Compile errors detected (Unity exit=$EXIT_CODE)" >&2
    echo "" >&2
    grep -E 'error CS[0-9]+' "$PRESERVED_LOG" | head -20 >&2 || true
    echo "" >&2
    echo "Full log: $PRESERVED_LOG" >&2
    exit 1
fi

echo "PASS: Compile check successful."
[ "$VERBOSE" = "--verbose" ] && echo "Log: $COMPILE_LOG"
exit 0
