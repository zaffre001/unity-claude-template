#!/usr/bin/env bash
# wait-editor.sh — 이 프로젝트로 구동된 Unity Editor 프로세스가 뜰 때까지 대기.
#
# Usage:
#   ./scripts/wait-editor.sh [timeout_seconds=30] [poll_interval_seconds=1]
#
# Exit:
#   0 — 준비됨. stdout에 "ready in Ns (pid=N)"
#   1 — 타임아웃. stderr에 "timeout after Ns"
#   2 — 프로젝트 루트 아님
#
# NOTE: 이 스크립트는 에디터 "프로세스"만 확인한다. ClaudeBridge op 응답 준비는
#       `mcp__claude-bridge__unity_bridge_status` 또는 실제 unity_call로 폴링하라.
#
# 에이전트가 인라인 `for i in ...; pgrep ...` 루프를 매번 쓰지 않도록 만든 헬퍼.
# zsh는 `$status` 등 built-in을 read-only로 예약하므로 변수명은 충돌 없는 것으로 유지한다.

set -u

if [ ! -f ProjectSettings/ProjectVersion.txt ]; then
    echo "ERROR: Run from a Unity project root." >&2
    exit 2
fi

TIMEOUT_SEC=${1:-30}
INTERVAL_SEC=${2:-1}
PROJECT_DIR=$(pwd)
ELAPSED=0

while [ "$ELAPSED" -lt "$TIMEOUT_SEC" ]; do
    UNITY_PID=$(pgrep -f "Unity.app/Contents/MacOS/Unity -projectPath $PROJECT_DIR" | head -1 || true)
    if [ -n "${UNITY_PID:-}" ]; then
        echo "ready in ${ELAPSED}s (pid=$UNITY_PID)"
        exit 0
    fi
    sleep "$INTERVAL_SEC"
    ELAPSED=$((ELAPSED + INTERVAL_SEC))
done

echo "timeout after ${TIMEOUT_SEC}s" >&2
exit 1
