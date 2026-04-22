# Unity Claude Template — a starting point for your game

[한국어](README.md) · **English**

> **This repo is an empty foundation for starting your own Unity game with Claude.**
> The plumbing is pre-wired so an AI agent can assemble scenes, write scripts, and create prefabs for you.
>
> Since an empty project doesn't say much on its own, **we build Solitaire together first as a warm-up** — a chance to feel how the tools click together. Once you've got the hang of it, you wipe out the solitaire bits and pivot into whatever genre you actually want to build (puzzle, platformer, RPG, rhythm game — anything).
>
> No programming experience needed. Just chat, and Claude takes care of the rest.

---

## What this guide walks you through

1. **Setup** (~10 min) — Unity, Claude Code, get the template
2. **Auto bootstrap** (~1 min) — one `/setup` command wires MCP + bridge
3. **Warm-up: build Solitaire together** — get a feel for the tools
4. **Clean up** — wipe the solitaire bits and reset the template
5. **Pivot to your own game** — genre-specific guides + prompts you can copy
6. **Skill reference** — what each skill does, with example prompts

---

## 1. Prerequisites

| What you need | Where |
|---|---|
| Unity Hub + Unity 2022.3 LTS | [unity.com/download](https://unity.com/download) |
| Claude Code desktop app | [claude.com/claude-code](https://www.anthropic.com/claude-code) — macOS and Windows. Run the installer |
| This template | Top-right **Use this template** to fork into your account, or green **Code** button → **Download ZIP** |

Unzip somewhere like `~/Documents/my-game`. **Keep the path ASCII-only and without spaces** — it avoids a lot of Unity import headaches.

> **No Python, no pipx, no Filesystem MCP.** `/setup` handles everything — it installs `uv` (a Python environment manager) if missing and wires the bridge. On first run, `/setup` will show you a one-line install command for `uv` matching your OS (curl on macOS/Linux, PowerShell on Windows).

---

## 2. Auto bootstrap — one `/setup` command

**1)** Launch the Claude Code desktop app.

**2)** Go to the **Code tab** and start a new session. In the prompt area, click **Project folder** and pick the template folder you unzipped (e.g. `~/Documents/my-game`). Drag-and-drop onto the window works too.

**3)** Claude Code auto-loads this project's `.mcp.json` and starts the bridge MCP server. If an approval dialog appears, click **Approve**.

**4)** Type `/` in the prompt box to see the command autocomplete, or just ask:

> /setup please.

From here Claude runs **everything through its own Bash tool** — you don't copy-paste commands into a terminal:

- Detects your OS (`darwin` / `win32` / `linux`)
- Runs `uv --version` to check for `uv` (harmless command, no permission prompt)
- If `uv` is missing, Claude **executes the install command itself** (`curl ... | sh` on macOS/Linux, PowerShell one-liner on Windows). This is the only step that asks for a permission prompt — just click **Approve**
- Verifies `.mcp.json` is in place at the project root (it ships with the template)
- Calls `unity_bridge_status` to confirm Unity Editor detection and project path
- Reports a checklist with only what was fixed or still needs attention

> **What you actually type: just two things** — (a) `/setup`, and (b) if `uv` isn't installed, click the **Approve** button on the install dialog. That's it. Claude drives every command through its own Bash tool — no copying paths, env vars, or installation steps.

**5)** You'll get a summary like:

```
Setup check
  ✓ uv installed
  ✓ .mcp.json found
  ✓ Unity Editor detected: /Applications/Unity/Hub/Editor/2022.3.45f1/...
  ✓ Bridge MCP responded: project_root = /Users/.../my-game

Next
  · /run editor to open Unity
  · Head to §3 "Solitaire warm-up"
```

> **Why is this so short?** The Claude Code desktop app auto-loads `.mcp.json` from the project root. This template's `.mcp.json` runs the bridge via `uv run` — `uv` works identically on macOS/Windows/Linux and manages its own venv, so no more pipx/pip/PATH gymnastics.

**Updating the bridge:** just `git pull`. `uv run` picks up the new code on the next call — no reinstall needed.

> **Prefer the Claude Code CLI?** It behaves identically — `.mcp.json`, `~/.claude.json`, `settings.json`, CLAUDE.md, and skills are all shared between the desktop app and CLI. Just `cd ~/Documents/my-game && claude`.
>
> **The Claude Desktop chat app is a different product.** Its `claude_desktop_config.json` has nothing to do with this template. If you insist on registering the bridge there, see `.claude/skills/setup.md` §5 "Legacy path" for the manual pipx route — not recommended.

---

## 3. Warm-up: build Solitaire together

> This section is a **practice run to get used to how the tools fit together** — the endgame is to clean this up and move to your real project. Don't focus on "how to make solitaire"; focus on "how to tell Claude to do things in Unity."

Paste this into Claude Code:

> I've never used Unity before. This folder is the unity-claude-template. Let's build a Klondike Solitaire as practice.
>
> Break it into small steps. I can't code — you write everything. I'll confirm results in the Unity window.

Claude will typically:

1. Skim `CLAUDE.md`, `RULES.md`, `.claude/INDEX.md` for house rules
2. Propose 3–5 steps
3. Ask "Shall I start with step 1?"

Just answer **"yes"** and Claude proceeds. Mid-way it'll call `/run editor` to open Unity itself when needed, and `/make-asset` to generate a Card prefab on the fly. Small symbols (hearts, etc.) — it **draws SVG directly**, rasterizes to PNG, and imports as a Sprite.

When done, hit **▶ Play** in Unity. When cards deal out in the initial layout, you're good.

> **If you get stuck**: paste the error into chat and say "fix it." Claude usually does. Often it spots console errors and fixes them without being asked.

---

## 4. Clean up and reset the template

Once the warm-up's done, wipe the solitaire bits. Have Claude do it:

> Practice's over. Do these four things:
>
> 1. Delete all solitaire files — `Assets/Scripts/_Core/Card*.cs, Deck*.cs, Solitaire*.cs` / `Assets/Prefabs/Card.prefab` / `Assets/Scenes/Solitaire.unity` / related Variant prefabs.
> 2. Ask me whether to keep `Assets/Art/Cards/`. Keep if I might reuse the pack, delete otherwise.
> 3. Show each file you're about to delete and confirm with me first.
> 4. After deletion, check for compile errors or broken references and fix them.

Claude will walk through each file, get your go-ahead, and handle the cleanup. Anything worth remembering from the practice (patterns, pitfalls) can be promoted into `.claude/domain/` or the manuals.

**If you want an absolutely clean wipe**, one line does it:

> Run /task-start and /task-done end-to-end, strip every solitaire trace, and return the template to its original state. Don't touch `Assets/Editor/`, `scripts/`, or `.claude/` without my permission — those are the template's skeleton.

---

## 5. Pivot to your own game

Now the real game starts. Three steps.

### 5-1. Rename game + namespace

The template defaults to `Project.Core`, `Project.UI`, etc. Rename to your game:

> My game is called "Rainbow Fishing." Rename all `Project.*` namespaces in this template to `RainbowFishing.*`. That includes `.asmdef` name fields, every `.cs` file's `namespace`, and references in `CLAUDE.md`. Also update `productName` in `ProjectSettings/ProjectSettings.asset` to "Rainbow Fishing."

### 5-2. Reshape assembly skeleton for your genre

Default skeleton: `_Core`, `_UI`, `_Combat`, `_Rendering`. Delete what you don't need, add what you do.

| Genre | Suggested skeleton |
|---|---|
| Puzzle / card / board | `_Core`, `_UI`, `_Gameplay` (rename `_Combat`), drop `_Rendering` |
| Platformer / action | `_Core`, `_UI`, `_Player`, `_Level`, `_Combat`, `_Rendering` |
| RPG / adventure | `_Core`, `_UI`, `_Battle`, `_Inventory`, `_Dialog`, `_Quest`, `_Rendering` |
| Rhythm / music | `_Core`, `_UI`, `_Audio`, `_Chart`, `_Input`, drop `_Rendering` |
| Sim / tycoon | `_Core`, `_UI`, `_Economy`, `_AI`, `_Time` |
| Racing / sports | `_Core`, `_UI`, `_Vehicle`, `_Track`, `_Physics`, `_Rendering` |

Ask Claude:

> I'm making a puzzle game. Delete `_Combat` and `_Rendering`, create a new `_Puzzle` assembly with namespace `RainbowFishing.Puzzle`. Update `.asmdef` dependencies and keep `autoReferenced: false`.

### 5-3. Hand Claude your design and run /design

Give Claude your game concept in plain language. Claude breaks it into areas and writes step prompts specific enough for the next agent to execute verbatim.

> /design the following:
>
> Rainbow Fishing — 2D top-down fishing puzzle. Player catches colored fish to complete a seven-color rainbow. 90-second timer. Catching three of the same color grants +5 seconds.
>
> Areas: `_Core` (data + rules), `_UI` (timer / rainbow gauge / score), `_Puzzle` (catch detection / spawning). Make each area's step-1 task concrete enough that the next agent can run it directly.

Claude drops per-area task files into `design/rainbow-fishing/`. Hand them out one by one: "do this task file."

### 5-4. Genre-flavored first-asset prompts

**Puzzle**
> /make-asset — 3×3 grid tile prefab. Click toggles color. Red / blue / yellow, exposed in Inspector.

**Platformer**
> /make-asset — placeholder player model. Capsule body + Cube hat. `Assets/Prefabs/Player/Player.prefab` so `PlayerController.cs` in `_Player` can reference it.

**RPG**
> /make-asset particle — healing spell. Soft green glow rising above the head, ~2 seconds.

**Rhythm**
> /make-asset ui — beat judge ring. White outer edge, shrinking yellow inner ring. 256×256 prefab.

**Fishing / sim**
> /make-asset icon — bobber icon 128×128. Red ball with a small white rod below. Draw as SVG.

---

## 6. When things go wrong

**If Claude hits an error**, it usually fixes itself. "Fix that error" works. Sometimes it catches console errors without being asked.

**Unity won't open**: run `./scripts/run-editor.sh` from terminal, or manually add the folder in Unity Hub and double-click.

**Bridge not responding**: check you pressed **Window → Claude Bridge → Start** in the Editor. If still stuck, close Unity and ask Claude "run `/run bridge`" — it'll process headless.

**You deleted something important during cleanup**: don't panic. It's a Git repo, so it's recoverable. Ask Claude: "restore that, and tell me which of the things we just deleted actually matter."

---

## 7. Skills you can use

Skills are workflows you invoke with `/name`. Ones marked **auto** can be invoked by agents on your behalf without being explicitly asked.

### `/setup` — first-time bootstrap

Run this once per machine. Claude walks the checklist: OS detection → `uv` install check → `.mcp.json` verify → Unity Editor detection → Bridge MCP connectivity. Works on macOS, Windows, and Linux.

**Does:**
- Shows the one-line `uv` install command for your OS when missing (curl on macOS/Linux, PowerShell on Windows)
- Verifies/repairs `.mcp.json` at the project root
- Calls `mcp__claude-bridge__unity_bridge_status` to confirm project path + Unity detection
- Reports a checklist (✓ / ✗) with concrete next steps

**Example prompts**
> /setup please.
>
> /setup — opening this on a new machine, run through the whole bootstrap.

### `/task-start` — scope & brief for a new task (auto)

Before a new task, locks down **what you're going to touch** and loads only the manual sections that matter. Scans `.claude/INDEX.md` with keywords and only opens relevant files. Saves time and context tokens.

**Does:**
- Picks relevant files from `.claude/knowledge/*`, `.claude/rules/*`, `RULES.md` based on task keywords
- Greps for the files this task will touch, declares scope
- Re-checks project-specific absolute rules (like `autoReferenced: false`)
- Summarizes the scope in one line

**Examples**
> /task-start make an HUD scoreboard. Only touch the `_UI` assembly — leave other assemblies alone.
>
> /task-start fix the jump logic — double-jump but never triple. Find the relevant files and start.

### `/task-done` — wrap up a task (auto)

Closes out a task cleanly and **promotes new knowledge** into the manual hierarchy so the next agent doesn't repeat your mistakes.

**Does:**
- Summarizes what changed (files, components)
- Cleans up dangling debug code, commented-out lines, temp logs
- Proposes promoting new domain knowledge to `.claude/domain/*.md`
- Optionally chains into `/self-update`
- Quick verification that build or tests still pass

**Examples**
> That worked. /task-done.
>
> /task-done and pull out anything worth promoting to the manuals.

### `/self-update` — promote learnings into the manuals

Analyzes the session and **proposes which knowledge tier** new lessons should go into. Always gets your approval before writing.

**Does:**
- Picks the right tier across RULES / knowledge / domain / CLAUDE / local
- Shows concrete "here's the patch I propose" diffs
- Checks against existing knowledge to avoid duplication
- Drops trivial one-liners so the manuals don't bloat

**Examples**
> RectTransform anchors confused me for an hour today. /self-update — add a tip to the manual.
>
> /self-update — figure out where the fishing-rod collider quirk should live.

### `/design` — break a design doc into agent prompts

Takes your design in natural language (or a Notion link) and **splits it by area**, producing per-step prompts the next agent can execute directly. Great when you have multiple agents working in parallel.

**Does:**
- Splits design by assembly (`_Core`, `_UI`, `_Puzzle`, `_Player`, etc.)
- Annotates each step with deliverable, verification, and constraints
- Writes per-step markdown files under `design/{slug}/`
- Marks cross-step dependencies (do X before Y)

**Examples**
> /design Rainbow Fishing — 90s round catching colored fish to fill a rainbow. Split into `_Core`, `_UI`, `_Puzzle`.
>
> Read this Notion link and /design it into tasks: https://notion...

### `/run` — run Unity (auto)

Branches three ways depending on argument.

**Arg 1. (none or platform name) — build + launch**
- `mac` / `win` / `linux` / `webgl` / `android` / `ios`
- Output lands in `../builds/{label}-{branch}-{shortsha}-{target}-{timestamp}/`
- Launches the resulting binary automatically (WebGL gets a serve-command hint)

**Arg 2. `editor` — open Unity GUI**
- No build, just pops the Editor for your current project
- If Unity's already open on this project, brings that window forward

**Arg 3. `bridge` — flush queued bridge commands**
- Runs `.claude-bridge/inbox/*.json` through a headless Unity without opening the Editor
- Returns a summary + list of any failed commands

**Examples**
> /run — build for my Mac and launch it.
>
> /run editor — open Unity so I can poke the Hierarchy myself.
>
> /run bridge — flush the commands we queued.
>
> /run webgl — build so I can test in a browser.

### `/make-asset` — generate an asset on the fly (auto)

Creates Unity assets on demand. If you're assembling a scene and some referenced prefab doesn't exist, Claude will reach for this skill first.

**Creates:**
- **UGUI prefabs** — buttons, panels, text blocks as RectTransform + Image + Text + Button combos, including state variants
- **Particles** — hits, explosions, sparkles, heal auras as ParticleSystem prefabs
- **Primitive placeholder models** — Cube/Sphere/Cylinder combos that stand in for real 3D art
- **SVG-drawn icons** — hearts, stars, checkmarks, X, arrows, card suits. **Claude drafts the SVG directly**, rasterizes to PNG, and imports as a Sprite. You don't need to supply image files for simple symbols.
- **User-provided image import** — for photos or complex illustrations, you supply file or URL and Claude sets up the Sprite importer

If the ask is vague, Claude asks about size, color, and where it'll be used first. If you don't answer, it falls back to "temporary color block, easy to swap later."

**Examples**
> /make-asset — a puzzle tile prefab. Going in a 3×3 grid; expose color in Inspector.
>
> /make-asset particle — dust puff when the player lands from a jump. Short, light.
>
> /make-asset icon — HP heart icon 128×128. Red, slightly darker outline. Draw as SVG.
>
> /make-asset prefab — dialog popup. Centered text area, "Next" button at bottom. Keep the skin default, I'll restyle later.
>
> /make-asset model — placeholder player. Capsule body, Sphere head, blue tint.

---

## 8. Inside the template (optional reading)

Skip if you're just doing the warm-up. Come back here when you're building for real.

### House rules Claude follows
| File | What it does |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | Shared team manual, auto-loaded at session start. Put your game's overview here once you pivot |
| [`RULES.md`](RULES.md) | Six immutable constraints — violating these actually breaks Unity |
| [`.claude/INDEX.md`](.claude/INDEX.md) | Knowledge index — agents read this first |
| [`.claude/knowledge/`](.claude/knowledge/) | Cross-project knowledge: Unity performance, C# language, Editor automation |
| [`.claude/rules/`](.claude/rules/) | Path-scoped rules (e.g. "how to handle `.asmdef`") |
| [`.claude/domain/`](.claude/domain/) | Where your game-specific knowledge accumulates over time |

### Skills
Live under `.claude/skills/`. §7 above is a summary; each file has the full treatment.

### Unity Editor automation
| Path | What it does |
|---|---|
| [`.mcp.json`](.mcp.json) | Project-scoped MCP server registration. Claude Code auto-loads it and launches the bridge via `uv run` — works identically on macOS/Windows/Linux |
| [`Assets/Editor/ClaudeBridge/`](Assets/Editor/ClaudeBridge/README.md) | File-based IPC + 22 reflection-driven ops (Scene / GameObject / Component / Prefab / Asset / Reflection). Works with Editor open *and* headless |
| [`scripts/claude-bridge-mcp/`](scripts/claude-bridge-mcp/README.md) | Thin Python MCP wrapper. Claude Code drives Unity via one `unity_call(op, args)` |
| [`scripts/run.sh`](scripts/run.sh) / [`run-editor.sh`](scripts/run-editor.sh) / [`bridge-run.sh`](scripts/bridge-run.sh) | The three shell scripts `/run` dispatches to |
| [`Assets/Editor/ParallelAgentSetup.cs`](Assets/Editor/ParallelAgentSetup.cs) | Disables Domain Reload — Play Mode entry is effectively instant |
| [`scripts/create-symlinked-worktrees.sh`](scripts/create-symlinked-worktrees.sh) | Git Worktree + symlink helpers for running multiple agents in parallel |

### Assembly skeleton
Default is `_Core`, `_UI`, `_Combat`, `_Rendering` + two test assemblies. Use §5-2's genre table to reshape it for your game.

---

## 9. Blog series that inspired this template (Korean)

Background design rationale:

1. [Designing an agent's brain](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-AI-에이전트를-실무에-쓴다)
2. [Parallel agent worktrees without Domain Reload](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-병렬-에이전트-설계)
3. [A DAP-based agent debugging environment](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-DAP-기반-에이전트-환경-만들기)

## License

- Template code: [MIT](LICENSE)
- Card graphics (used only in the warm-up): Kenney Playing Cards Pack (CC0) — see [`Assets/Art/Cards/License.txt`](Assets/Art/Cards/License.txt). Feel free to delete after the warm-up.
