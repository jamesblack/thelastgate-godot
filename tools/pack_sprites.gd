# Packs the imported sprites into their own resource pack, so they ship apart from the game's main pack.
# Each sheet needs its .import file, which the loader follows, and the imported texture it points at, the same
# pair a normal export writes. The art sheets' .json manifests go in as they are; without one the client finds no art
# sprite, so a pack with none is refused.
# Usage: godot --headless --path game --script <this file> -- <output .pck>
extends SceneTree

const SPRITE_FOLDER := "res://sprites/"
const IMPORT_SUFFIX := ".png.import"
const MANIFEST_SUFFIX := ".json"


func _init() -> void:
	var arguments := OS.get_cmdline_user_args()
	if arguments.size() != 1:
		push_error("usage: --script pack_sprites.gd -- <output .pck>")
		quit(2)
		return

	var import_paths: Array[String] = []
	var manifest_paths: Array[String] = []
	collect(SPRITE_FOLDER, import_paths, manifest_paths)
	if manifest_paths.is_empty():
		push_error("%s has no art sheet manifests; run tools/build_art_sheets.gd" % SPRITE_FOLDER)
		quit(1)
		return

	var packer := PCKPacker.new()
	if packer.pck_start(arguments[0]) != OK:
		push_error("can't write %s" % arguments[0])
		quit(1)
		return

	var packed := 0

	for import_path in import_paths:
		var settings := ConfigFile.new()
		settings.load(import_path)
		var imported: String = settings.get_value("remap", "path", "")
		if imported.is_empty() or not FileAccess.file_exists(imported):
			push_error("%s has no imported texture; run an import first" % import_path)
			quit(1)
			return

		packer.add_file(import_path, import_path)
		packer.add_file(imported, imported)
		packed += 1

	for manifest_path in manifest_paths:
		packer.add_file(manifest_path, manifest_path)

	packer.flush()
	print("packed %d sheets and %d manifests into %s" % [packed, manifest_paths.size(), arguments[0]])
	quit(0 if packed > 0 else 1)


func collect(folder: String, import_paths: Array[String], manifest_paths: Array[String]) -> void:
	for file in DirAccess.get_files_at(folder):
		if file.ends_with(IMPORT_SUFFIX):
			import_paths.append(folder + file)
		elif file.ends_with(MANIFEST_SUFFIX):
			manifest_paths.append(folder + file)
	for sub in DirAccess.get_directories_at(folder):
		collect(folder + sub + "/", import_paths, manifest_paths)
