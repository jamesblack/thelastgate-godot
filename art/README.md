# Art

The source art for every sprite that isn't a character animation frame. It sits outside the Godot project, so the
editor never scans it: the game loads these sprites from the sheets built out of this folder.

Each folder is a group, and each group becomes one sheet, `game/sprites/<group>.png`, with its manifest,
`game/sprites/<group>.json`, which gives every sprite's place on the sheet.

- Sprites are `art/<group>/NNNNN.png`, `NNNNN` being the sprite number. A number is in one group only.
- Group names are lower case words joined by `-` (`floors`, `unsorted-32x32`). `characters` is reserved, and a name
  can't be only digits.
- Character frames don't go here: they live on the character sheets in `game/sprites/characters/`.

## Adding, changing or regrouping a sprite

1. Put the PNG in its group's folder, or move it to another folder.
2. Run `godot --headless --path game --script ../tools/build_art_sheets.gd`. It rebuilds every sheet and checks each
   one against its sources.
3. Run `godot --headless --path game --import`, or open the editor, so a new sheet gets its `.import` file.
4. Commit the art with its sheets and manifests. The pre-commit hook refuses a commit whose sheets are out of date.

`tools/test_art_sheets.gd` tests the tool, run the same way.
