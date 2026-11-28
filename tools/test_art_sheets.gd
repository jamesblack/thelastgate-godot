# Runs build_art_sheets.gd on scratch art folders and checks the sheets and manifests it writes.
# Usage: godot --headless --path game --script ../tools/test_art_sheets.gd
extends SceneTree

const TOOL := "build_art_sheets.gd"

var scratch := OS.get_temp_dir().path_join("tlg-art-test-%d" % Time.get_ticks_usec()) + "/"
var art := scratch + "art/"
var out := scratch + "out/"
var failures := 0
var tool_output := ""


func _init() -> void:
	DirAccess.make_dir_recursive_absolute(art + "floors")
	DirAccess.make_dir_recursive_absolute(art + "wide")
	DirAccess.make_dir_recursive_absolute(out + "characters")
	Image.create_empty(2048, 896, false, Image.FORMAT_RGBA8).save_png(out + "characters/02000-knight.png")
	write_sprite("floors/00116", 32, 32, Color.RED, false)
	write_sprite("floors/00117", 32, 32, Color.BLUE, false)
	write_sprite("floors/00118", 32, 32, Color.GREEN, true)
	write_sprite("floors/00119", 32, 32, Color.TRANSPARENT, false)
	write_sprite("wide/00500", 64, 64, Color.YELLOW, false)
	write_sprite("wide/18101", 576, 11, Color.WHITE, false)
	FileAccess.open(out + "wide.png.import", FileAccess.WRITE).close()

	check(run_tool(), "the first run failed: " + tool_output)
	check(tool_output.contains("built 2 sheets holding 6 sprites"), "no summary line: " + tool_output)
	for file in ["floors.png", "floors.json", "wide.png", "wide.json"]:
		check(FileAccess.file_exists(out + file), file + " wasn't written")

	for group in ["floors", "wide"]:
		var manifest := read_manifest(group)
		var sheet := load_rgba(out + group + ".png")
		if sheet == null:
			check(false, group + ".png won't load")
			continue
		check(manifest.get("sheet_sha256") == FileAccess.get_sha256(out + group + ".png"),
			group + ": sheet_sha256 isn't the sheet's hash")
		for number: String in manifest.get("sprites", {}):
			var entry: Dictionary = manifest["sprites"][number]
			var source_path: String = art + group + "/" + number + ".png"
			var source := load_rgba(source_path)
			var region := Rect2i(entry["x"], entry["y"], entry["w"], entry["h"])
			if source == null:
				check(false, number + ": its source won't load")
				continue
			check(region.size == source.get_size(), number + ": region size %s isn't the sprite's" % region.size)
			check(sheet.get_region(region).get_data() == source.get_data(), number + ": region differs from its source")
			check(border_extruded(sheet, region, source), number + ": border doesn't repeat the sprite's edges")
			check(entry["source_sha256"] == FileAccess.get_sha256(source_path), number + ": source_sha256 is wrong")

	var wide := load_rgba(out + "wide.png")
	check(wide != null and wide.get_width() >= 578, "the wide sheet is narrower than its widest sprite plus border")
	check(used_area("00119") == Rect2i(0, 0, 0, 0), "the transparent sprite's used area isn't empty")
	check(used_area("00118") == Rect2i(16, 0, 16, 32), "the half-transparent sprite's used area isn't its right half")
	check(used_area("00116") == Rect2i(0, 0, 32, 32), "the solid sprite's used area isn't all of it")

	var hashes := output_hashes()
	check(run_tool(), "the second run failed: " + tool_output)
	check(output_hashes() == hashes, "a second run over the same art changed the output")

	refuses("abc", "floors/abc.png", "abc.png isn't NNNNN.png")
	refuses("too_long", "floors/140000.png", "140000.png isn't NNNNN.png")
	refuses("duplicate", "wide/00116.png", "00116 is in floors and wide")
	refuses("character_range", "floors/02005.png", "02005 falls on 02000-knight.png")
	refuses("bad_group", "Bad_Name/00900.png", "Bad_Name isn't a group name")
	refuses("numeric_group", "00901/00902.png", "00901 isn't a group name")
	refuses("reserved_group", "characters/00903.png", "characters is reserved")
	FileAccess.open(art + "floors/00904.png", FileAccess.WRITE).store_string("not a png")
	check(not run_tool(), "refuses_undecodable: a broken PNG was accepted")
	check(tool_output.contains("00904.png won't decode"), "refuses_undecodable: no error naming the file")
	check(output_hashes() == hashes, "refuses_undecodable: the output changed")
	DirAccess.remove_absolute(art + "floors/00904.png")

	remove_folder(art + "wide/")
	FileAccess.open(out + "00002.png", FileAccess.WRITE).close()
	check(run_tool(), "the run after removing a group failed: " + tool_output)
	for file in ["wide.png", "wide.png.import", "wide.json"]:
		check(not FileAccess.file_exists(out + file), file + " was left after its group went")
	check(FileAccess.file_exists(out + "characters/02000-knight.png"), "the character sheet was removed")
	check(FileAccess.file_exists(out + "00002.png"), "a loose sprite in the output folder was removed")
	check(FileAccess.file_exists(out + "floors.png"), "the floors sheet was removed")

	remove_folder(scratch)
	print("all checks passed" if failures == 0 else "%d checks failed" % failures)
	quit(0 if failures == 0 else 1)


func check(passed: bool, message: String) -> void:
	if not passed:
		printerr("FAIL: " + message)
		failures += 1


func refuses(case: String, file: String, message: String) -> void:
	var path := art + file
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	write_sprite(file.get_basename(), 32, 32, Color.ORANGE, false)
	var hashes := output_hashes()
	check(not run_tool(), "refuses_%s: %s was accepted" % [case, file])
	check(tool_output.contains(message), "refuses_%s: no error saying \"%s\"" % [case, message])
	check(output_hashes() == hashes, "refuses_%s: the output changed" % case)
	DirAccess.remove_absolute(path)
	if DirAccess.get_files_at(path.get_base_dir()).is_empty():
		DirAccess.remove_absolute(path.get_base_dir())


func run_tool() -> bool:
	var this_script: Script = get_script()
	var tool_path := ProjectSettings.globalize_path(this_script.resource_path.get_base_dir()).path_join(TOOL)
	var output := []
	var exit_code := OS.execute(
		OS.get_executable_path(),
		["--headless", "--path", ProjectSettings.globalize_path("res://"), "--script", tool_path, "--",
			"--art", art, "--out", out],
		output,
		true)
	tool_output = "\n".join(output)
	return exit_code == 0


func write_sprite(name: String, width: int, height: int, colour: Color, left_half_clear: bool) -> void:
	var image := Image.create_empty(width, height, false, Image.FORMAT_RGBA8)
	image.fill(colour)
	for x in width:
		for y in height:
			if (x + y) % 3 == 0 and colour.a > 0:
				image.set_pixel(x, y, colour.darkened(0.5))
	if left_half_clear:
		image.fill_rect(Rect2i(0, 0, width / 2, height), Color.TRANSPARENT)
	image.save_png(art + name + ".png")


func read_manifest(group: String) -> Dictionary:
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(out + group + ".json"))
	return parsed if parsed is Dictionary else {}


func load_rgba(path: String) -> Image:
	if not FileAccess.file_exists(path):
		return null
	var image := Image.load_from_file(path)
	if image != null:
		image.convert(Image.FORMAT_RGBA8)
	return image


func used_area(number: String) -> Rect2i:
	var entry: Dictionary = read_manifest("floors").get("sprites", {}).get(number, {})
	var used: Dictionary = entry.get("used", {})
	return Rect2i(used.get("x", -1), used.get("y", -1), used.get("w", -1), used.get("h", -1))


func border_extruded(sheet: Image, region: Rect2i, source: Image) -> bool:
	var width := region.size.x
	var height := region.size.y
	var x := region.position.x
	var y := region.position.y
	return (sheet.get_region(Rect2i(x, y - 1, width, 1)).get_data() == source.get_region(Rect2i(0, 0, width, 1)).get_data()
		and sheet.get_region(Rect2i(x, y + height, width, 1)).get_data()
			== source.get_region(Rect2i(0, height - 1, width, 1)).get_data()
		and sheet.get_region(Rect2i(x - 1, y, 1, height)).get_data() == source.get_region(Rect2i(0, 0, 1, height)).get_data()
		and sheet.get_region(Rect2i(x + width, y, 1, height)).get_data()
			== source.get_region(Rect2i(width - 1, 0, 1, height)).get_data()
		and sheet.get_pixel(x - 1, y - 1) == source.get_pixel(0, 0)
		and sheet.get_pixel(x + width, y - 1) == source.get_pixel(width - 1, 0)
		and sheet.get_pixel(x - 1, y + height) == source.get_pixel(0, height - 1)
		and sheet.get_pixel(x + width, y + height) == source.get_pixel(width - 1, height - 1))


func output_hashes() -> Dictionary:
	var hashes := {}
	for file in DirAccess.get_files_at(out):
		hashes[file] = FileAccess.get_sha256(out + file)
	return hashes


func remove_folder(folder: String) -> void:
	for sub in DirAccess.get_directories_at(folder):
		remove_folder(folder.path_join(sub) + "/")
	for file in DirAccess.get_files_at(folder):
		DirAccess.remove_absolute(folder + file)
	DirAccess.remove_absolute(folder)
