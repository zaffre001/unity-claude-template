#!/usr/bin/env bash
# run-editor.sh — Unity Editor GUI를 현재 프로젝트로 연다.
# `/run editor` 스킬이 이걸 호출한다. 빌드는 하지 않음.
#
# Usage:
#   ./scripts/run-editor.sh
#
# 종료 코드:
#   0 — Editor 실행(백그라운드) 명령을 보냄. Unity 자체의 동작은 비동기
#   2 — 프로젝트 루트 아님
#   3 — Unity Editor 미설치
#
# 같은 프로젝트로 이미 떠 있으면 그 인스턴스를 전면으로 올린다.
# 다른 프로젝트(= 워크트리)로 이미 떠 있으면 새 인스턴스를 강제로 띄운다
# (macOS `open -a` 는 기본적으로 기존 인스턴스에 args 를 포워드해버리므로
#  `-n` 플래그가 없으면 두 번째 워크트리가 열리지 않는다).

set -euo pipefail

if [ ! -f ProjectSettings/ProjectVersion.txt ]; then
    echo "ERROR: Run from a Unity project root." >&2
    exit 2
fi

UNITY_VERSION=$(awk '/m_EditorVersion:/ {print $2; exit}' ProjectSettings/ProjectVersion.txt)
PROJECT_PATH="$(pwd)"

case "$(uname -s)" in
    Darwin)
        UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"
        if [ ! -d "$UNITY_APP" ]; then
            cat >&2 <<EOF
ERROR: Unity Editor not found at:
  $UNITY_APP

Install Unity $UNITY_VERSION via Unity Hub, then retry.
EOF
            exit 3
        fi
        if pgrep -fl "Unity.app/Contents/MacOS/Unity" 2>/dev/null | grep -qF " -projectPath $PROJECT_PATH"; then
            open -a "$UNITY_APP" --args -projectPath "$PROJECT_PATH"
        else
            open -n -a "$UNITY_APP" --args -projectPath "$PROJECT_PATH"
        fi
        ;;
    Linux)
        UNITY_BIN="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"
        if [ ! -x "$UNITY_BIN" ]; then
            echo "ERROR: Unity Editor not found at: $UNITY_BIN" >&2
            exit 3
        fi
        nohup "$UNITY_BIN" -projectPath "$(pwd)" >/dev/null 2>&1 &
        ;;
    *)
        UNITY_BIN="/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe"
        if [ ! -x "$UNITY_BIN" ]; then
            echo "ERROR: Unity Editor not found at: $UNITY_BIN" >&2
            exit 3
        fi
        "$UNITY_BIN" -projectPath "$(pwd)" &
        ;;
esac

echo "Unity $UNITY_VERSION opening for project: $(pwd)"
echo "(창이 뜨는 데 10~30초 걸릴 수 있습니다)"
