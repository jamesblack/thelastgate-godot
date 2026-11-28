# Builds the classic bitmap fonts from the font sheet sprites, and the HUD text theme that names them. Rerun after a
# font sheet changes, then commit the fonts and the theme.
# Each sheet holds 96 letters of 6x9 pixels from its second pixel row, for the characters from space to 127. Grey, Dim
# and Engraved have no sheet: they are the white letters tinted, Engraved like the lettering in the window art.
# Nothing is written unless every font sheet is found in art/.
# Usage: godot --headless --path game --script ../tools/build_classic_fonts.gd [-- --art <folder>]
# Then save the theme once in the editor: a script run can't write the uids of the theme's font references.
extends SceneTree

const FIRST_FONT_SPRITE := 18100
const SHEET_COLOURS := ["Red", "Yellow", "Green", "Blue", "Pink", "Orange", "Lime", "Teal", "Purple", "White"]
const TINTED_WHITE := {
	"Grey": Color8(72, 81, 81),
	"Dim": Color8(144, 154, 154),
	"Engraved": Color8(86, 87, 76),
}
const FONT_SIZE := 9
const GLYPH_WIDTH := 6
const GLYPH_HEIGHT := 9
const GLYPH_TOP := 1
const FIRST_CHARACTER := 32
const LAST_CHARACTER := 127
const FONT_FOLDER := "res://fonts/classic/"
const THEME_PATH := "res://themes/HudTextTheme.tres"


func _init() -> void:
	var art := ProjectSettings.globalize_path("res://").path_join("../art").simplify_path()
	var arguments := OS.get_cmdline_user_args()
	if arguments.size() == 2 and arguments[0] == "--art":
		art = arguments[1]
	elif not arguments.is_empty():
		push_error("usage: --script build_classic_fonts.gd [-- --art <folder>]")
		quit(2)
		return

	var sheets: Array[Image] = []
	for index in SHEET_COLOURS.size():
		var sheet := art_image(art, FIRST_FONT_SPRITE + index)
		if sheet == null:
			quit(1)
			return
		sheets.append(sheet)

	var theme := Theme.new()
	theme.default_font_size = FONT_SIZE
	for index in SHEET_COLOURS.size():
		var colour: String = SHEET_COLOURS[index]
		var font := build_font(sheets[index])
		var path := FONT_FOLDER + colour.to_lower() + ".tres"
		if not save_keeping_uid(font, path):
			quit(1)
			return

		font = load(path)
		add_variation(theme, colour, font, Color.WHITE)
		if colour == "White":
			theme.default_font = font

	for colour: String in TINTED_WHITE:
		add_variation(theme, colour, theme.default_font, TINTED_WHITE[colour])

	if not save_keeping_uid(theme, THEME_PATH):
		quit(1)
		return

	print("built %d fonts and %s" % [SHEET_COLOURS.size(), THEME_PATH])
	quit()


# A script run doesn't give saved resources a uid as the editor does, so each file keeps the one it had, or gets one.
func save_keeping_uid(resource: Resource, path: String) -> bool:
	var uid := ResourceLoader.get_resource_uid(path)
	if uid == ResourceUID.INVALID_ID:
		uid = ResourceUID.create_id()
	if ResourceUID.has_id(uid):
		ResourceUID.set_id(uid, path)
	else:
		ResourceUID.add_id(uid, path)
	if ResourceSaver.save(resource, path) != OK or ResourceSaver.set_uid(path, uid) != OK:
		push_error("can't write %s" % path)
		return false
	return true


func build_font(sheet: Image) -> FontFile:
	var font := FontFile.new()
	font.fixed_size = FONT_SIZE
	font.antialiasing = TextServer.FONT_ANTIALIASING_NONE
	font.generate_mipmaps = false
	font.hinting = TextServer.HINTING_NONE
	font.subpixel_positioning = TextServer.SUBPIXEL_POSITIONING_DISABLED
	var size := Vector2i(FONT_SIZE, 0)
	font.set_texture_image(0, size, 0, sheet)
	font.set_cache_ascent(0, FONT_SIZE, GLYPH_HEIGHT)
	font.set_cache_descent(0, FONT_SIZE, 0)
	for character in range(FIRST_CHARACTER, LAST_CHARACTER + 1):
		font.set_glyph_advance(0, FONT_SIZE, character, Vector2(GLYPH_WIDTH, 0))
		font.set_glyph_offset(0, size, character, Vector2(0, -GLYPH_HEIGHT))
		font.set_glyph_size(0, size, character, Vector2(GLYPH_WIDTH, GLYPH_HEIGHT))
		var cell := Rect2((character - FIRST_CHARACTER) * GLYPH_WIDTH, GLYPH_TOP, GLYPH_WIDTH, GLYPH_HEIGHT)
		font.set_glyph_uv_rect(0, size, character, cell)
		font.set_glyph_texture_idx(0, size, character, 0)
	return font


# Null, with the reason pushed, when the sprite is in no group or won't decode.
func art_image(art: String, sprite_number: int) -> Image:
	for group in DirAccess.get_directories_at(art):
		var path := art.path_join(group).path_join("%05d.png" % sprite_number)
		if FileAccess.file_exists(path):
			var image := Image.load_from_file(path)
			if image == null:
				push_error("art/%s/%05d.png won't decode" % [group, sprite_number])
				return null
			image.convert(Image.FORMAT_RGBA8)
			return image
	push_error("sprite %d isn't in any art/ folder" % sprite_number)
	return null


func add_variation(theme: Theme, colour: String, font: Font, tint: Color) -> void:
	var variation := "Hud" + colour
	theme.set_type_variation(variation, "Label")
	theme.set_font("font", variation, font)
	theme.set_font_size("font_size", variation, FONT_SIZE)
	theme.set_color("font_color", variation, tint)
