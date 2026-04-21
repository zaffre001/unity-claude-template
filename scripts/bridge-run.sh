#!/usr/bin/env bash
# bridge-run.sh — Unity를 headless(-batchmode)로 띄워 .claude-bridge/inbox/*.json 을 일괄 처리.
#
# Usage:
#   ./scripts/bridge-run.sh
#
# 전제:
#   이미 .claude-bridge/inbox/ 아래에 커맨드 JSON이 드롭되어 있어야 한다.
#   (Claude가 Filesystem MCP로 파일을 쓴 뒤 이 스크립트를 호출하는 흐름)
#
# 종료 코드:
#   0 — 모든 커맨드 성공
#   1 — 일부/전부 실패 (.claude-bridge/outbox/*.json 의 ok 플래그 확인)
#   2 — 프로젝트 루트 아님
#   3 — Unity Editor 미설치
#   4 — Unity 프로세스 자체 실패(로그 참조)

set -euo pipefail

if [ ! -f ProjectSettings/ProjectVersion.txt ]; then
    echo "ERROR: Run from a Unity project root." >&2
    exit 2
fi

UNITY_VERSION=$(awk '/m_EditorVersion:/ {print $2; exit}' ProjectSettings/ProjectVersion.txt)

case "$(uname -s)" in
    Darwin)  UNITY_BIN="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity" ;;
    Linux)   UNITY_BIN="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity" ;;
    *)       UNITY_BIN="/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe" ;;
esac

if [ ! -x "$UNITY_BIN" ]; then
    cat >&2 <<EOF
ERROR: Unity Editor not found at:
  $UNITY_BIN

Install Unity $UNITY_VERSION via Unity Hub, then retry.
EOF
    exit 3
fi

mkdir -p .claude-bridge/inbox .claude-bridge/outbox .claude-bridge/logs

INBOX_COUNT=$(find .claude-bridge/inbox -maxdepth 1 -name "*.json" | wc -l | tr -d ' ')
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE=".claude-bridge/logs/bridge-$TIMESTAMP.log"

echo "Unity version : $UNITY_VERSION"
echo "Inbox count   : $INBOX_COUNT"
echo "Log           : $LOG_FILE"
echo ""

set +e
"$UNITY_BIN" \
    -batchmode \
    -nographics \
    -quit \
    -projectPath "$(pwd)" \
    -executeMethod Project.Editor.ClaudeBridge.ClaudeBridgeBatch.Run \
    -logFile "$LOG_FILE"
EXIT=$?
set -e

# Unity 자체 실패는 4로 매핑 (커맨드 일부 실패 1과 구분)
if [ $EXIT -ne 0 ] && [ $EXIT -ne 1 ]; then
    echo "ERROR: Unity process failed (exit $EXIT). Log: $LOG_FILE" >&2
    echo "--- last 40 log lines ---" >&2
    tail -40 "$LOG_FILE" >&2 || true
    exit 4
fi

OUTBOX_COUNT=$(find .claude-bridge/outbox -maxdepth 1 -name "*.json" | wc -l | tr -d ' ')
echo ""
echo "Outbox result : $OUTBOX_COUNT file(s)"
echo "Exit code     : $EXIT  (0=all ok, 1=partial fail)"

exit $EXIT
