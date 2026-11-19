# Changelog

## [1.2.0](https://github.com/jamesblack/thelastgate-godot/compare/v1.1.0...v1.2.0) (2026-11-19)


### Features

* **hud:** bring back the gamma key, now for world gamma only ([bb055b3](https://github.com/jamesblack/thelastgate-godot/commit/bb055b3af2195ab00872880ee3b8306e30919b82))
* **hud:** right-click an offense or defense stat to read what it does ([5fc31ce](https://github.com/jamesblack/thelastgate-godot/commit/5fc31cea97d583ca064f34f2a5a79b7f8f2083d3))
* **hud:** show offense and defense stats with the List Offenses and List Defenses buttons ([5fc31ce](https://github.com/jamesblack/thelastgate-godot/commit/5fc31cea97d583ca064f34f2a5a79b7f8f2083d3))
* **hud:** show the summary stats above the skills by clicking List Skills again ([5fc31ce](https://github.com/jamesblack/thelastgate-godot/commit/5fc31cea97d583ca064f34f2a5a79b7f8f2083d3))
* **windows:** warn when a hotkey would type into the chat line ([87dd248](https://github.com/jamesblack/thelastgate-godot/commit/87dd248259ef2092bfe029c6773387d809e1f6fb))


### Bug Fixes

* **hud:** hand typing back to chat when a window field is done ([87dd248](https://github.com/jamesblack/thelastgate-godot/commit/87dd248259ef2092bfe029c6773387d809e1f6fb))
* **hud:** let the skill list slider run the full height of its frame, without arrows ([5fc31ce](https://github.com/jamesblack/thelastgate-godot/commit/5fc31cea97d583ca064f34f2a5a79b7f8f2083d3))
* **hud:** scroll the chat log with page up and page down while typing ([87dd248](https://github.com/jamesblack/thelastgate-godot/commit/87dd248259ef2092bfe029c6773387d809e1f6fb))
* **launcher:** show the connection status in the launcher and stay there if it fails ([70f475a](https://github.com/jamesblack/thelastgate-godot/commit/70f475ab5cd4e4876a922ac2ef1ec1b39b28ed94))
* **planner:** show the Bless power that recasting settles at ([f7a5c5f](https://github.com/jamesblack/thelastgate-godot/commit/f7a5c5f4174097df59caa2e7a295411c4766026a))
* **planner:** stop a placeholder effect showing on spell rows and covering the warning ([f7a5c5f](https://github.com/jamesblack/thelastgate-godot/commit/f7a5c5f4174097df59caa2e7a295411c4766026a))

## [1.1.0](https://github.com/jamesblack/thelastgate-godot/compare/v1.0.1...v1.1.0) (2026-11-19)


### Features

* **launcher:** open the data and sprite overrides folders ([20f7830](https://github.com/jamesblack/thelastgate-godot/commit/20f78309d2a8d4d54d59f07ee8ffdca49789a682)), closes [#3](https://github.com/jamesblack/thelastgate-godot/issues/3)


### Bug Fixes

* **hud:** keep the hotbar menu text sharp and bright ([4624125](https://github.com/jamesblack/thelastgate-godot/commit/4624125222399b6b5d9c27aea7d1fbd157982c74))
* **hud:** send a recalled chat line with one Enter ([2d4b9e8](https://github.com/jamesblack/thelastgate-godot/commit/2d4b9e83d5060ce6d55da005d624a47164a0487b))
* **map:** keep right-click menu text sharp and bright ([4624125](https://github.com/jamesblack/thelastgate-godot/commit/4624125222399b6b5d9c27aea7d1fbd157982c74))

## [1.0.1](https://github.com/jamesblack/thelastgate-godot/compare/v1.0.0...v1.0.1) (2026-11-19)


### Bug Fixes

* **hud:** make hotkeys work while enter to talk is off ([f587d93](https://github.com/jamesblack/thelastgate-godot/commit/f587d9389af77e3b109b7889668b3a9bf2d2cb45))

## 1.0.0 (2026-11-19)


### Features

* install the game with Setup.exe on Windows or as an AppImage on Linux ([3636f95](https://github.com/jamesblack/thelastgate-godot/commit/3636f95b3bf4048fbda7957bc81f31e7013355a3))
* **launcher:** roll back to an earlier version by holding Ctrl ([3636f95](https://github.com/jamesblack/thelastgate-godot/commit/3636f95b3bf4048fbda7957bc81f31e7013355a3))
* **launcher:** show what each update changes before installing it ([3636f95](https://github.com/jamesblack/thelastgate-godot/commit/3636f95b3bf4048fbda7957bc81f31e7013355a3))
* **launcher:** tell players when an update is out and install it when they ask ([3636f95](https://github.com/jamesblack/thelastgate-godot/commit/3636f95b3bf4048fbda7957bc81f31e7013355a3))
* show the game's icon on its window and shortcuts ([3636f95](https://github.com/jamesblack/thelastgate-godot/commit/3636f95b3bf4048fbda7957bc81f31e7013355a3))


### Bug Fixes

* **display:** read a player's own sprite overrides from their user folder, so they work on Linux and survive updates ([52a8924](https://github.com/jamesblack/thelastgate-godot/commit/52a8924da70274c9aaf36d22b6f602bb9a74f91a))
* **launcher:** bring over the SDL client's characters, map and notes on Linux ([52a8924](https://github.com/jamesblack/thelastgate-godot/commit/52a8924da70274c9aaf36d22b6f602bb9a74f91a))
* **planner:** let spells be ticked in the Advanced window ([8375a80](https://github.com/jamesblack/thelastgate-godot/commit/8375a80cc0bb68f27076a7d763b188603d01c82a))
* **world:** keep walking when a corner button around the map is clicked ([ef71a56](https://github.com/jamesblack/thelastgate-godot/commit/ef71a5619f82c93f7b08c23a07f00883e292f225))
* **world:** stop dark specks appearing between floor tiles at some window sizes ([fe0a2b4](https://github.com/jamesblack/thelastgate-godot/commit/fe0a2b44657b5a3cc2b2e785e5aec86f39cf0658))


### Build

* release with Release Please ([0f18828](https://github.com/jamesblack/thelastgate-godot/commit/0f1882817b47ed06e48c8df97cee2b8f92f540d8))
