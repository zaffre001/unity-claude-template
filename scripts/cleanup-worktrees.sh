#!/bin/bash
# cleanup-worktrees.sh
# 사용법: ./scripts/cleanup-worktrees.sh
#
# 모든 에이전트 워크트리를 제거하고 잔여 메타데이터를 정리한다.

WORKTREE_ROOT="../worktrees"

if [ ! -d "$WORKTREE_ROOT" ]; then
  echo "워크트리 디렉터리 없음: $WORKTREE_ROOT"
  exit 0
fi

echo "현재 워크트리 목록:"
git worktree list
echo ""

for dir in "$WORKTREE_ROOT"/agent-*; do
  if [ -d "$dir" ]; then
    echo "제거 중: $dir"
    git worktree remove "$dir" --force 2>/dev/null || {
      echo "WARNING: git worktree remove 실패, 수동 삭제: $dir"
      rm -rf "$dir"
    }
  fi
done

git worktree prune
echo ""
echo "정리 완료."
git worktree list
