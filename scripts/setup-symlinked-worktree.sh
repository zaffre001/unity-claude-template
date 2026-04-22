#!/bin/bash
# setup-symlinked-worktree.sh
# 사용법: ./scripts/setup-symlinked-worktree.sh agent-0 task/ui-refactor
#
# 소스코드만 Git worktree로 격리하고,
# Assets(비코드), Library, ProjectSettings 등은 심링크로 공유.
#
# 심링크/Library 시딩 로직은 ensure-worktree-setup.sh 에 있다 (멱등).
# 이 스크립트는 `git worktree add` 후 그 훅을 수동으로 한 번 돌린다.

set -eu

AGENT_ID=${1:? "에이전트 ID 필요 (예: agent-0)"}
BRANCH=${2:? "브랜치 이름 필요 (예: task/ui-refactor)"}
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
MAIN_PROJECT="$(pwd)"
WORKTREE_ROOT="../worktrees"
WORKTREE="$WORKTREE_ROOT/$AGENT_ID"

# ── 1. Git worktree 생성 ──
git worktree add -b "$BRANCH" "$WORKTREE" HEAD
echo "OK: Git worktree 생성: $WORKTREE"

# ── 2. 심링크·Library 시딩은 공용 스크립트에 위임 ──
# ensure-worktree-setup.sh 는 .claude/worktrees/* 만 건드리므로,
# 명시적 경로를 넘겨 게이트를 우회한다.
WORKTREE_ABS="$(cd "$WORKTREE" && pwd -P)"
"$SCRIPT_DIR/ensure-worktree-setup.sh" "$WORKTREE_ABS" || true

echo ""
echo "============================================="
echo "  워크트리 준비 완료: $WORKTREE"
echo "  브랜치: $BRANCH"
echo "  심링크: Assets(비코드), Library(캐시), Settings"
echo "  실제 파일: Scripts, Editor, Scenes (Git 관리)"
echo "============================================="
