#!/bin/sh
# Runs build_classic_fonts.gd against an art folder without the font sprites and checks it refuses, writing nothing.
# Usage: sh tools/test_build_classic_fonts.sh   (with GODOT_BIN set to the Godot executable)

repo="$(cd "$(dirname "$0")/.." && pwd)"
scratch=$(mktemp -d)
trap 'rm -rf "$scratch"' EXIT
mkdir -p "$scratch/art/interface"

before=$(cd "$repo" && sha256sum game/fonts/classic/*.tres game/themes/HudTextTheme.tres)
output=$(cd "$repo" && timeout 120 "$GODOT_BIN" --headless --path game --script ../tools/build_classic_fonts.gd \
    -- --art "$scratch/art" 2>&1)
status=$?
after=$(cd "$repo" && sha256sum game/fonts/classic/*.tres game/themes/HudTextTheme.tres)

failures=0
[ "$status" -ne 0 ] || { echo "FAIL: exit 0 with no font sprites"; failures=$((failures + 1)); }
case "$output" in
    *"sprite 18100 isn't in any art/ folder"*) ;;
    *) echo "FAIL: no message naming sprite 18100 in: $output"; failures=$((failures + 1)) ;;
esac
[ "$before" = "$after" ] || { echo "FAIL: fonts or theme were rewritten"; failures=$((failures + 1)); }

[ "$failures" -eq 0 ] && echo "all checks passed" || echo "$failures checks failed"
[ "$failures" -eq 0 ]
