#!/bin/sh
# Rejects a loose sprite (game/sprites/NNNNN.png) whose number falls on a character sheet
# (game/sprites/characters/NNNNN-<name>.png covers its base to base + 447), so no frame is in both places.
#
# Usage: tools/check-sprite-sheets.sh --staged     check the staged files (the pre-commit hook)
#        tools/check-sprite-sheets.sh [files...]   check these files, or every tracked one

SLOTS=448

case "$1" in
    --staged)
        sheets=$(git ls-files --cached -- 'game/sprites/characters/*.png')
        loose=$(git diff --cached --name-only --diff-filter=ACMR -- 'game/sprites/*.png')
        ;;
    *)
        sheets=$(git ls-files -- 'game/sprites/characters/*.png')
        if [ $# -gt 0 ]; then loose=$(printf '%s\n' "$@"); else loose=$(git ls-files -- 'game/sprites/*.png'); fi
        ;;
esac

found=$(printf '%s\n' "$loose" | awk -v slots="$SLOTS" -v sheets="$sheets" '
    BEGIN {
        count = split(sheets, sheet_paths, "\n")
        for (index_ = 1; index_ <= count; index_++) {
            name = sheet_paths[index_]
            sub(/.*\//, "", name)
            if (name ~ /^[0-9][0-9][0-9][0-9][0-9]-/) {
                bases[name] = substr(name, 1, 5) + 0
            }
        }
    }
    $0 ~ /^game\/sprites\/[0-9][0-9][0-9][0-9][0-9]\.png$/ {
        number = substr($0, 14, 5) + 0
        for (name in bases) {
            if (number >= bases[name] && number < bases[name] + slots) {
                printf "%s: frame %d of %s; fold it in with tools/build_character_sheets.gd\n", $0, number - bases[name], name
            }
        }
    }')

if [ -n "$found" ]; then
    printf '%s\n' "$found"
    exit 1
fi
