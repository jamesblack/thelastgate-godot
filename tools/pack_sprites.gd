# Packs the imported sprites into their own resource pack, so they ship apart from the game's main pack.
# Each sprite needs its .import file, which the loader follows, and the imported texture it points at, the same
# pair a normal export writes.
# Usage: godot --headless --path game --script <this file> -- <output .pck>
extends SceneTree

const SPRITE_FOLDERS: Array[String] = ["res://sprites/", "res://sprites/characters/"]
const IMPORT_SUFFIX := ".png.import"


func _init() -> void:
	var arguments := OS.get_cmdline_user_args()
	if arguments.size() != 1:
		push_error("usage: --script pack_sprites.gd -- <output .pck>")
		quit(2)
		return

	var packer := PCKPacker.new()
	if packer.pck_start(arguments[0]) != OK:
		push_error("can't write %s" % arguments[0])
		quit(1)
		return

	var packed := 0
	var import_paths: Array[String] = []
	for folder in SPRITE_FOLDERS:
		for file in DirAccess.get_files_at(folder):
			if file.ends_with(IMPORT_SUFFIX):
				import_paths.append(folder + file)

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

	packer.flush()
	print("packed %d sprites into %s" % [packed, arguments[0]])
	quit(0 if packed > 0 else 1)
