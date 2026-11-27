# Folds loose character frames (NNNNN.png) into their character's sheet in characters/, then deletes them.
# A sheet is NNNNN-<name>.png, where NNNNN is the character's base sprite. It is a grid of 32 x 14 cells, one per
# offset from the base (offset k at column k % 32, row k / 32), each the size of that character's frames.
# Nothing is written unless every frame fits: same size as its sheet's cells, not blank, and into an empty cell.
# Usage: godot --headless --path game --script ../tools/build_character_sheets.gd -- \
#            [--folder <sprites folder>] [--new <base sprite> <name>]...
extends SceneTree

const COLUMNS := 32
const ROWS := 14
const SLOTS := COLUMNS * ROWS
const SHEET_FOLDER := "characters/"

var sheet_name_pattern := RegEx.create_from_string("^(\\d{5})-([a-z0-9]+(?:-[a-z0-9]+)*)\\.png$")
var frame_name_pattern := RegEx.create_from_string("^(\\d{5})\\.png$")
var errors: PackedStringArray = []


class Sheet:
	var base: int
	var file_name: String
	var image: Image
	var cell_size := Vector2i.ZERO
	var folded: Dictionary = {}

	func _init(sheet_base: int, sheet_file_name: String, sheet_image: Image) -> void:
		base = sheet_base
		file_name = sheet_file_name
		image = sheet_image
		if image != null:
			cell_size = Vector2i(image.get_width() / COLUMNS, image.get_height() / ROWS)

	func holds(sprite_number: int) -> bool:
		return sprite_number >= base and sprite_number < base + SLOTS

	func cell(offset: int) -> Rect2i:
		return Rect2i(Vector2i(offset % COLUMNS, offset / COLUMNS) * cell_size, cell_size)


func _init() -> void:
	var folder := "res://sprites/"
	var new_sheets: Array[Array] = []
	var arguments := OS.get_cmdline_user_args()
	var index := 0
	while index < arguments.size():
		match arguments[index]:
			"--folder" when index + 1 < arguments.size():
				folder = arguments[index + 1].trim_suffix("/") + "/"
				index += 2
			"--new" when index + 2 < arguments.size():
				new_sheets.append([arguments[index + 1].to_int(), arguments[index + 2]])
				index += 3
			_:
				push_error("usage: --script build_character_sheets.gd -- [--folder <folder>] [--new <base> <name>]...")
				quit(2)
				return

	var sheets := load_sheets(folder + SHEET_FOLDER)
	for new_sheet in new_sheets:
		start_sheet(sheets, new_sheet[0], new_sheet[1])
	var frames := fold_frames(folder, sheets)
	if not errors.is_empty():
		for error in errors:
			printerr(error)
		quit(1)
		return

	var touched := sheets.filter(func(sheet: Sheet) -> bool: return not sheet.folded.is_empty())
	DirAccess.make_dir_recursive_absolute(folder + SHEET_FOLDER)
	for sheet: Sheet in touched:
		save_and_check(folder + SHEET_FOLDER, sheet)
	if not errors.is_empty():
		for error in errors:
			printerr(error)
		quit(1)
		return

	for sprite_number: int in frames:
		var frame_path: String = frames[sprite_number]
		DirAccess.remove_absolute(frame_path)
		if FileAccess.file_exists(frame_path + ".import"):
			DirAccess.remove_absolute(frame_path + ".import")
	print("folded %d frames into %d sheets" % [frames.size(), touched.size()])
	quit(0)


func load_sheets(sheet_folder: String) -> Array[Sheet]:
	var sheets: Array[Sheet] = []
	if not DirAccess.dir_exists_absolute(sheet_folder):
		return sheets
	for file in DirAccess.get_files_at(sheet_folder):
		var found := sheet_name_pattern.search(file)
		if found == null:
			continue
		var image := Image.load_from_file(sheet_folder + file)
		if image == null:
			errors.append("%s couldn't be read" % file)
			continue
		image.convert(Image.FORMAT_RGBA8)
		sheets.append(Sheet.new(found.get_string(1).to_int(), file, image))
	return sheets


func start_sheet(sheets: Array[Sheet], base: int, sheet_name: String) -> void:
	var file_name := "%05d-%s.png" % [base, sheet_name]
	if sheet_name_pattern.search(file_name) == null:
		errors.append("%s isn't a sheet name: use lower-case letters, digits and hyphens" % sheet_name)
		return
	for sheet in sheets:
		if absi(sheet.base - base) < SLOTS:
			errors.append("%s would overlap %s" % [file_name, sheet.file_name])
			return
	sheets.append(Sheet.new(base, file_name, null))


func fold_frames(folder: String, sheets: Array[Sheet]) -> Dictionary:
	var frames := {}
	var names := DirAccess.get_files_at(folder)
	names.sort()
	for file in names:
		var found := frame_name_pattern.search(file)
		if found == null:
			continue
		var sprite_number := found.get_string(1).to_int()
		var owners := sheets.filter(func(sheet: Sheet) -> bool: return sheet.holds(sprite_number))
		if owners.is_empty():
			continue
		var sheet: Sheet = owners[0]
		var frame := Image.load_from_file(folder + file)
		if frame == null:
			errors.append("%s couldn't be read" % file)
			continue
		frame.convert(Image.FORMAT_RGBA8)
		if frame.is_invisible():
			errors.append("%s is blank, so it would read as an empty cell" % file)
			continue
		if sheet.image == null:
			sheet.cell_size = frame.get_size()
			sheet.image = Image.create_empty(
				sheet.cell_size.x * COLUMNS, sheet.cell_size.y * ROWS, false, Image.FORMAT_RGBA8)
		if frame.get_size() != sheet.cell_size:
			errors.append("%s is %s, but %s's cells are %s" % [file, frame.get_size(), sheet.file_name, sheet.cell_size])
			continue
		var offset := sprite_number - sheet.base
		var cell := sheet.cell(offset)
		if not sheet.image.get_region(cell).is_invisible():
			errors.append("%s's cell %d already holds a frame; clear it by hand to replace it" % [sheet.file_name, offset])
			continue
		sheet.image.blit_rect(frame, Rect2i(Vector2i.ZERO, sheet.cell_size), cell.position)
		sheet.folded[offset] = frame
		frames[sprite_number] = folder + file
	for sheet in sheets:
		if sheet.image == null:
			errors.append("%s has no frames to start it with" % sheet.file_name)
	return frames


func save_and_check(sheet_folder: String, sheet: Sheet) -> void:
	var path := sheet_folder + sheet.file_name
	if sheet.image.save_png(path) != OK:
		errors.append("%s couldn't be written" % path)
		return
	var saved := Image.load_from_file(path)
	saved.convert(Image.FORMAT_RGBA8)
	for offset: int in sheet.folded:
		var frame: Image = sheet.folded[offset]
		if saved.get_region(sheet.cell(offset)).get_data() != frame.get_data():
			errors.append("%s's cell %d doesn't match its frame after saving" % [sheet.file_name, offset])
