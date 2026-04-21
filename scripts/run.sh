#!/usr/bin/env bash
# run.sh — Unity player build + launch.
# Called by the /run skill. Safe to invoke directly.
#
# Usage:
#   ./scripts/run.sh [platform]
#     platform: mac | win | linux | webgl | web | android | ios
#     default : current OS
#
# Output:
#   ../builds/{label}-{branch}-{shortsha}[-dirty]-{target}-{timestamp}/
#     BUILD_INFO.txt   — fingerprint
#     unity-build.log  — batchmode log
#     <player files>   — actual build artifact

set -euo pipefail

# ── resolve platform ────────────────────────────────────────────────────
PLATFORM_ARG="${1:-}"
if [ -z "$PLATFORM_ARG" ]; then
    case "$(uname -s)" in
        Darwin)                     PLATFORM_ARG="mac" ;;
        Linux)                      PLATFORM_ARG="linux" ;;
        CYGWIN*|MINGW*|MSYS*)       PLATFORM_ARG="win" ;;
        *)                          PLATFORM_ARG="mac" ;;
    esac
fi

case "$PLATFORM_ARG" in
    mac|macos|osx)
        BUILD_TARGET="StandaloneOSX"
        MODULE_DIR="MacStandaloneSupport"
        MODULE_LABEL="macOS Build Support (Mono)"
        HUB_MODULE="mac-mono" ;;
    win|windows)
        BUILD_TARGET="StandaloneWindows64"
        MODULE_DIR="WindowsStandaloneSupport"
        MODULE_LABEL="Windows Build Support (Mono)"
        HUB_MODULE="windows-mono" ;;
    linux)
        BUILD_TARGET="StandaloneLinux64"
        MODULE_DIR="LinuxStandaloneSupport"
        MODULE_LABEL="Linux Build Support (Mono)"
        HUB_MODULE="linux-mono" ;;
    webgl|web)
        BUILD_TARGET="WebGL"
        MODULE_DIR="WebGLSupport"
        MODULE_LABEL="WebGL Build Support"
        HUB_MODULE="webgl" ;;
    android)
        BUILD_TARGET="Android"
        MODULE_DIR="AndroidPlayer"
        MODULE_LABEL="Android Build Support"
        HUB_MODULE="android" ;;
    ios)
        BUILD_TARGET="iOS"
        MODULE_DIR="iOSSupport"
        MODULE_LABEL="iOS Build Support"
        HUB_MODULE="ios" ;;
    *)
        echo "ERROR: Unknown platform: $PLATFORM_ARG" >&2
        echo "Supported: mac | win | linux | webgl | android | ios" >&2
        exit 2 ;;
esac

# ── project root sanity ─────────────────────────────────────────────────
if [ ! -f ProjectSettings/ProjectVersion.txt ]; then
    echo "ERROR: Run from a Unity project root (ProjectSettings/ProjectVersion.txt missing)." >&2
    exit 2
fi

UNITY_VERSION=$(awk '/m_EditorVersion:/ {print $2; exit}' ProjectSettings/ProjectVersion.txt)
echo "Unity version   : $UNITY_VERSION"
echo "Build target    : $BUILD_TARGET"

# ── Unity editor location ───────────────────────────────────────────────
case "$(uname -s)" in
    Darwin)
        UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"
        UNITY_BIN="$UNITY_APP/Contents/MacOS/Unity"
        PLAYBACK_ENGINES="$UNITY_APP/Contents/PlaybackEngines" ;;
    Linux)
        UNITY_BIN="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"
        PLAYBACK_ENGINES="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/PlaybackEngines" ;;
    *)
        UNITY_BIN="/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe"
        PLAYBACK_ENGINES="/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/PlaybackEngines" ;;
esac

if [ ! -x "$UNITY_BIN" ]; then
    cat >&2 <<EOF
ERROR: Unity Editor not found at:
  $UNITY_BIN

Install Unity $UNITY_VERSION via Unity Hub, then retry.
EOF
    exit 3
fi

# ── platform module installation check ──────────────────────────────────
if [ ! -d "$PLAYBACK_ENGINES/$MODULE_DIR" ]; then
    cat >&2 <<EOF
ERROR: "$MODULE_LABEL" is not installed for Unity $UNITY_VERSION.

Install via Unity Hub GUI:
  1. Open Unity Hub
  2. Installs → $UNITY_VERSION → gear icon → "Add modules"
  3. Check "$MODULE_LABEL" → Continue
  4. Retry:  ./scripts/run.sh $PLATFORM_ARG

Or via Unity Hub CLI (macOS example):
  "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- \\
    --headless install-modules --version $UNITY_VERSION --module $HUB_MODULE

Expected path:
  $PLAYBACK_ENGINES/$MODULE_DIR
EOF
    exit 4
fi

# ── fingerprint ─────────────────────────────────────────────────────────
PROJECT_ROOT="$(pwd)"
PROJECT_DIRNAME="$(basename "$PROJECT_ROOT")"

MAIN_TOPLEVEL=$(git worktree list --porcelain 2>/dev/null | awk '/^worktree / {print $2; exit}' || true)
if [ "${MAIN_TOPLEVEL:-}" = "$PROJECT_ROOT" ]; then
    LABEL="main"
else
    LABEL="$PROJECT_DIRNAME"
fi

BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "nogit")
BRANCH_SAN=$(echo "$BRANCH" | tr '/' '-' | tr -cd '[:alnum:]-_')
SHORTSHA=$(git rev-parse --short HEAD 2>/dev/null || echo "nogit")
FULLSHA=$(git rev-parse HEAD 2>/dev/null || echo "nogit")
DIRTY=""
if ! git diff-index --quiet HEAD 2>/dev/null; then
    DIRTY="-dirty"
fi
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

BUILD_ID="${LABEL}-${BRANCH_SAN}-${SHORTSHA}${DIRTY}-${BUILD_TARGET}-${TIMESTAMP}"

BUILDS_ROOT="$(cd .. && pwd)/builds"
OUTPUT_DIR="$BUILDS_ROOT/$BUILD_ID"
mkdir -p "$OUTPUT_DIR"

cat > "$OUTPUT_DIR/BUILD_INFO.txt" <<EOF
Build ID     : $BUILD_ID
Label        : $LABEL     # "main" = primary worktree; otherwise = worktree dir name
Project dir  : $PROJECT_DIRNAME
Project path : $PROJECT_ROOT
Branch       : $BRANCH
Commit       : $FULLSHA
Short SHA    : $SHORTSHA
Dirty        : $([ -n "$DIRTY" ] && echo "yes" || echo "no")
Platform     : $BUILD_TARGET
Unity        : $UNITY_VERSION
Built at     : $(date -u +%Y-%m-%dT%H:%M:%SZ)
Host         : $(hostname)
EOF

echo "Build output    : $OUTPUT_DIR"
echo "Fingerprint     : $BUILD_ID"
echo ""

# ── build ───────────────────────────────────────────────────────────────
LOG_FILE="$OUTPUT_DIR/unity-build.log"
set +e
"$UNITY_BIN" \
    -batchmode \
    -quit \
    -nographics \
    -projectPath "$PROJECT_ROOT" \
    -buildTarget "$BUILD_TARGET" \
    -executeMethod Project.Editor.RunBuildCommand.Build \
    -outputPath "$OUTPUT_DIR" \
    -logFile "$LOG_FILE"
BUILD_EXIT=$?
set -e

if [ $BUILD_EXIT -ne 0 ]; then
    echo "ERROR: Build failed (exit $BUILD_EXIT). Log: $LOG_FILE" >&2
    echo "--- last 40 log lines ---" >&2
    tail -40 "$LOG_FILE" >&2 || true
    exit 5
fi

echo "Build succeeded."
echo ""

# ── launch ──────────────────────────────────────────────────────────────
case "$BUILD_TARGET" in
    StandaloneOSX)
        APP=$(find "$OUTPUT_DIR" -maxdepth 2 -name "*.app" 2>/dev/null | head -1)
        if [ -n "$APP" ]; then open "$APP" && echo "Launched: $APP"; fi ;;
    StandaloneWindows64)
        EXE=$(find "$OUTPUT_DIR" -maxdepth 2 -name "*.exe" 2>/dev/null | head -1)
        if [ -n "$EXE" ]; then "$EXE" & echo "Launched: $EXE"; fi ;;
    StandaloneLinux64)
        BIN=$(find "$OUTPUT_DIR" -maxdepth 2 -type f -perm -u+x ! -name "*.so" 2>/dev/null | head -1)
        if [ -n "$BIN" ]; then "$BIN" & echo "Launched: $BIN"; fi ;;
    WebGL)
        echo "WebGL build at: $OUTPUT_DIR"
        echo "Serve locally :  (cd \"$OUTPUT_DIR\" && python3 -m http.server 8080)"
        echo "Then open     :  http://localhost:8080" ;;
    Android)
        APK=$(find "$OUTPUT_DIR" -name "*.apk" 2>/dev/null | head -1)
        echo "APK: $APK"
        if command -v adb >/dev/null 2>&1 && [ -n "$APK" ]; then
            echo "Install       :  adb install -r \"$APK\""
        fi ;;
    iOS)
        XCP=$(find "$OUTPUT_DIR" -name "*.xcodeproj" 2>/dev/null | head -1)
        if [ -n "$XCP" ]; then open "$XCP" && echo "Opened Xcode: $XCP"; fi ;;
esac

echo "Done: $OUTPUT_DIR"
