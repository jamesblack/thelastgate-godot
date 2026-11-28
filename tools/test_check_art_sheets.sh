#!/bin/sh
# Runs check-art-sheets.sh in a scratch repository and checks what it accepts and refuses.
# Usage: sh tools/test_check_art_sheets.sh

check_script="$(cd "$(dirname "$0")" && pwd)/check-art-sheets.sh"
scratch=$(mktemp -d)
failures=0
trap 'rm -rf "$scratch"' EXIT

fail() {
    printf 'FAIL: %s\n' "$1"
    failures=$((failures + 1))
}

expect() {
    expected_exit=$1
    expected_text=$2
    case_name=$3
    shift 3
    output=$(cd "$scratch" && CHECK_ART_TRACE=1 sh "$check_script" "$@" 2>&1)
    actual_exit=$?
    [ "$actual_exit" -eq "$expected_exit" ] || fail "$case_name: exit $actual_exit, expected $expected_exit: $output"
    case "$output" in
        *"$expected_text"*) ;;
        *) fail "$case_name: no \"$expected_text\" in: $output" ;;
    esac
}

hash_of() {
    sha256sum "$scratch/$1" | cut -d' ' -f1
}

pointer() {
    printf 'version https://git-lfs.github.com/spec/v1\noid sha256:%s\nsize 1234\n' "$2" > "$scratch/$1"
}

manifest() {
    cat > "$scratch/game/sprites/floors.json" <<EOF
{
  "sheet_sha256": "$1",
  "sprites": {
    "00116": {
      "h": 32,
      "source_sha256": "$2",
      "used": {
        "h": 15,
        "w": 32,
        "x": 0,
        "y": 17
      },
      "w": 32,
      "x": 1,
      "y": 1
    },
    "00117": {
      "h": 32,
      "source_sha256": "$3",
      "used": {
        "h": 15,
        "w": 32,
        "x": 0,
        "y": 17
      },
      "w": 32,
      "x": 35,
      "y": 1
    }
  }
}
EOF
}

lfs_art=aaaa111111111111111111111111111111111111111111111111111111111111
lfs_sheet=bbbb222222222222222222222222222222222222222222222222222222222222

cd "$scratch" || exit 1
git init -q .
git config user.email test@example.invalid
git config user.name test
git config core.autocrlf false
mkdir -p art/floors game/sprites/characters
pointer art/floors/00116.png "$lfs_art"
printf 'plain png bytes\001\002' > art/floors/00117.png
pointer game/sprites/floors.png "$lfs_sheet"
manifest "$lfs_sheet" "$lfs_art" "$(hash_of art/floors/00117.png)"
printf 'readme\n' > README.md
git add -A
cd - > /dev/null || exit 1

expect 0 "" "clean_staged" --staged

printf 'changed bytes\003' > "$scratch/art/floors/00117.png"
(cd "$scratch" && git add art/floors/00117.png)
expect 1 "art/floors/00117.png: game/sprites/floors.png is out of date" "changed_art" --staged
manifest "$lfs_sheet" "$lfs_art" "$(hash_of art/floors/00117.png)"
(cd "$scratch" && git add game/sprites/floors.json)
expect 0 "" "rebuilt_manifest" --staged

(cd "$scratch" && git rm -q --cached art/floors/00116.png)
expect 1 "game/sprites/floors.json: 00116 has no source in art/floors/" "deleted_art" --staged
(cd "$scratch" && git add art/floors/00116.png)

pointer game/sprites/floors.png cccc333333333333333333333333333333333333333333333333333333333333
(cd "$scratch" && git add game/sprites/floors.png)
expect 1 "game/sprites/floors.png doesn't match game/sprites/floors.json" "changed_sheet" --staged
pointer game/sprites/floors.png "$lfs_sheet"
(cd "$scratch" && git add game/sprites/floors.png)

mkdir -p "$scratch/art/walls"
printf 'wall\n' > "$scratch/art/walls/00900.png"
(cd "$scratch" && git add art/walls/00900.png)
expect 1 "art/walls/00900.png: game/sprites/walls.png is out of date" "group_without_sheet" --staged
(cd "$scratch" && git rm -q --cached art/walls/00900.png && rm -r art/walls)

for odd in "new floors/00500.png" "cafÃ©/00501.png" "floors/00502.PNG" "floors/notes.txt"; do
    odd_path="art/$(printf "$odd")"
    mkdir -p "$scratch/$(dirname "$odd_path")"
    printf 'odd
' > "$scratch/$odd_path"
    (cd "$scratch" && git add "$odd_path")
    expect 1 "$odd_path: isn't art/<group>/NNNNN.png" "odd_name $odd_path" --staged
    (cd "$scratch" && git rm -q --cached "$odd_path" && rm "$odd_path")
done
rmdir "$scratch/art/new floors" "$scratch/art/$(printf 'cafÃ©')"
printf '# art
' > "$scratch/art/README.md"
(cd "$scratch" && git add art/README.md)
expect 0 "" "readme_allowed" --staged

(cd "$scratch" && git commit -q -m base --no-verify)
printf 'changed\n' > "$scratch/README.md"
(cd "$scratch" && git add README.md)
expect 0 "skipped: nothing staged under art/ or game/sprites/" "unrelated_change" --staged

printf 'character frame\n' > "$scratch/game/sprites/characters/02000-knight.png"
(cd "$scratch" && git add game/sprites/characters/02000-knight.png)
expect 0 "skipped: nothing staged under art/ or game/sprites/" "character_sheet_change" --staged

printf 'plain sheet\n' > "$scratch/game/sprites/floors.png"
printf 'plain art\n' > "$scratch/art/floors/00116.png"
manifest "$(hash_of game/sprites/floors.png)" "$(hash_of art/floors/00116.png)" "$(hash_of art/floors/00117.png)"
expect 0 "" "clean_working_tree"
printf 'edited\n' > "$scratch/art/floors/00116.png"
expect 1 "art/floors/00116.png: game/sprites/floors.png is out of date" "changed_working_tree"

printf 'plain sheet
' > "$scratch/game/sprites/floors.png"
printf 'plain art
' > "$scratch/art/floors/00116.png"
manifest "$(hash_of game/sprites/floors.png)" "$(hash_of art/floors/00116.png)" "$(hash_of art/floors/00117.png)"
(cd "$scratch" && git add -A)
expect 0 "" "staged_without_any_lfs_pointer" --staged

[ "$failures" -eq 0 ] && echo "all checks passed" || echo "$failures checks failed"
[ "$failures" -eq 0 ]
