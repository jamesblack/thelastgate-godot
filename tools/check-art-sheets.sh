#!/bin/sh
# Rejects art sheets that are out of date with art/: every art/<group>/NNNNN.png must be in game/sprites/<group>.json
# with its SHA-256, every manifest entry must have its source, and every sheet must have its manifest's sheet_sha256.
# Staged files are hashed from their Git LFS pointers, whose oid is the file's SHA-256, so no image is read; a file
# that isn't in LFS is hashed from its content.
#
# Usage: tools/check-art-sheets.sh --staged   check what is staged (the pre-commit hook)
#        tools/check-art-sheets.sh            check the working tree

export LC_ALL=C
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

trace() {
    [ -n "$CHECK_ART_TRACE" ] && printf '%s\n' "$1"
}

# Reads "path" lines and writes "path sha256" lines for the staged copies.
hash_staged() {
    awk '{ print ":" $0 " " $0 }' > "$work/wanted"
    git cat-file --batch-check='%(objectname) %(objectsize) %(rest)' < "$work/wanted" \
        | awk '$2 ~ /^[0-9]+$/' > "$work/objects"
    awk '$2 < 1024 { print $1 }' "$work/objects" | git cat-file --batch \
        | awk '/^[0-9a-f]+ blob [0-9]+$/ { object = $1; next }
               /^oid sha256:[0-9a-f]+$/ { print object, substr($2, 8) }' > "$work/pointers"
    awk 'FILENAME == ARGV[1] { lfs[$1] = $2; next } { print $1, $3, ($1 in lfs) ? lfs[$1] : "" }' \
        "$work/pointers" "$work/objects" \
        | while read -r object path hash; do
            [ -n "$hash" ] || hash=$(git cat-file blob "$object" | sha256sum | cut -d' ' -f1)
            printf '%s %s\n' "$path" "$hash"
        done
}

# Reads "path" lines and writes "path sha256" lines for the working-tree copies.
hash_working() {
    while read -r path; do
        [ -f "$path" ] && printf '%s\n' "$path"
    done | xargs -r sha256sum | awk '{ sub(/^\*/, "", $2); print $2, $1 }'
}

show_manifest() {
    if [ "$mode" = staged ]; then git show ":$1"; else cat "$1"; fi
}

if [ "$1" = "--staged" ]; then
    mode=staged
    if ! git diff --cached --name-only -- art game/sprites | grep -v '^game/sprites/characters/' | grep -q .; then
        trace "skipped: nothing staged under art/ or game/sprites/"
        exit 0
    fi
    git -c core.quotePath=false ls-files --cached -- art > "$work/art_files"
    git ls-files --cached -- 'game/sprites/*.json' | grep '^game/sprites/[^/]*\.json$' > "$work/manifests"
else
    mode=working
    find art -type f 2> /dev/null | sort > "$work/art_files"
    find game/sprites -maxdepth 1 -type f -name '*.json' 2> /dev/null | sort > "$work/manifests"
fi

sprite_path='^art/[a-z0-9]+(-[a-z0-9]+)*/[0-9]{5}\.png$'
grep -E "$sprite_path" "$work/art_files" > "$work/art_paths"
grep -vE "$sprite_path" "$work/art_files" | grep -vx 'art/README.md' \
    | sed "s|\$|: isn't art/<group>/NNNNN.png; see art/README.md|" > "$work/bad_names"

: > "$work/expected"
while read -r manifest; do
    group=$(basename "$manifest" .json)
    show_manifest "$manifest" | awk -v group="$group" '
        /^  "sheet_sha256": "/ { split($0, part, "\""); print "sheet", group, part[4] }
        /^    "[0-9][0-9][0-9][0-9][0-9]": \{$/ { split($0, part, "\""); number = part[2] }
        /^      "source_sha256": "/ { split($0, part, "\""); print "sprite", group, number, part[4] }' >> "$work/expected"
    printf 'game/sprites/%s.png\n' "$group" >> "$work/sheet_paths"
done < "$work/manifests"
touch "$work/sheet_paths"

if [ "$mode" = staged ]; then
    hash_staged < "$work/art_paths" > "$work/art_hashes"
    hash_staged < "$work/sheet_paths" > "$work/sheet_hashes"
else
    hash_working < "$work/art_paths" > "$work/art_hashes"
    hash_working < "$work/sheet_paths" > "$work/sheet_hashes"
fi

found=$(awk '
    FILENAME == ARGV[1] && $1 == "sprite" { expected[$2 "/" $3] = $4; next }
    FILENAME == ARGV[1] && $1 == "sheet" { sheet[$2] = $3; next }
    FILENAME == ARGV[2] {
        split($1, part, "/")
        group = part[2]
        number = substr(part[3], 1, 5)
        seen[group "/" number] = 1
        if (expected[group "/" number] != $2) {
            printf "%s: game/sprites/%s.png is out of date; run tools/build_art_sheets.gd\n", $1, group
        }
        next
    }
    {
        group = $1
        sub(/^game\/sprites\//, "", group)
        sub(/\.png$/, "", group)
        actual[group] = $2
    }
    END {
        for (key in expected) {
            if (!(key in seen)) {
                split(key, part, "/")
                printf "game/sprites/%s.json: %s has no source in art/%s/\n", part[1], part[2], part[1]
            }
        }
        for (group in sheet) {
            if (actual[group] != sheet[group]) {
                printf "game/sprites/%s.png doesn'"'"'t match game/sprites/%s.json; run tools/build_art_sheets.gd\n", group, group
            }
        }
    }' "$work/expected" "$work/art_hashes" "$work/sheet_hashes" | sort | cat "$work/bad_names" -)

if [ -n "$found" ]; then
    printf '%s\n' "$found"
    exit 1
fi
