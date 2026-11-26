# Fonts

Fonts players can choose for the game's text in Options → Display. The current client's bitmap font is built in and stays the default; it isn't listed here. Captions (the names on the gear and smith slots) always use Silkscreen, which is built into the game, as are the launcher's fonts in `game/fonts/`.

Each font ships with its licence beside it as `<name>-ofl.txt`.

| File | Font | Licence |
|---|---|---|
| `dogicapixel-regular.ttf` | Dogica Pixel, by Roberto Mocci | SIL Open Font License 1.1 |

## `fonts.json`

The list the options window offers, in the order shown. Each entry has:

| Field | Meaning |
|---|---|
| `id` | The name saved in `settings.cfg`. Don't change it once released, or players fall back to the classic font. `classic` is taken. |
| `name` | The name players see |
| `file` | The font file in this folder |
| `size` | Size in 1280×720 pixels; the line is fitted to the classic 9 px line |
| `offset` | Pixels to move the text down (negative moves it up) |
| `smooth` | `true` smooths the letters' edges; `false` keeps pixel fonts crisp |

Adding a font: put the `.ttf` and its licence here with lower-case names, add an entry, and run `dotnet test tests/TheLastGate.Tests --filter ShippedFontsTests`.
