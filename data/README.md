# Game data

Everything in this folder ships with the client. The sprites themselves live in `game/sprites/` and ship as `sprites.pck`, which packaging writes into this folder.

It is stored in Git LFS, so run `git lfs pull` after cloning. The client reads it directly at run time.

| Folder | Holds |
|---|---|
| `overrides/` | Loose sprites drawn instead of the sprite with the same number. A player's own sprites go in `user://overrides` and win over these. |
| `ui/` | UI images |
| `sounds/` | `.wav` sound effects |

File names are lower case so they resolve the same way on Windows and Linux.
