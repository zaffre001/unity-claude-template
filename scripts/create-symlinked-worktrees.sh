#!/bin/bash
# create-symlinked-worktrees.sh
# 사용법: ./scripts/create-symlinked-worktrees.sh 3

AGENT_COUNT=${1:-3}
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

for i in $(seq 0 $((AGENT_COUNT - 1))); do
  "$SCRIPT_DIR/setup-symlinked-worktree.sh" \
    "agent-$i" \
    "agent/worker-$i"
done

echo ""
echo "$AGENT_COUNT 개 심링크 워크트리 생성 완료"
git worktree list
