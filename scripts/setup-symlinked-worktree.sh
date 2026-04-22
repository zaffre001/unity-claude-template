#!/bin/bash
# setup-symlinked-worktree.sh
# 사용법: ./scripts/setup-symlinked-worktree.sh agent-0 task/ui-refactor
#
# 소스코드만 Git worktree로 격리하고,
# Assets(비코드), Library, ProjectSettings 등은 심링크로 공유

AGENT_ID=${1:? "에이전트 ID 필요 (예: agent-0)"}
BRANCH=${2:? "브랜치 이름 필요 (예: task/ui-refactor)"}
MAIN_PROJECT="$(pwd)"
WORKTREE_ROOT="../worktrees"
WORKTREE="$WORKTREE_ROOT/$AGENT_ID"

# ── 1. Git worktree 생성 ──
git worktree add -b "$BRANCH" "$WORKTREE" HEAD
echo "OK: Git worktree 생성: $WORKTREE"

# ── 2. 심링크 대상 (에이전트가 수정하지 않는 것들) ──
SYMLINK_ASSET_DIRS=(
  "Assets/Art"
  "Assets/Audio"
  "Assets/Animations"
  "Assets/Prefabs"
  "Assets/Materials"
  "Assets/Textures"
  "Assets/Models"
  "Assets/Plugins"
  "Assets/Resources"
  "Assets/StreamingAssets"
  "Assets/AddressableAssetsData"
)

SYMLINK_ROOT_DIRS=(
  "ProjectSettings"
  "Packages"
)

# ── 3. Assets 심링크 생성 ──
for dir in "${SYMLINK_ASSET_DIRS[@]}"; do
  if [ -d "$MAIN_PROJECT/$dir" ]; then
    rm -rf "$WORKTREE/$dir"
    ln -sfn "$(realpath "$MAIN_PROJECT/$dir")" "$WORKTREE/$dir"
    echo "LINK: $dir"
  fi
done

# ── 4. 루트 폴더 심링크 ──
for dir in "${SYMLINK_ROOT_DIRS[@]}"; do
  if [ -d "$MAIN_PROJECT/$dir" ]; then
    rm -rf "$WORKTREE/$dir"
    ln -sfn "$(realpath "$MAIN_PROJECT/$dir")" "$WORKTREE/$dir"
    echo "LINK: $dir"
  fi
done

# ── 5. Library 처리 — 선택적 심링크 ──
LIBRARY_SRC="$MAIN_PROJECT/Library"
LIBRARY_DST="$WORKTREE/Library"

if [ -d "$LIBRARY_SRC" ]; then
  mkdir -p "$LIBRARY_DST"

  # 워크트리별로 '복사'해야 하는 Library 하위 항목들.
  # 심링크로 공유하면 병렬 Unity 에디터들이 같은 파일을 동시에 쓰면서
  # 어셈블리 혼선·ArtifactDB 충돌이 생긴다.
  LOCK_FILES=(
    "ArtifactDB"
    "SourceAssetDB"
    "Artifacts"
    "ArtifactDB-lock"
    "SourceAssetDB-lock"
    "BuildPlayer.prefs"
    "ScriptAssemblies"    # 컴파일된 .dll — 병렬 컴파일 레이스 방지
    "Bee"                 # Unity 빌드 시스템 작업 디렉터리 (Csc 중간 산출물 포함)
  )

  for item in "$LIBRARY_SRC"/*; do
    basename=$(basename "$item")
    is_locked=false

    for lock in "${LOCK_FILES[@]}"; do
      if [ "$basename" = "$lock" ]; then
        is_locked=true
        break
      fi
    done

    if [ "$is_locked" = true ]; then
      cp -r "$item" "$LIBRARY_DST/$basename" 2>/dev/null
      echo "COPY (Lock): Library/$basename"
    else
      ln -sfn "$(realpath "$item")" "$LIBRARY_DST/$basename"
      echo "LINK: Library/$basename"
    fi
  done
fi

echo ""
echo "============================================="
echo "  워크트리 준비 완료: $WORKTREE"
echo "  브랜치: $BRANCH"
echo "  심링크: Assets(비코드), Library(캐시), Settings"
echo "  실제 파일: Scripts, Editor, Scenes (Git 관리)"
echo "============================================="
