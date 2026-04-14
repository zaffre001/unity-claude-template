#!/bin/bash
# create-worktrees.sh
# 사용법: ./scripts/create-worktrees.sh 3 main

AGENT_COUNT=${1:-3}
BASE_BRANCH=${2:-main}
WORKTREE_ROOT="../worktrees"

mkdir -p "$WORKTREE_ROOT"

for i in $(seq 0 $((AGENT_COUNT - 1))); do
  BRANCH_NAME="agent/worker-$i"
  WORKTREE_PATH="$WORKTREE_ROOT/agent-$i"

  if [ -d "$WORKTREE_PATH" ]; then
    echo "WARNING: 워크트리 이미 존재: $WORKTREE_PATH — 건너뜀"
    continue
  fi

  git worktree add -b "$BRANCH_NAME" "$WORKTREE_PATH" "$BASE_BRANCH"
  echo "OK: 워크트리 생성: $WORKTREE_PATH ($BRANCH_NAME)"
done

echo ""
git worktree list
