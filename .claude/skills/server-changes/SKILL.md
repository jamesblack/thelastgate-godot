---
name: server-changes
description: Use when the user runs /server-changes, or asks whether recent commits to the live server (ZarroTsu/TheLastGate) need changes in the Godot client, such as protocol, packet lengths, server text the client parses, formulas copied into Rules, or new sprites and sounds.
argument-hint: "[server commit or branch to check up to; defaults to origin/master]"
allowed-tools: Bash(git *), Bash(gh *), Bash(awk *), Bash(grep *), Bash(wc *), Read, Grep, Glob
---

# Server changes

The client's only contract is with the server. This skill reads the server commits made since the last check and works out which ones the client has to follow.

`reference/server` is a submodule of the server repo. **The commit it is pinned to is the last server commit the client was checked against.** Each run reviews `pin..<end>` and finishes by moving the pin to `<end>`. The end is `$ARGUMENTS` if given, otherwise `origin/master`.

All commands run from the client repo root (`git rev-parse --show-toplevel`). `S` below is `git -C reference/server`.

## Steps

1. **Get the clone.** The submodule has `update = none`, so a fresh checkout or worktree has an empty `reference/server`. If `reference/server/.git` is missing, clone it without file contents, which keeps it at about 20 MB instead of 733 MB, and check out only the server source:
   ```bash
   pin=$(git ls-files -s reference/server | awk '{print $2}')
   git clone --filter=blob:none --no-checkout "$(git config -f .gitmodules submodule.reference/server.url)" reference/server
   git -C reference/server sparse-checkout set Source/Server
   git -C reference/server checkout --detach "$pin"
   ```
   Never run a plain `git submodule update` or a full clone: the repo is mostly sprites.
2. **Fetch and fix the range.** `S fetch origin`. Resolve the end once to a full hash (`S rev-parse <end>`) and use that hash for the rest of the run, so commits pushed during the review are left for the next run. The start is the pin (`git ls-files -s reference/server`, second field). If `S log --oneline <pin>..<end>` is empty, tell the user the client is up to date and stop.
3. **List the commits.** `S log --reverse --format='%h %ci %s' --stat <pin>..<end>`. The server's commit messages are short and often vague ("Update svr_do.c"), so judge each commit by its diff, not its message.
4. **Triage each commit** against the table below. File contents download on demand, so diffs work even though only `Source/Server` is checked out. Write every diff to one scratchpad file and read it in chunks, rather than one `S show` per commit:
   ```bash
   for hash in $(S log --reverse --format=%h <pin>..<end>); do S show "$hash" -- Source/Server; done > <scratchpad>/server-diffs.txt
   ```
   For a large diff, read the hunks that touch a row of the table and skim the rest. A diff that looks like reformatting can still hide a change: compare the removed and added `case` lines, calls and `do_char_log` strings as sets. Most commits are balance, quests and NPC behaviour that the client never sees. Those still get a line in the report.
5. **Check the client for each candidate.** Find the client code that mirrors it (the table's last column, then a case-sensitive `git grep -F '<phrase>' -- src game ':!game/addons' ':!game/sprites'` for a distinctive number, name or phrase from the hunk). Compare it with the server's new version. Some Rules code was ported after the pin, so the client may already match. Then the item is "already matches", not a change.
6. **Report** in the format below and wait for the user.
7. **Offer issues.** Ask which items should become GitHub issues. Before creating one, search for a duplicate (`gh issue list --state all --search "<key words>"`). Create each picked item with `gh issue create --title "<summary>" --body-file <scratchpad file>`. The body names the server commit(s) (`ZarroTsu/TheLastGate@<hash>`), the server file and function, what changed, the client file(s) to change, and how to check the change in game. Create nothing the user didn't pick.
8. **Move the pin** once the user agrees the review is done. Follow the `committing-and-merging` skill for the branch. `S checkout --detach <end hash>`, `git add reference/server`, and commit `chore(reference): check server changes up to <short hash>`. If the user wants to stop partway, move the pin only to the last commit that was fully reviewed.

## What the client follows

Server paths are under `Source/Server/`. Client paths are from the client repo root.

| Server change | What to look for in the diff | Client code |
|---|---|---|
| **Protocol** (`client.h`, `svr_tick.c`, `server.c`) | New, renamed or renumbered `SV_*` or `CL_*` values. A changed packet length or byte layout. New fields sent in a tick. Login or challenge changes. | `src/TheLastGate.Core/Net/`: `ServerCommand.cs`, `ServerCommandLength.cs`, `ClientCommand.cs`, the `*Packets.cs` files and `Handlers/` |
| **Packets sent from game code** (`svr_do.c`, `use_driver.c`, `build.c`, `driver.c`) | Any hunk that uses `SV_*` or builds a buffer to send | Same as Protocol |
| **Text the client parses** (`do_char_log` calls, mostly in `svr_do.c`, `look_driver.c`, `skill_driver.c`) | Changed wording of a message the client matches: the buff list, look and compare text, contracts, item mods, the flood reply, shop and depot lines | `src/TheLastGate.Core/Buffs/`, `src/TheLastGate.Core/Items/` (`ItemDetailsParser.cs`, `LookCapture.cs`, `ItemLooks.cs`), and `git grep -F` for the old phrase anywhere in `src/` and `game/` |
| **Formulas copied into Rules** (`svr_do.c`, mostly `really_update_char`, plus `skill.c` and `svr_effect.c`) | Raise costs (`attrib_needed`, `hp_needed`, `mana_needed`, `skill_needed`). Buff and debuff powers. Tarot cards. Skill tree nodes and their text. Item effect tables. Combat values (hit, parry, weapon and armour value). | Every file in `src/TheLastGate.Rules/`. `TarotMath.cs` models only the cards in its `TarotCard` enum, and the planner models no crit, speed or companions, so a change to anything else goes under "not settled", not under a fix. |
| **Tables Core keeps a copy of** (`area.c`, `skill.c`, `svr_do.c`, `data.h`) | Waypoint names and descriptions (`area.c` `waypoint[]`). Area names. Skill names, help text and skill tree layout. Rank names. | `src/TheLastGate.Core/State/`: `Waypoints.cs`, `WorldMap.cs`, `SkillText.cs`, `SkillTrees.cs`, `SkillTreeNode.cs`, `Ranks.cs`, `Terminology.cs`. Grep the old text when unsure. |
| **Tables and limits** (`data.h`, `gendefs.h`, `numbers.h`, `server.h`) | New skills, classes, races, seasons or game modes. A changed skill index. A changed maximum or array size the client sizes something by. | `src/TheLastGate.Core/Characters/`, `src/TheLastGate.Rules/SkillGroups.cs` |
| **New art or sound** (outside `Source/Server`: `Resources/Gfx Compiler/gfx/`, `Resources/Packaged Files/sfx/`) | `S diff --stat <pin> <end> -- Resources` lists added or changed files. Don't read their contents. | `game/sprites/NNNNN.png` (see "Adding or changing a sprite" in `CLAUDE.md`); a character's animation frames go onto its sheet in `game/sprites/characters/` (see "Adding character frames"); `data/sounds/` |
| **Everything else** (NPC drivers, quests, area logic, `populate.c`, `talk.c`, balance numbers) | Nothing, unless a hunk also matches a row above | None |

`Source/Client` in the server repo is the old client. Its changes are not part of the contract: ignore them.

## Report

Group the commits by what the client has to do, most urgent first. Leave out a heading that has no commits. "Already matches" is for a change the client mirrors and already has. A change the client sees but deliberately ignores, such as a new `#buffs` line its parser skips, goes under "Nothing to do".

```markdown
## Server changes <pin short>..<end short> (<N> commits, <first date> to <last date>)

### Breaks the client
<Protocol and packet changes. The client loses its place in the tick stream or sends a bad command.>
- `<hash>` <server subject>: <what changed, server file:function> → <client file to change>

### Client shows something wrong
<Parsed text, Rules formulas and tables that no longer match.>
- `<hash>` ...

### New art or sound
- `<hash>` <sprite or sound numbers>

### Already matches
- `<hash>` <server subject>: <client file that already has it>

### Nothing to do
- `<hash>` <server subject>: <three or four words on why, e.g. "NPC dialogue only">
```

After the report, list anything you could not settle from the code: a hunk whose effect on the client you're unsure of, a formula the client might mirror but you found no match for, a change to something the client deliberately doesn't model, and client text (launcher, help, tooltips) that the change may have made untrue.

## Common mistakes

- **Trusting the commit message.** "Compile Error" or "Update svr_do.c" can hide a formula change. Read the diff.
- **Reporting a change the client already has.** Compare with the client code first (step 5).
- **Moving the pin past what was reviewed.** Use the end hash fixed in step 2, not `origin/master` at commit time.
- **Consulting `C:\Code\thelastgate-sdl`.** The SDL client and its copy of the server are out of date. The submodule is the server.
