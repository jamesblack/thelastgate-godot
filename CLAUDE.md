# CLAUDE.md

This file guides Claude Code when working in this repository.

## End goal

A new **Godot 4 .NET (C#)** client for **The Last Gate**, an MMORPG built on a modified Mercenaries of Astonia v2 engine. It connects to the **existing, unchanged server**; the server is not being rewritten.

Stages:
1. **Test project (done):** prove the risky parts work (zlib stream, sprite archives, isometric drawing).
2. **MVP (done):** log in, walk, fight, chat, inventory and equipment, stats, sound.
3. **Feature parity (done)** with the SDL client: shop, depot, skill tree, skill planner, item tooltips, buff watch, and the other client mods.
4. **Its own client (current stage):** the client no longer follows the SDL client's behaviour or code. The only contract is with the server.

Everything is laid out for a 1280×720 virtual resolution (`viewport_width`/`viewport_height` in `project.godot`), and the window scales it. Pixel accuracy matters at that size.

## Repository layout

| Folder | Holds |
|---|---|
| `game/` | The Godot project: `project.godot`, `TheLastGate.csproj` and Godot's own `TheLastGate.sln`. Godot can't see anything outside this folder. |
| `src/TheLastGate.Core/` | Everything a headless client needs: the protocol, the game state, and the file formats those use |
| `src/TheLastGate.Assets/` | Plain C# for the game's data folder: its layout, finding sprite overrides, and keying magenta in override files. It references nothing else here. |
| `src/TheLastGate.Rules/` | Copies of the server's formulas, for client state the server doesn't send, such as the planner's raise costs, tarot steps and buff powers. It references Core, and Core never references it. |
| `src/TheLastGate.Starter/` | The small executable Velopack installs as the app's entry point. It answers Velopack's install and update hooks without starting Godot, then starts the game. Ships with every build. |
| `tests/TheLastGate.Tests/` | xUnit tests for Core, Assets and the tools; they run without the engine |
| `tests/TheLastGate.GodotTests/` | GdUnit4 tests for code in `game/`. It is a Godot project of its own that references the game, so tests can run engine behaviour (input, nodes, scenes) as well as plain C# |
| `data/` | Game data the client reads at run time. Everything here ships. |
| `reference/server/` | A submodule of the server repo, pinned to the last server commit the client was checked against. It stays empty until the `server-changes` skill clones it. |

The root `TheLastGate.sln` holds every project and is the one to open in Rider. Godot builds `game/TheLastGate.sln`, which only lists the game, so failing tests never stop the game from running.

## Reference code (read-only from here)

**Server source:** the live server is `C:\Code\TheLastGateReal` (`ZarroTsu/TheLastGate` on GitHub). It is the only contract this client keeps: the protocol, packet lengths and what the server's data means. Protocol questions are answered from it. `/server-changes` reviews the server commits since `reference/server`'s pin for changes the client has to follow. `C:\Code\thelastgate-sdl\Source\Server` is out of date, so don't rely on it.

**SDL client:** `C:\Code\thelastgate-sdl\Source\Client` is no longer a reference for this client's behaviour or code. Don't consult or copy it unless a task says to. Features that read its files on the player's machine to migrate them (`unique.dat`, `TLGCharacters.dat`, the explored map and notes) are features of this client and stay.

**Where the game data came from:** the SDL repo's `Resources\Packaged Files\` and `Resources\Gfx Compiler\gfx\`. They were copied once into `data/` and the sprites into `game/sprites/`, and are now reference only.

## Sprites and game data (in Git LFS)

`game/sprites/` and `data/` are stored in Git LFS, so run `git lfs pull` after cloning. File names are lower case.

- `game/sprites/NNNNN.png` are the game's sprites, as ordinary Godot texture resources: 32-bit, with transparency baked in rather than keyed from magenta. `SpriteLoader` loads `res://sprites/NNNNN.png` on first use into a size-limited cache.
- They import lossless, without mipmaps and without alpha-border fixing, from the `[importer_defaults]` in `project.godot`. `SpriteImportTests` fails if a sprite's `.import` file says otherwise.
- They don't ship inside the game's main pack. Both export presets exclude `sprites/*`, and `tools/pack_sprites.gd` packs them into `data/sprites.pck`, which `SpritePack` mounts before the first sprite loads. A code-only update leaves that pack unchanged.
- Sprite overrides are loose files read at run time: `data/overrides/`, then the player's `user://overrides`, both ahead of the sprite resources. Those keep the old magenta key.
- `data/` holds everything else that ships beside the game, listed in its README.

**Adding or changing a sprite:** put the `NNNNN.png` in `game/sprites/`, open the editor or run `godot --headless --path game --import`, and commit the PNG with its `.import` file. A fresh checkout imports all ~19,000 sprites on first open, which takes a few minutes.

While the SDL client is still live, new art is also added to the SDL repo by hand. Nothing here writes to that repo.

## Build and run

```powershell
dotnet build TheLastGate.sln
dotnet test tests/TheLastGate.Tests
dotnet test tests/TheLastGate.GodotTests
sh tools/check-code-cleanup.sh
sh tools/check-godot-files.sh
powershell -ExecutionPolicy Bypass -File tools/view-sprites.ps1
godot --path game --editor
godot --path game -- --start-as=<character name>
bash tools/package-release.sh <win|linux> <version> <channel> <notes-file> <releases-dir>
```
- `dotnet test tests/TheLastGate.GodotTests` builds the game, then starts Godot to run the GdUnit4 tests (about 25 seconds). GdUnit4 finds Godot through the `GODOT_BIN` environment variable, which is set on this machine to the console executable listed below.
- `--start-as` presses Start in the launcher only if the character it has loaded has that name, so a check in game can begin without clicking through.
- `tools/package-release.sh` exports the game into `build/`, adds `data/` and the Starter, and packs it with Velopack (`vpk` 1.2.161 must be on PATH). Channels are `win` and `linux` for releases, and `test-win` and `test-linux` for test builds, which real clients never see.
- Every Release Please release is packaged by `.github/workflows/package.yml` for Windows and Linux and attached to that release. To make a test release, push a tag `updater-test-v<version>`. The version must be higher than any earlier test release and never used before, because GitHub won't recreate a tag that belonged to an immutable release (0.1.0 to 0.1.7 are used up). It builds on the `test-win` and `test-linux` channels as a prerelease, and test builds find the newest published one through GitHub's API. Releases are immutable once published, so every release stays a draft until CI has attached its files. Delete test releases afterwards with `gh release delete <tag> -y`.
- PowerShell scripts in `tools/` run with `-ExecutionPolicy Bypass`, because this machine blocks scripts by default. It applies to that one run only.
- Godot version: 4.7.2-stable .NET (mono), installed through winget. It isn't on PATH as `godot`. The console executable is `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`
- .NET SDK version: 10.0.403. Every project targets .NET 8.
- Claude may build and run tests without asking. Run only the tests that cover the changed code (`--filter`), and the full suite at the end of a finished chain of work.
- Connecting to the live game server is allowed. Its host, proxy and port are `ServerAddress.Live` in `src/TheLastGate.Core/Net/ServerAddress.cs`. Keep test sessions short, and don't send commands that change a real character's state (giving, dropping, trading or deleting items, raising stats) unless the user asks.
- Passwords are never stored, in files, settings or source. The player types theirs in the launcher each time, for the character loaded from its `.moa` file.
- After cloning, run `git config core.hooksPath .githooks` once. The pre-commit hook runs two checks on what is staged:
  - `tools/check-godot-files.sh` rejects a `.tscn` or `.tres` without a `uid` in its header and on every `ext_resource`, or with a node that has no `unique_id`. A Claude Code hook runs the same check whenever Claude writes one.
  - `tools/check-code-cleanup.sh` rejects C# that JetBrains' Full Cleanup would still change, using the settings in `.editorconfig`. It leaves its changes in the working tree to review and stage.
- A hand-written scene is fine as long as it passes that check. Never commit an editor save of `game/Hud/hud_view.tscn`: the editor bakes the HUD's bitmap fonts into it, so its nodes are written by hand.
- The editor sometimes saves `game/Hud/HudView.cs` with tab indents. Running `sh tools/check-code-cleanup.sh` turns them back into spaces.
- The Godot MCP Toolkit plugin (`game/addons/godot_mcp_toolkit/`) lets Claude read and drive the open editor and a running debug game. Its server needs Node 22 or newer and is added once with `claude mcp add godot-mcp-toolkit -s local -e GODOT_MCP_PROJECT_PATH=<absolute path to game/> -- npx -y @npgamedev/godot-mcp-server@1.1.2`. The path is needed because the Godot project isn't at the repo root. Local scope is shared by every worktree of the repo, so point it at the worktree whose editor is open. The plugin keeps itself out of exported builds, and `tools/check-godot-files.sh` skips `game/addons/`.
- `game/TheLastGate.csproj` and `game/TheLastGate.sln` are generated by the Godot editor. Don't write or change the `Godot.NET.Sdk` version by hand.
- Dependencies must not need a licence key, even a free one.

## Architecture guidelines

- **Core holds only what a headless client needs.** A headless client logs in, keeps the game state from the ticks, and sends commands, with no screen. If it wouldn't use something, that code belongs in `game/` beside the nodes that use it. Core mirrors as much client state as it can, including state it has to ask the server for, such as buff readings and item looks.
- **Testability is not a reason to put code in Core.** Engine behaviour is tested with GdUnit4 (`tests/TheLastGate.GodotTests`), which can also run plain C# tests on code in `game/` without starting Godot.
- **Protocol and file-format code is plain C#** over `ReadOnlySpan<byte>` / `BinaryPrimitives` (little-endian).
- **The game references Core, Assets and Rules, never the tools.** Code and scripts only the build needs (packaging, the sprite pack) stay in `tools/`, so they never ship to players.
- **All packet reads go through a bounds-checked `PacketReader`.** Every command type needs an exact length, because one wrong length loses the client's place in the tick stream.
- **One inflater for the whole session,** started after login succeeds. Decompressing each frame on its own fails from the second frame on.
- Sprites load on first use into a size-limited cache, from the sprite resources or an override file. Scenes may use a sprite resource directly, for UI; the world's sprites go through `SpriteLoader` so the cache budget holds.
- Networking runs off the main thread. The game state is updated once per tick on the main thread.
- **Call down, signal up.** A parent calls methods on its children and sets their properties. A child tells its parent something only through a signal or event, and never reaches up to it (`GetParent`, walking the tree for an ancestor). Core works the same way: it raises events, and the game decides what to do with them.
- The main screen recreates the classic sprite HUD. Overlay windows (options, world map, planner) are styled Godot Controls. Layout lives in scenes and containers, not in computed coordinates, and `[Tool]` scripts give runtime content a preview in the editor.
- The dark window look is drawn procedurally, not from the classic sprites. A canvas shader takes flat colours (the HUD blue, the red fill, the gold) and adds the art's grain as noise in the window's pixel grid. Frames are profiles of gold and gap rings, counted in from each box's own edges, so any box resizes cleanly. A panel's `Edges` leaves out the rings on chosen sides, so boxes that touch share one border. `game/StyleGuide/style_guide.tscn` is the reference: every shared component (panel settings, lines, the scroll bar, the text and close buttons, item cells, text colours and a window recipe), each with how to set it up and an example of every setting. Keep it up to date when a component is added or changed. New or reworked UI is built this way, not from the classic sprites or 9-slices of them. It only has to look like the art, not match it pixel for pixel.
- Scenes sit beside their scripts, named in snake_case (`game/Planner/planner_window.tscn` beside `PlannerWindowView.cs`).
- Text is drawn in a swappable font set, never a hard-wired font. Everything in game uses the HUD's `IFontSet`, which `WindowFonts` puts on every window under the HUD. The launcher has its own `ILauncherFontSet`.
- Text inputs are plain `LineEdit` and `TextEdit` nodes, so `TextFields` can keep hotkeys and chat keys away from a field while it is being typed in. Numbers are typed, not picked from drop-downs.
## Code style

`.editorconfig` holds the rules and `Directory.Build.props` makes the build enforce them, so a violation fails `dotnet build`.

- C# with the usual .NET conventions and nullable reference types enabled. Naming is an error when broken: PascalCase for types, members, constants and `static readonly` fields; camelCase for locals, parameters and private fields.
- **Namespaces follow folders**, starting from the project's root namespace (`game/Config/` is `TheLastGate.Config`). This is an error too, so the game keeps its code directly under `game/`, not in a `src/` folder.
- **`var` wherever it compiles** without changing the type. This is an error too. Keep an explicit type only for a declaration with no initializer, a collection expression, or `stackalloc`.
- **Descriptive names**, at least 3 letters. No `i`, `n`, `buf`, `out`, `tmp`. For example `tileIndex`, `payloadBytes`, `spriteNumber`.
- No warning suppressions (`#pragma warning disable`, `[SuppressMessage]`). Fix the warning, or leave it visible.
- Every `switch` over an enum ends with a default that throws `UnreachableException`.
- Pull constants out with names when the number has meaning (sprite numbers, packet sizes, layout offsets), not for every literal.

## Comments

Code should explain itself through its names, small methods and named constants. A comment is the last resort, used only for what the code can't say. The default is no comment. If something seems to need one, first try a clearer name or a clearer expression, for example `recencyOrder.Count > 1` instead of a comment saying the newest entry is never evicted.

**Write a comment only when the reason isn't visible in the code and can't be made visible:**
- a protocol or file-format quirk (`// pnglib has no length field, so each PNG runs to the next stored offset`)
- a workaround, or a trade-off a reviewer would otherwise question
- a rule that has to match something outside the code (`// 0x1F widens to 0xFF, not 0xF8, matching how SDL expands 565`)

**Don't write a comment that:**
- says what the next line does (`// Read the index`, `// Loop over the sprites`)
- repeats a name (`// The sprite cache` above `class SpriteCache`, `/// <summary>Gets the width</summary>`)
- summarises what a class or method is for, or the design around it (`// The client's own copy of the current character. Loading copies it here, saving copies it out` above `CharacterStore`). If a class needs that explained, its name or its methods should say it.
- explains a choice the code already shows (`// Paths are compared ignoring case` above code using `OrdinalIgnoreCase`)
- describes parameters or return values that the names and types already make clear
- cites where code came from ("ported from socket.c:539"). Pointers to reference files belong in this file.
- keeps old code commented out. Delete it; Git has it.

When code changes, update or delete its comments in the same change. A stale comment is worse than none.

## Working style

- Work through a numbered step in pieces: first the tests with stubs that throw `NotImplementedException`, so every commit builds; then the Core code that passes them; then the view. Snapshot each piece as it is done, and commit the pieces in order once the user has checked the result, in game when there is something to see.
- Work on a branch, use Conventional Commits, and squash-merge into `main` with player-facing changelog lines, only when the user clearly says to merge. The `committing-and-merging` skill (`.claude/skills/`) has the details. Release Please (`.github/workflows/release-please.yml`) builds the changelog from `main` and keeps a release PR open. Merging that PR tags the release and raises the version in `Directory.Build.props`; merge it only when the user says to release.
- Plans and specs from superpowers skills (`docs/superpowers/`, `.superpowers/`) stay local and are never committed. They are listed in `.gitignore`.
