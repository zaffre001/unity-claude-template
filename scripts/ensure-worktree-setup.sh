#!/bin/bash
# ensure-worktree-setup.sh
#
# 워크트리를 심링크·Library 시딩까지 포함해 멱등 복구한다.
# - SessionStart 훅에서 stdin JSON({"cwd": "..."}) 으로 호출됨
# - 또는 CLI 에서 인자 없이(현재 PWD 사용) 혹은 경로를 넘겨 호출
#
# Gate:
#   1. 대상 경로가 .claude/worktrees/* 아래가 아니면 no-op 종료
#   2. git 상 linked worktree 가 아니면 no-op 종료 (메인 프로젝트에서 잘못 돌린 경우 방어)
#
# Main 프로젝트 위치는 `git worktree list --porcelain` 의 첫 엔트리로 확정한다.

set -eu

# ── 플랫폼 감지 (Windows Git Bash / MSYS / CYGWIN) ──
IS_WINDOWS=0
case "$(uname -s 2>/dev/null)" in
  MINGW*|MSYS*|CYGWIN*) IS_WINDOWS=1 ;;
esac

# Windows 용 Windows-포맷 경로 변환 (mklink 인자용)
to_native_path() {
  if [ "$IS_WINDOWS" -eq 1 ] && command -v cygpath >/dev/null 2>&1; then
    cygpath -w "$1"
  else
    printf '%s' "$1"
  fi
}

# 디렉터리 링크 생성 — Windows 에선 junction(mklink /J), 그 외 symlink.
# junction 은 admin 없이 생성 가능하며, 같은 볼륨 안에서만 동작한다.
make_dir_link() {
  local src="$1" dst="$2"
  if [ "$IS_WINDOWS" -eq 1 ]; then
    local src_w dst_w
    src_w=$(to_native_path "$src")
    dst_w=$(to_native_path "$dst")
    cmd //c mklink //J "$dst_w" "$src_w" >/dev/null
  else
    ln -sfn "$src" "$dst"
  fi
}

# 파일 링크 생성 — Windows 에선 file symlink 가 admin 필요라 복사로 대체.
make_file_link() {
  local src="$1" dst="$2"
  if [ "$IS_WINDOWS" -eq 1 ]; then
    cp -p "$src" "$dst"
  else
    ln -sfn "$src" "$dst"
  fi
}

# 링크(또는 빈/실폴더) 제거 — Windows junction 안전 경로.
# rmdir 는 junction/symlink/빈 디렉터리면 성공하고 실제 비어있지 않은 폴더면 실패하므로,
# 이 조합이 "junction 을 만들기 전에 target 내용으로 재귀 rm 하는" 사고를 방지한다.
remove_link_or_dir() {
  local dst="$1"
  if [ "$IS_WINDOWS" -eq 1 ]; then
    rmdir "$dst" 2>/dev/null || rm -rf "$dst"
  else
    rm -rf "$dst"
  fi
}

# $dst 가 (symlink/junction/실폴더 관계없이) $src 의 physical path 와 같은지 확인.
# junction 은 readlink 로 못 읽히기 때문에 physical-path 비교로 통일.
links_to() {
  local src="$1" dst="$2"
  [ -e "$dst" ] || return 1
  local src_canon dst_canon
  src_canon=$(cd "$src" 2>/dev/null && pwd -P) || return 1
  dst_canon=$(cd "$dst" 2>/dev/null && pwd -P) || return 1
  [ "$src_canon" = "$dst_canon" ]
}

# ── stdin 에서 cwd 파싱 (SessionStart 훅 호출 경로) ──
parse_cwd_from_stdin() {
  if [ -t 0 ]; then
    return 1
  fi
  local input
  input=$(cat)
  if [ -z "$input" ]; then
    return 1
  fi
  if command -v jq >/dev/null 2>&1; then
    printf '%s' "$input" | jq -r '.cwd // empty'
  elif command -v python3 >/dev/null 2>&1; then
    printf '%s' "$input" | python3 -c 'import json,sys
try:
  print(json.load(sys.stdin).get("cwd", ""))
except Exception:
  pass'
  else
    return 1
  fi
}

# ── 대상 워크트리 경로 결정 ──
# 인자가 있으면 명시적 호출(setup-symlinked-worktree.sh 경유) → 경로 게이트 우회
# 인자가 없으면 훅/자동 호출 → stdin JSON.cwd 또는 PWD, .claude/worktrees/* 만 처리
EXPLICIT=0
if [ "$#" -gt 0 ] && [ -n "${1:-}" ]; then
  EXPLICIT=1
  TARGET="$1"
else
  STDIN_CWD=$(parse_cwd_from_stdin || true)
  TARGET="${STDIN_CWD:-$PWD}"
fi

if [ ! -d "$TARGET" ]; then
  exit 0
fi

TARGET=$(cd "$TARGET" && pwd -P)

# ── Gate 1: 자동 호출이면 .claude/worktrees/ 경로만 처리 ──
if [ "$EXPLICIT" -eq 0 ]; then
  case "$TARGET" in
    *"/.claude/worktrees/"*) ;;
    *) exit 0 ;;
  esac
fi

# ── Gate 2: linked worktree 인지 확인 ──
GIT_DIR=$(git -C "$TARGET" rev-parse --git-dir 2>/dev/null || echo "")
GIT_COMMON=$(git -C "$TARGET" rev-parse --git-common-dir 2>/dev/null || echo "")
if [ -z "$GIT_DIR" ] || [ "$GIT_DIR" = "$GIT_COMMON" ]; then
  exit 0
fi

# ── Main 프로젝트 경로 ──
MAIN_PROJECT=$(git -C "$TARGET" worktree list --porcelain 2>/dev/null \
  | awk '/^worktree / { print $2; exit }')
if [ -z "$MAIN_PROJECT" ] || [ ! -d "$MAIN_PROJECT" ]; then
  echo "ensure-worktree-setup: main worktree 를 찾을 수 없음" >&2
  exit 0
fi

# physical path 로 정규화 (links_to 비교·mklink 절대 경로 요구사항 둘 다 만족)
MAIN_PROJECT=$(cd "$MAIN_PROJECT" && pwd -P)

if [ "$MAIN_PROJECT" = "$TARGET" ]; then
  exit 0
fi

# ── 심링크 대상 (RULES.md RULE-02 + parallel-work.md §1 과 일치) ──
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

# Library 안에서 '복사'해야 하는 항목 (병렬 Unity ArtifactDB·어셈블리 레이스 방지)
LIBRARY_LOCK_ITEMS=(
  "ArtifactDB"
  "SourceAssetDB"
  "Artifacts"
  "ArtifactDB-lock"
  "SourceAssetDB-lock"
  "BuildPlayer.prefs"
  "ScriptAssemblies"
  "Bee"
)

CHANGED=0

# $1: 메인측 소스 경로(절대), $2: 워크트리측 목표 경로(절대), $3: 로그용 상대 라벨
link_or_skip() {
  local src="$1"
  local dst="$2"
  local label="$3"

  if [ ! -d "$src" ]; then
    return 0
  fi

  # 이미 올바르게 연결돼 있으면 no-op (symlink/junction/실폴더 모두 커버)
  if links_to "$src" "$dst"; then
    return 0
  fi

  if [ -e "$dst" ] || [ -L "$dst" ]; then
    # 실제 폴더/파일이거나 잘못된 링크. 워크트리의 로컬 변경 확인 후 제거
    local rel="${dst#$TARGET/}"
    local dirty
    dirty=$(git -C "$TARGET" status --porcelain -- "$rel" 2>/dev/null || echo "")
    if [ -n "$dirty" ]; then
      echo "SKIP (dirty): $label — 로컬 변경 존재, 커밋/스태시 후 재실행" >&2
      return 0
    fi
    remove_link_or_dir "$dst"
  fi

  mkdir -p "$(dirname "$dst")"
  if ! make_dir_link "$src" "$dst" 2>/dev/null; then
    echo "FAIL:   $label — 링크 생성 실패 (Windows cross-volume 여부 확인)" >&2
    return 1
  fi
  echo "LINK:   $label"
  CHANGED=1
}

# ── 3. Assets 심링크 ──
for rel in "${SYMLINK_ASSET_DIRS[@]}"; do
  link_or_skip "$MAIN_PROJECT/$rel" "$TARGET/$rel" "$rel"
done

# ── 4. 루트 심링크 ──
for rel in "${SYMLINK_ROOT_DIRS[@]}"; do
  link_or_skip "$MAIN_PROJECT/$rel" "$TARGET/$rel" "$rel"
done

# ── 5. Library 시딩 — 존재하면 건드리지 않음 ──
LIBRARY_SRC="$MAIN_PROJECT/Library"
LIBRARY_DST="$TARGET/Library"

if [ -d "$LIBRARY_SRC" ] && [ ! -e "$LIBRARY_DST" ]; then
  mkdir -p "$LIBRARY_DST"
  for item in "$LIBRARY_SRC"/*; do
    [ -e "$item" ] || continue
    local_name=$(basename "$item")
    is_lock=false
    for lock in "${LIBRARY_LOCK_ITEMS[@]}"; do
      if [ "$local_name" = "$lock" ]; then
        is_lock=true
        break
      fi
    done
    if [ "$is_lock" = true ]; then
      cp -R "$item" "$LIBRARY_DST/$local_name" 2>/dev/null || true
    elif [ -d "$item" ]; then
      # 디렉터리는 junction/symlink, 실패 시 복사로 fallback
      make_dir_link "$item" "$LIBRARY_DST/$local_name" 2>/dev/null \
        || cp -R "$item" "$LIBRARY_DST/$local_name" 2>/dev/null \
        || true
    else
      make_file_link "$item" "$LIBRARY_DST/$local_name" 2>/dev/null || true
    fi
  done
  echo "SEED:   Library (lock 항목은 복사, 나머지는 링크)"
  CHANGED=1
fi

if [ "$CHANGED" -eq 0 ]; then
  exit 0
fi

echo "워크트리 복구 완료: $TARGET"
exit 0
