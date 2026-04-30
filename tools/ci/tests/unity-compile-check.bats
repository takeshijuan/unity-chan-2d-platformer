#!/usr/bin/env bats
# Tests for tools/ci/unity-compile-check.sh
# Run: bats tools/ci/tests/unity-compile-check.bats
#
# Coverage: 4 error paths (exit codes 2, 3, 4, 5)
# The compile-error path (exit 1) and PASS path (exit 0) require real Unity
# invocation and are verified manually during ship cycles. See README.md.

setup() {
    REPO_ROOT="$(cd "$BATS_TEST_DIRNAME/../../.." && pwd)"
    SCRIPT="$REPO_ROOT/tools/ci/unity-compile-check.sh"
    # Each test runs in an isolated temp project root
    TEST_PROJ="$(mktemp -d "${TMPDIR:-/tmp}/unity-compile-check-test.XXXXXX")"
    mkdir -p "$TEST_PROJ/ProjectSettings" "$TEST_PROJ/tools/ci"
    # Copy the script under test into the temp project so its $PROJECT_ROOT
    # auto-resolves to the test directory (script uses $SCRIPT_DIR/../..)
    cp "$SCRIPT" "$TEST_PROJ/tools/ci/unity-compile-check.sh"
}

teardown() {
    rm -rf "$TEST_PROJ"
}

@test "exit 3 when ProjectVersion.txt is missing" {
    # Do not create ProjectVersion.txt
    run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 3 ]
    [[ "$output" == *"ProjectVersion.txt not found"* ]]
}

@test "exit 5 when ProjectVersion.txt has invalid version format" {
    echo "m_EditorVersion: not-a-version" > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 5 ]
    [[ "$output" == *"Invalid Unity version format"* ]]
}

@test "exit 5 rejects path traversal in version field" {
    # Security: malicious ProjectVersion.txt should not allow ../../bin/sh
    echo "m_EditorVersion: ../../bin/sh" > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 5 ]
    [[ "$output" == *"Invalid Unity version format"* ]]
}

@test "exit 2 when Unity Editor binary not found" {
    echo "m_EditorVersion: 6000.3.13f1" > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    UNITY_EDITOR_PATH="/nonexistent/Unity.app/Contents/MacOS/Unity" \
        run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 2 ]
    [[ "$output" == *"Editor not executable"* ]]
    # DX requirement: error message includes Unity Hub install URL
    [[ "$output" == *"unity.com/download"* ]]
}

@test "exit 4 when Library/Locks/Packages.lock exists (concurrent Editor)" {
    echo "m_EditorVersion: 6000.3.13f1" > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    mkdir -p "$TEST_PROJ/Library/Locks"
    touch "$TEST_PROJ/Library/Locks/Packages.lock"
    UNITY_EDITOR_PATH="/bin/echo" \
        run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 4 ]
    [[ "$output" == *"Project is locked"* ]]
}

@test "exit 4 when Temp/UnityLockfile exists (alternate lock path)" {
    echo "m_EditorVersion: 6000.3.13f1" > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    mkdir -p "$TEST_PROJ/Temp"
    touch "$TEST_PROJ/Temp/UnityLockfile"
    UNITY_EDITOR_PATH="/bin/echo" \
        run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 4 ]
}

@test "BOM in ProjectVersion.txt is stripped" {
    # UTF-8 BOM (EF BB BF) prefix should not break version parsing
    printf '\xEF\xBB\xBFm_EditorVersion: 6000.3.13f1\n' > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    UNITY_EDITOR_PATH="/nonexistent/Unity" \
        run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    # Should reach exit 2 (editor not found), not exit 5 (invalid format)
    # — proves BOM was stripped before regex validation
    [ "$status" -eq 2 ]
}

@test "CRLF line endings in ProjectVersion.txt are stripped" {
    # Windows-style CRLF should not break version parsing
    printf 'm_EditorVersion: 6000.3.13f1\r\n' > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    UNITY_EDITOR_PATH="/nonexistent/Unity" \
        run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 2 ]
}

@test "exit 6 when Unity batch-mode exceeds timeout (Codex C2)" {
    # Fake Unity that hangs for 30s regardless of args. Timeout=2s should kill it.
    echo "m_EditorVersion: 6000.3.13f1" > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    cat > "$TEST_PROJ/fake-unity-hang.sh" <<'SCRIPT'
#!/bin/bash
sleep 30
SCRIPT
    chmod +x "$TEST_PROJ/fake-unity-hang.sh"
    UNITY_EDITOR_PATH="$TEST_PROJ/fake-unity-hang.sh" \
    UNITY_COMPILE_CHECK_TIMEOUT=2 \
        run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 6 ]
    [[ "$output" == *"timed out after 2s"* ]]
}

@test "exit 7 when Unity log file is unreadable (Codex C1)" {
    # Use /usr/bin/true as fake Unity — exits 0 immediately without writing log.
    # Then delete the log file before the grep step. Hard to simulate cleanly,
    # so we test by pointing to a fake editor that nukes its own log file.
    echo "m_EditorVersion: 6000.3.13f1" > "$TEST_PROJ/ProjectSettings/ProjectVersion.txt"
    # Create a fake Unity that deletes the log file argument and exits 0
    cat > "$TEST_PROJ/fake-unity-no-log.sh" <<'EOF'
#!/bin/bash
# Find -logFile arg and delete the file
for ((i=1; i<=$#; i++)); do
    if [ "${!i}" = "-logFile" ]; then
        nexti=$((i+1))
        rm -f "${!nexti}"
        break
    fi
done
exit 0
EOF
    chmod +x "$TEST_PROJ/fake-unity-no-log.sh"
    UNITY_EDITOR_PATH="$TEST_PROJ/fake-unity-no-log.sh" \
        run bash "$TEST_PROJ/tools/ci/unity-compile-check.sh"
    [ "$status" -eq 7 ]
    [[ "$output" == *"log file unreadable"* ]]
}
