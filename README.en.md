# Unity Claude Template — Start with Solitaire

[한국어](README.md) · **English**

> **No coding experience required.** This is a tutorial for building Solitaire end-to-end by chatting in Claude Desktop. Claude drives the Unity Editor **directly** — writing scripts, assembling scenes, running builds. You just watch and confirm.

---

## 0. The whole flow

1. **Setup (~20 min)** — Unity, Claude Desktop, get the template
2. **Connect two MCPs (~5 min)** — so Claude can touch your files and Unity
3. **"Make me solitaire" — one line** — Claude drives the rest, asking you when it needs input

---

## 1. Prerequisites

| What you need | Where |
|---|---|
| Unity Hub + Unity 2022.3 LTS | [unity.com/download](https://unity.com/download) |
| Claude Desktop app | [claude.ai/download](https://claude.ai/download) |
| Python 3.10+ | macOS: pre-installed, check with `python3 --version` / Windows: [python.org](https://python.org) |
| This template | Top-right **Use this template** → copy to your account → green **Code** button → **Download ZIP** |

Unzip to a place like `~/Documents/my-solitaire`. **Keep the path ASCII-only and without spaces** — it avoids a lot of Unity import headaches.

---

## 2. Connect **two** MCPs to Claude Desktop

Claude needs two MCP connectors to drive your setup:

- **Filesystem MCP** — read/write your project files
- **Claude Bridge MCP** — drive the Unity Editor

### 2-1. Filesystem MCP (~30 seconds)

**1)** Claude Desktop → **Settings** → sidebar **Connectors** → top-right **Browse Connectors**

![Settings → Connectors](docs/images/01-connector-filesystem.png)

**2)** Find **Filesystem** in the list → click **Install**

![Install Filesystem](docs/images/02-filesystem-install.png)

**3)** When the toggle flips to **Active**, click **Configure** and pick your template folder

![Filesystem active](docs/images/03-filesystem-active.png)

### 2-2. Claude Bridge MCP (~1 minute)

This MCP is **already included in the template**. You just install and register it.

**1)** Open a terminal and install the Python package:

```bash
pip3 install mcp
```

**2)** Open Claude Desktop's config file:

- macOS: Finder → `Cmd+Shift+G` → paste `~/Library/Application Support/Claude/` → open `claude_desktop_config.json` in a text editor
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`

**3)** Add a `claude-bridge` entry under `mcpServers` (right after `filesystem`):

```json
{
  "mcpServers": {
    "filesystem": { "...": "..." },
    "claude-bridge": {
      "command": "python3",
      "args": [
        "/Users/YOUR_NAME/Documents/my-solitaire/scripts/claude-bridge-mcp/server.py"
      ]
    }
  }
}
```

**Replace the path** with where you unzipped the template.

**4)** Quit Claude Desktop completely (Cmd+Q on macOS) and relaunch.

**5)** Sanity-check in a new chat:

> Call the `unity_bridge_status` tool and show me the result.

If `project_root` points to your template folder, you're good. `editor_running: false` is expected (Unity isn't running yet).

---

## 3. Start building Solitaire

From here on, **you just chat**. Paste this into Claude Desktop:

> I've never used Unity before. This folder is the unity-claude-template. I want to build a Klondike Solitaire.
>
> Break it into small steps. If you get stuck, ask me for images or clarifications.
>
> I can't code — you write everything. I'll confirm results in the Unity window.

Claude will typically respond with:

1. Reading `CLAUDE.md`, `RULES.md`, `.claude/INDEX.md` first
2. Proposing a 3–5 step design for Solitaire
3. Asking "Shall I start with step 1?"

Say **"yes"** and Claude proceeds step by step. You just answer when asked. Here's what you'll see during the process:

### 3-1. Script generation

Claude creates `Card.cs`, `Deck.cs`, `SolitaireGame.cs`, and similar under `Assets/Scripts/_Core/` and `_UI/`. When Claude Desktop prompts for file-creation permission, click **Allow**.

### 3-2. Card prefab auto-creation (`/make-asset`)

Solitaire needs a Card prefab. The template ships with card images but no prefab. Claude will (silently or with a one-liner notice) invoke the `/make-asset ui` skill to generate a Card prefab (RectTransform + Image) at `Assets/Prefabs/Card.prefab`.

For this Claude needs the Unity Editor running. It calls `/run editor` first, which opens Unity for you.

Once the Editor is up, click **Window → Claude Bridge → Start** *once*. Now the Editor is in "accept Claude commands" mode. It auto-resumes on Editor restart after this first setup.

> If Claude needs a custom icon (heart, star, check mark, etc.), it will generate an **SVG directly**, rasterize it to PNG with ImageMagick or `rsvg-convert`, and import as a Unity Sprite. You don't need to supply images for simple symbols.

### 3-3. Scene assembly

Claude calls `Scene.New` → create `Canvas` → attach `SolitaireGame` component to `GameRoot` → hook up the `cardPrefab` field — all via ClaudeBridge. You can watch GameObjects appear in the Hierarchy one by one.

### 3-4. Run (`/run`)

When done, Claude asks you to run `/run` (a build for the current OS) or hit the **▶ Play** button in Unity. When cards deal out in the initial layout, you're done.

---

## 4. When things go wrong

**If Claude hits an error**, it will usually fix it on its own. You can just say "fix that error" — or even say nothing. The agent reads the Console error and attempts a fix.

**Card images look weird**: the Kenney card pack is pre-installed at `Assets/Art/Cards/PNG/Cards (large)/`. Tell Claude "use images from this folder" if it loses track.

**Unity won't open**: run `./scripts/run-editor.sh` from the terminal. Or manually add the project folder in Unity Hub and double-click.

**Bridge not responding**: check that you pressed **Window → Claude Bridge → Start** in the Editor. If still stuck, close the Editor and ask Claude "run `/run bridge`" — it will process commands headless.

---

## 5. Things to try after it works

Just chat:

- "Change the background to a green felt texture"
- "Add an Undo button"
- "Add a victory effect — cards bouncing with particles"
- "Make a card-back Variant prefab so dealt cards can flip between front and back"
- "Build it for iOS"

Claude will invoke `/make-asset`, `/run editor`, `/run bridge`, `/run ios`, etc. as needed.

---

## 6. What this template actually ships (optional reading)

You can skip this if you're just building Solitaire. Come back here when you want to use the template for real work.

### Agent instruction ecosystem
| File | Role |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | Shared team manual, auto-loaded at session start |
| [`RULES.md`](RULES.md) | Six immutable constraints — violating these actually breaks Unity |
| [`.claude/INDEX.md`](.claude/INDEX.md) | Knowledge / rules / skills index, read first by agents |
| [`.claude/knowledge/`](.claude/knowledge/) | Generic knowledge: Unity performance, C# language, Editor automation |
| [`.claude/rules/`](.claude/rules/) | Path-scoped rules (e.g. "how to handle `.asmdef`") |

### Skills (agents can auto-invoke)
| Skill | What it does |
|---|---|
| [`/task-start`](.claude/skills/task-start.md) / [`/task-done`](.claude/skills/task-done.md) | Task begin/end routines |
| [`/run`](.claude/skills/run.md) | Unity build / **`editor`**=open GUI / **`bridge`**=headless batch |
| [`/make-asset`](.claude/skills/make-asset.md) | Create UGUI prefabs, particles, primitive models, or SVG-drawn sprites. Auto-invoked when referenced assets are missing |
| [`/design`](.claude/skills/design.md) | Turn game design brief into agent prompts |
| [`/self-update`](.claude/skills/self-update.md) | Promote session knowledge into the 5-tier hierarchy |

### Unity Editor automation stack
| Path | Role |
|---|---|
| [`Assets/Editor/ClaudeBridge/`](Assets/Editor/ClaudeBridge/README.md) | File-based IPC + reflection ops (Scene / GameObject / Component / Prefab / Asset / Reflection). Supports both GUI polling and headless `-executeMethod` |
| [`scripts/claude-bridge-mcp/`](scripts/claude-bridge-mcp/README.md) | Python MCP wrapper. Claude Desktop drives the Editor with a single `unity_call(op, args)` |
| [`scripts/run.sh`](scripts/run.sh) / [`run-editor.sh`](scripts/run-editor.sh) / [`bridge-run.sh`](scripts/bridge-run.sh) | Build / open Editor / headless bridge (each is an argument branch of `/run`) |
| [`Assets/Editor/ParallelAgentSetup.cs`](Assets/Editor/ParallelAgentSetup.cs) | Disables Domain Reload — Play Mode entry is effectively instant |
| [`scripts/create-symlinked-worktrees.sh`](scripts/create-symlinked-worktrees.sh) | Git Worktree + symlink parallel agent helpers, for when you run multiple agents simultaneously |

### Assembly skeleton
`_Core`, `_UI`, `_Combat`, `_Rendering` + two test assemblies. Solitaire doesn't use `_Combat`/`_Rendering`, feel free to delete them.

---

## 7. Foundational blog series (Korean)

If you want the design rationale:

1. [Designing an agent's brain](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-AI-에이전트를-실무에-쓴다)
2. [Parallel agent worktrees without Domain Reload](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-병렬-에이전트-설계)
3. [A DAP-based agent debugging environment](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-DAP-기반-에이전트-환경-만들기)

## License

- Template code: [MIT](LICENSE)
- Card graphics: Kenney Playing Cards Pack (CC0) — see [`Assets/Art/Cards/License.txt`](Assets/Art/Cards/License.txt)
