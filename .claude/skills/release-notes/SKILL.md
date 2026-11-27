---
name: release-notes
description: Use when the user runs /release-notes, is about to merge the Release Please release PR, or asks for player-facing release notes, launcher update notes, a Discord changelog or an announcement post for a release of the Godot client.
argument-hint: "[release PR number; defaults to the open Release Please PR]"
allowed-tools: Bash(gh *), Bash(git *), Bash(powershell *), Bash(grep *), Bash(wc *), Read, Grep, Glob
---

# Release notes

Release PR to rewrite: `$ARGUMENTS`. If empty, use the open one: `gh pr list --label "autorelease: pending" --json number,title -q '.[0]'`. If there is none, stop and tell the user.

Release Please writes the release PR's description from the commit lines, which are written for developers. When the PR merges, that description becomes the GitHub release body. `package.yml` copies the body into the Velopack feed, and the launcher shows it to players before they update. The release is immutable once published, so the player-facing text has to be in the description **before the merge**. `CHANGELOG.md` in the PR keeps the developer lines. Leave it alone.

## Steps

All commands run from the repo root (`git rev-parse --show-toplevel`).

1. **Fetch.** `git fetch origin --tags`. Read the PR: `gh pr view <number> --json title,body,headRefOid`. The version is the end of the title (`chore(main): release 1.2.0`). The previous release is the highest tag: `git tag --list 'v*' --sort=-v:refname | head -1`.
2. **Read the changes.** `git log --format='%h%n%B%n----' <prev tag>..origin/main`. Each squash commit can carry several `type(scope): summary` entry lines at the bottom of its body. For anything vague, read the commit (`git show --stat <hash>`, then the relevant diff) to find out:
   - what the player notices, and where (which window, the launcher, which Options tab)
   - defaults: whether it is on or off, and whether a key is bound by default (and which)
   - whether it brings over a feature of the SDL client (`C:\Code\thelastgate-sdl\Source\Client`)
   - whether the diff changes something a player would notice that no entry line mentions. Include it, and flag it in step 6.
3. **Drop what players never see:** hidden types (`refactor`, `test`, `docs`, `build`, `ci`, `chore`, `style`), internal names, and fixes to bugs that never reached a player. A fix is for a shipped bug only if the broken code is already in the previous release (`git show <prev tag>:<file>`).
4. **Merge** entries that are parts of one feature into a single line. Name areas after what the player sees (`Chat`, `Stats`, `World map`), not after the commit scope.
5. **Write the notes** in the format below, to a file in the scratchpad.
6. **Show the notes** to the user in a fenced code block. List outside the block anything you could not verify from the code (a behaviour claim, a default, a key name). Wait for the user's OK or edits.
7. **Update the PR** only after the OK. Keep the description's header and footer, and replace only what lies between its first and last `---` lines:
   - Write the full new description to a scratchpad file.
   - Check that `gh pr view <number> --json headRefOid` still matches step 1. If it changed, Release Please has regenerated the description, so start again from step 1.
   - `gh pr edit <number> --body-file <file>`.
8. **Copy the Discord post** to the clipboard: the line `CLIENT UPDATE <short version> - you can now update by clicking the Update button in the launcher`, then the notes from step 5 without their `## [version]` heading and with no blank line between. The short version is `1.2` for `1.2.0`, and keeps the patch only when it is non-zero (`1.2.1`). The post has no link: players update from the launcher, not from GitHub. Copy with `powershell -NoProfile -Command "Set-Clipboard -Value (Get-Content -Raw -Encoding UTF8 '<file>')"`, and give the character count, which must stay under Discord's 2000-character limit. Do not post it anywhere.
9. **Remind the user** to merge the release PR before anything else lands on `main`. Any new commit on `main` makes Release Please regenerate the description, and then this skill has to run again.

## Format

The launcher's parser (`ReleaseNotes.Parse` in `src/TheLastGate.Core/Updates/`) reads only these lines:

| Line | Meaning |
|---|---|
| `## [1.2.0](<compare url>) (2026-11-20)` | The version heading. Release Please needs it to find the version, and the launcher shows its date. Keep Release Please's line, but set the date to today, since the merge follows straight after. |
| `### World map` | A feature area, shown in bold |
| `* Zoom with the mouse wheel` | One change, shown as a bullet |

Every other line is ignored, so everything a player should read goes in a bullet.

- **One to five changes:** bullets straight under the version heading, with no area headings.
- **More:** one `### ` heading per feature area (`### World map`, `### Launcher`, `### HUD`), with areas ordered by how much players will care and a `### Other` area last.
- **SDL client ports:** end the bullet with `(as in the SDL client)`. The parser has no nested bullets, so a lead-in bullet with the features under it would read as one more change.
- Plain text only inside bullets. The launcher shows `**`, backticks and links as literal characters. Use ASCII punctuation (`Options > General`, `-`, `'`), because the launcher's pixel font may not have arrows or curly quotes.

## Voice

- Player-facing and casual. Describe what you can now do, not what the code does: "Zoom with the mouse wheel", not "Add scroll handler to minimap".
- Tell players how to reach a feature when it is not obvious: the menu path (`Options > General`), the default key, or that it has no key bound by default. When an option has no short name, use a few words of its visible label.
- Say when something is off by default or experimental.
- Keep each bullet to one line. Short is better.
- Honest hedges are fine when a fix is uncertain ("Fixes the hotbar losing its keys after a reconnect (hopefully)").
- This is a solo project: write as one developer, "I" and "me", never "we" or "us". Asks for feedback say "let me know".
- Be facetious rather than dry: lead with the funny, player-side view of a change and make light jokes of it. "Stubborn use no longer gets confused by an enemy spawning on a pentagram it just activated" beats "Stubborn use no longer retries an activated pentagram". Don't invent jokes about behaviour you didn't verify.

## Example

```
## [1.2.0](https://github.com/jamesblack/thelastgate-godot/compare/v1.1.0...v1.2.0) (2026-11-20)

### World map
* Zoom, pan, and a category list to show or hide what's drawn
* Add your own pins, text and paths (bind a key for the map in Options > Keybindings, it has none by default)
### Launcher
* Open the data and sprite overrides folders from the launcher
### Other
* Hotbar menu text stays sharp and bright
* A recalled chat line sends with one Enter
```
