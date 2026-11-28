# Builds each art/<group>/ folder of NNNNN.png sprites into one sheet, game/sprites/<group>.png, and its manifest,
# <group>.json, which gives every sprite's rectangle on the sheet, its drawn area and its source's SHA-256.
# Sprites are packed in rows, tallest first, each inside a 1-pixel border that repeats its own edge pixels, so a draw
# at a fractional scale or position never samples a neighbour. Nothing is written unless every sprite is valid.
# Usage: godot --headless --path game --script ../tools/build_art_sheets.gd -- [--art <folder>] [--out <folder>]
extends SceneTree

const BORDER := 1
const MIN_SHEET_WIDTH := 256
const HEIGHT_STEP := 32
const CHARACTER_SLOTS := 448
const RESERVED_GROUP := "characters"

var group_pattern := RegEx.create_from_string("^[a-z0-9]+(?:-[a-z0-9]+)*$")
var digits_pattern := RegEx.create_from_string("^\\d+$")
var sprite_pattern := RegEx.create_from_string("^(\\d{5})\\.png$")
var character_sheet_pattern := RegEx.create_from_string("^(\\d{5})-[a-z0-9-]+\\.png$")
var errors: PackedStringArray = []


class Sprite:
	var number: String
	var path: String
	var image: Image
	var position := Vector2i.ZERO

	func _init(sprite_number: String, sprite_path: String, sprite_image: Image) -> void:
		number = sprite_number
		path = sprite_path
		image = sprite_image

	func size() -> Vector2i:
		return image.get_size()

	func footprint() -> Vector2i:
		return image.get_size() + Vector2i(2 * BORDER, 2 * BORDER)

	func region() -> Rect2i:
		return Rect2i(position + Vector2i(BORDER, BORDER), image.get_size())


func _init() -> void:
	var arguments := OS.get_cmdline_user_args()
	var art := ProjectSettings.globalize_path("res://").path_join("../art").simplify_path()
	var out := ProjectSettings.globalize_path("res://sprites")
	var index := 0
	while index < arguments.size():
		match arguments[index]:
			"--art" when index + 1 < arguments.size():
				art = arguments[index + 1]
				index += 2
			"--out" when index + 1 < arguments.size():
				out = arguments[index + 1]
				index += 2
			_:
				push_error("usage: --script build_art_sheets.gd -- [--art <folder>] [--out <folder>]")
				quit(2)
				return

	art = art.trim_suffix("/") + "/"
	out = out.trim_suffix("/") + "/"
	var groups := read_groups(art, character_ranges(out + RESERVED_GROUP + "/"))
	if not errors.is_empty():
		for error in errors:
			push_error(error)
		quit(1)
		return

	var sprite_count := 0
	for group in groups:
		if not write_group(out, group, groups[group]):
			for error in errors:
				push_error(error)
			quit(1)
			return
		sprite_count += groups[group].size()

	remove_stale_groups(out, groups)
	print("built %d sheets holding %d sprites" % [groups.size(), sprite_count])
	quit(0)


func character_ranges(folder: String) -> Dictionary:
	var ranges := {}
	if not DirAccess.dir_exists_absolute(folder):
		return ranges
	for file in DirAccess.get_files_at(folder):
		var found := character_sheet_pattern.search(file)
		if found != null:
			ranges[file] = int(found.get_string(1))
	return ranges


func read_groups(art: String, characters: Dictionary) -> Dictionary:
	var groups := {}
	var owner := {}
	var folders := Array(DirAccess.get_directories_at(art))
	folders.sort()
	for group: String in folders:
		if group == RESERVED_GROUP:
			errors.append("%s is reserved for the character sheets" % group)
			continue
		if group_pattern.search(group) == null or digits_pattern.search(group) != null:
			errors.append("%s isn't a group name (lower case words joined by -, not only digits)" % group)
			continue
		var sprites: Array[Sprite] = []
		var files := Array(DirAccess.get_files_at(art + group))
		files.sort()
		for file: String in files:
			if file.get_extension().to_lower() != "png":
				continue
			var found := sprite_pattern.search(file)
			if found == null:
				errors.append("art/%s/%s isn't NNNNN.png" % [group, file])
				continue
			var number := found.get_string(1)
			if owner.has(number):
				errors.append("%s is in %s and %s" % [number, owner[number], group])
				continue
			owner[number] = group
			for sheet: String in characters:
				var base: int = characters[sheet]
				if int(number) >= base and int(number) < base + CHARACTER_SLOTS:
					errors.append("%s falls on %s; fold it in with build_character_sheets.gd" % [number, sheet])
			var path := art + group + "/" + file
			var image := Image.new()
			if image.load(path) != OK:
				errors.append("art/%s/%s won't decode" % [group, file])
				continue
			image.convert(Image.FORMAT_RGBA8)
			sprites.append(Sprite.new(number, path, image))
		if not sprites.is_empty():
			groups[group] = sprites
	return groups


func write_group(out: String, group: String, sprites: Array[Sprite]) -> bool:
	var sheet := pack(sprites)
	var sheet_path := out + group + ".png"
	if sheet.save_png(sheet_path) != OK:
		errors.append("can't write %s" % sheet_path)
		return false

	var written := Image.load_from_file(sheet_path)
	written.convert(Image.FORMAT_RGBA8)
	for sprite in sprites:
		if not matches(written, sprite):
			errors.append("%s: sprite %s differs from its source after saving" % [sheet_path, sprite.number])
			DirAccess.remove_absolute(sheet_path)
			return false

	var entries := {}
	for sprite in sprites:
		var region := sprite.region()
		var used := sprite.image.get_used_rect()
		entries[sprite.number] = {
			"x": region.position.x, "y": region.position.y, "w": region.size.x, "h": region.size.y,
			"used": {"x": used.position.x, "y": used.position.y, "w": used.size.x, "h": used.size.y},
			"source_sha256": FileAccess.get_sha256(sprite.path),
		}
	var manifest := {"sheet_sha256": FileAccess.get_sha256(sheet_path), "sprites": entries}
	var file := FileAccess.open(out + group + ".json", FileAccess.WRITE)
	if file == null:
		errors.append("can't write %s" % (out + group + ".json"))
		return false
	file.store_string(JSON.stringify(manifest, "  ", true) + "\n")
	file.close()
	return true


func pack(sprites: Array[Sprite]) -> Image:
	sprites.sort_custom(func(first: Sprite, second: Sprite) -> bool:
		if first.size().y != second.size().y:
			return first.size().y > second.size().y
		return first.number < second.number)
	var widest := 0
	for sprite in sprites:
		widest = max(widest, sprite.footprint().x)
	var width := MIN_SHEET_WIDTH
	while width < widest:
		width *= 2
	var height := place(sprites, width)
	while height > width:
		width *= 2
		height = place(sprites, width)

	var sheet := Image.create_empty(width, ceili(float(height) / HEIGHT_STEP) * HEIGHT_STEP, false, Image.FORMAT_RGBA8)
	for sprite in sprites:
		extrude(sheet, sprite)
	return sheet


func place(sprites: Array[Sprite], width: int) -> int:
	var cursor := Vector2i.ZERO
	var row_height := 0
	for sprite in sprites:
		var footprint := sprite.footprint()
		if cursor.x + footprint.x > width:
			cursor = Vector2i(0, cursor.y + row_height)
			row_height = 0
		if row_height == 0:
			row_height = footprint.y
		sprite.position = cursor
		cursor.x += footprint.x
	return cursor.y + row_height


func extrude(sheet: Image, sprite: Sprite) -> void:
	var image := sprite.image
	var size := image.get_size()
	var inner := sprite.region().position
	sheet.blit_rect(image, Rect2i(Vector2i.ZERO, size), inner)
	sheet.blit_rect(image, Rect2i(0, 0, size.x, 1), inner + Vector2i(0, -1))
	sheet.blit_rect(image, Rect2i(0, size.y - 1, size.x, 1), inner + Vector2i(0, size.y))
	sheet.blit_rect(image, Rect2i(0, 0, 1, size.y), inner + Vector2i(-1, 0))
	sheet.blit_rect(image, Rect2i(size.x - 1, 0, 1, size.y), inner + Vector2i(size.x, 0))
	sheet.set_pixelv(inner + Vector2i(-1, -1), image.get_pixel(0, 0))
	sheet.set_pixelv(inner + Vector2i(size.x, -1), image.get_pixel(size.x - 1, 0))
	sheet.set_pixelv(inner + Vector2i(-1, size.y), image.get_pixel(0, size.y - 1))
	sheet.set_pixelv(inner + Vector2i(size.x, size.y), image.get_pixel(size.x - 1, size.y - 1))


func matches(sheet: Image, sprite: Sprite) -> bool:
	var expected := Image.create_empty(sprite.footprint().x, sprite.footprint().y, false, Image.FORMAT_RGBA8)
	var placed := Sprite.new(sprite.number, sprite.path, sprite.image)
	extrude(expected, placed)
	return sheet.get_region(Rect2i(sprite.position, sprite.footprint())).get_data() == expected.get_data()


func remove_stale_groups(out: String, groups: Dictionary) -> void:
	for file in DirAccess.get_files_at(out):
		if file.get_extension() != "json":
			continue
		var group := file.get_basename()
		if groups.has(group):
			continue
		for stale in [group + ".json", group + ".png", group + ".png.import"]:
			if FileAccess.file_exists(out + stale):
				DirAccess.remove_absolute(out + stale)
