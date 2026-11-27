# Runs build_character_sheets.gd on a scratch copy of a few frames and checks what it made.
# Usage: godot --headless --path game --script ../tools/test_character_sheets.gd
extends SceneTree

const SPRITES := "res://sprites/"
const TOOL := "build_character_sheets.gd"

var scratch := OS.get_temp_dir().path_join("tlg-sheet-test-%d" % Time.get_ticks_usec()) + "/"
var failures := 0
var tool_output := ""


func _init() -> void:
	DirAccess.make_dir_recursive_absolute(scratch)
	for frame in ["02000", "02064", "02447", "22480", "22481", "16336"]:
		copy_sprite(frame, frame)
		FileAccess.open(scratch + frame + ".png.import", FileAccess.WRITE).close()

	check(run_tool(["--new", "2000", "knight", "--new", "22480", "flame"]), "the first run failed: " + tool_output)
	check(size_of("characters/02000-knight.png") == Vector2i(2048, 896), "the knight sheet isn't 2048x896")
	check(size_of("characters/22480-flame.png") == Vector2i(1024, 896), "the flame sheet isn't 1024x896")
	for frame in ["02000", "02064", "02447", "22480", "22481"]:
		check(not FileAccess.file_exists(scratch + frame + ".png"), frame + ".png was left loose")
		check(not FileAccess.file_exists(scratch + frame + ".png.import"), frame + ".png.import was left")
	check(FileAccess.file_exists(scratch + "16336.png"), "16336, outside every sheet, was removed")
	check(cell_matches("characters/02000-knight.png", 447, "02447"), "cell 447 of the knight sheet differs from 02447")
	check(cell_matches("characters/22480-flame.png", 1, "22481"), "cell 1 of the flame sheet differs from 22481")

	Image.create_empty(64, 64, false, Image.FORMAT_RGBA8).save_png(scratch + "02002.png")
	check(not run_tool([]), "refuses_a_blank_frame: a blank frame was accepted")
	check(tool_output.contains("02002.png is blank"), "refuses_a_blank_frame: no error naming the frame")
	check(FileAccess.file_exists(scratch + "02002.png"), "refuses_a_blank_frame: the blank frame was removed")
	DirAccess.remove_absolute(scratch + "02002.png")

	var small := Image.create_empty(32, 32, false, Image.FORMAT_RGBA8)
	small.fill(Color.RED)
	small.save_png(scratch + "02003.png")
	check(not run_tool([]), "refuses_a_frame_of_another_size: a 32x32 frame was accepted")
	check(tool_output.contains("02003.png is (32, 32)"), "refuses_a_frame_of_another_size: no error naming the frame")
	DirAccess.remove_absolute(scratch + "02003.png")

	copy_sprite("02065", "02064")
	check(not run_tool([]), "refuses_to_replace_a_frame: a second 02064 replaced the first")
	check(tool_output.contains("cell 64 already holds a frame"), "refuses_to_replace_a_frame: no error naming the cell")

	remove_scratch(scratch)
	print("all checks passed" if failures == 0 else "%d checks failed" % failures)
	quit(0 if failures == 0 else 1)


func check(passed: bool, message: String) -> void:
	if not passed:
		printerr("FAIL: " + message)
		failures += 1


func run_tool(arguments: Array) -> bool:
	var this_script: Script = get_script()
	var tool_path := ProjectSettings.globalize_path(this_script.resource_path.get_base_dir()).path_join(TOOL)
	var output := []
	var exit_code := OS.execute(
		OS.get_executable_path(),
		["--headless", "--path", ProjectSettings.globalize_path("res://"), "--script", tool_path, "--",
			"--folder", scratch] + arguments,
		output,
		true)
	tool_output = "\n".join(output)
	return exit_code == 0


func copy_sprite(from_frame: String, to_frame: String) -> void:
	var image := Image.load_from_file(SPRITES + from_frame + ".png")
	image.save_png(scratch + to_frame + ".png")


func size_of(file: String) -> Vector2i:
	var image := Image.load_from_file(scratch + file)
	return image.get_size() if image != null else Vector2i.ZERO


func cell_matches(sheet_file: String, offset: int, frame: String) -> bool:
	var sheet := Image.load_from_file(scratch + sheet_file)
	var original := Image.load_from_file(SPRITES + frame + ".png")
	if sheet == null or original == null:
		return false
	sheet.convert(Image.FORMAT_RGBA8)
	original.convert(Image.FORMAT_RGBA8)
	var cell_size := Vector2i(sheet.get_width() / 32, sheet.get_height() / 14)
	var cell := Rect2i(Vector2i(offset % 32, offset / 32) * cell_size, cell_size)
	return sheet.get_region(cell).get_data() == original.get_data()


func remove_scratch(folder: String) -> void:
	for sub in DirAccess.get_directories_at(folder):
		remove_scratch(folder.path_join(sub) + "/")
	for file in DirAccess.get_files_at(folder):
		DirAccess.remove_absolute(folder + file)
	DirAccess.remove_absolute(folder)
