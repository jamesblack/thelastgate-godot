#!/bin/sh
# Runs pack_sprites.gd in a scratch Godot project and checks it refuses sheets without manifests, which the client
# needs to find any art sprite.
# Usage: sh tools/test_pack_sprites.sh   (with GODOT_BIN set to the Godot executable)

pack_script="$(cd "$(dirname "$0")" && pwd)/pack_sprites.gd"
scratch=$(mktemp -d)
trap 'rm -rf "$scratch"' EXIT
failures=0

fail() {
    printf 'FAIL: %s\n' "$1"
    failures=$((failures + 1))
}

pack() {
    rm -f "$scratch/sprites.pck"
    output=$(timeout 120 "$GODOT_BIN" --headless --path "$scratch/project" --script "$pack_script" \
        -- "$scratch/sprites.pck" 2>&1)
}

mkdir -p "$scratch/project/sprites" "$scratch/project/.godot/imported"
printf 'config_version=5\n' > "$scratch/project/project.godot"
printf 'texture bytes' > "$scratch/project/.godot/imported/floors.ctex"
printf '[remap]\n\nimporter="texture"\ntype="CompressedTexture2D"\npath="res://.godot/imported/floors.ctex"\n' \
    > "$scratch/project/sprites/floors.png.import"

pack
[ $? -ne 0 ] || fail "no_manifests: exit 0"
case "$output" in
    *"no art sheet manifests"*) ;;
    *) fail "no_manifests: no message in: $output" ;;
esac
[ ! -f "$scratch/sprites.pck" ] || fail "no_manifests: wrote a pack"

printf '{"sheet_sha256": "", "sprites": {}}\n' > "$scratch/project/sprites/floors.json"
pack
[ $? -eq 0 ] || fail "with_manifest: exit not 0: $output"
[ -f "$scratch/sprites.pck" ] || fail "with_manifest: no pack"

[ "$failures" -eq 0 ] && echo "all checks passed" || echo "$failures checks failed"
[ "$failures" -eq 0 ]
