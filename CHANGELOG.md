# Changelog

## [1.4.0](https://github.com/jamesblack/thelastgate-godot/compare/v1.3.0...v1.4.0) (2026-11-24)


### Features

* **display:** soften the infravision tint and keep the classic red for corrupted characters ([ff1464c](https://github.com/jamesblack/thelastgate-godot/commit/ff1464c4e230d55ae4d0b7e48bf9e8a08ed3aaba))
* **windows:** add an Experimental tab to the options window ([82c5701](https://github.com/jamesblack/thelastgate-godot/commit/82c5701f2b9c9e986c9c2d9fd5ebcdeb473a8c66))
* **windows:** fade only a window's background with the window background option, keeping its frame, lines and buttons solid ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **windows:** give the waypoint list a green scroll bar and scroll it with the wheel only over the list ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **windows:** keep the skill tree centred in a window wide enough for its longest description ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **windows:** label the smith's slots and its Smith, Grind, Hone and Sharpen buttons ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **windows:** light up window buttons and close buttons while the mouse is over them, and more while held ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **windows:** redraw the shop, depot, smith, waypoint, message and skill tree windows in a new dark style ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **windows:** show the depot's current page and the selected waypoint in steel blue ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **world:** keep retrying a use that doesn't go through, as an experimental option ([82c5701](https://github.com/jamesblack/thelastgate-godot/commit/82c5701f2b9c9e986c9c2d9fd5ebcdeb473a8c66))
* **world:** mark a use that is waiting to be retried in red ([82c5701](https://github.com/jamesblack/thelastgate-godot/commit/82c5701f2b9c9e986c9c2d9fd5ebcdeb473a8c66))


### Bug Fixes

* **hud:** keep label colours inside themed windows and the classic fonts intact after a font swap ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))
* **planner:** match the server's new Hermit, Strength Reversed and Hanged Man Reversed values ([e6b0ae8](https://github.com/jamesblack/thelastgate-godot/commit/e6b0ae8b59d86abc91936d2a6a0f29a1aef771a8)), closes [#41](https://github.com/jamesblack/thelastgate-godot/issues/41)
* **windows:** call waypoint 22 The Emerald Cavern, West, as the server does ([e6b0ae8](https://github.com/jamesblack/thelastgate-godot/commit/e6b0ae8b59d86abc91936d2a6a0f29a1aef771a8))
* **windows:** keep the new player welcome above a tutorial so its buttons do what they say ([68dbf13](https://github.com/jamesblack/thelastgate-godot/commit/68dbf1357cd0bb72882dbd4f29d5500858515974))

## [1.3.0](https://github.com/jamesblack/thelastgate-godot/compare/v1.2.0...v1.3.0) (2026-11-21)


### Features

* **display:** add a Display Toggles section with a more obvious hole traps option ([42a3d46](https://github.com/jamesblack/thelastgate-godot/commit/42a3d4645894ca306be13b9eb0f5fdf7aa61b4ae))
* **display:** let each window's background be made see-through ([42a3d46](https://github.com/jamesblack/thelastgate-godot/commit/42a3d4645894ca306be13b9eb0f5fdf7aa61b4ae))
* **display:** option to draw infravision in normal colours instead of red ([3a32303](https://github.com/jamesblack/thelastgate-godot/commit/3a323032eece8ee5f2c689c6a63795fbbe361ccd))
* **windows:** drag the waypoint window's scrollbar, like the inventory and skill list ([ee08b98](https://github.com/jamesblack/thelastgate-godot/commit/ee08b98c8d8847a58bb6dcef90b1fc4d908d5418))
* **windows:** move through the waypoint list with the arrow keys and travel with Enter ([ee08b98](https://github.com/jamesblack/thelastgate-godot/commit/ee08b98c8d8847a58bb6dcef90b1fc4d908d5418))
* **windows:** reset each options tab to its defaults on its own ([62ab1f7](https://github.com/jamesblack/thelastgate-godot/commit/62ab1f75056ba967167f273ec50b2e7d0201e421))
* **windows:** restyle the options window to match the game's dark windows ([62ab1f7](https://github.com/jamesblack/thelastgate-godot/commit/62ab1f75056ba967167f273ec50b2e7d0201e421))
* **windows:** show what each currency earned and spent this session in a stats window ([1c70388](https://github.com/jamesblack/thelastgate-godot/commit/1c70388dd99dac516447120c37da4fa753de670b))
* **windows:** type in the waypoint window to find a waypoint ([ee08b98](https://github.com/jamesblack/thelastgate-godot/commit/ee08b98c8d8847a58bb6dcef90b1fc4d908d5418))
* **world:** queue every use and pickup in a dragged area with the Area Use hotkey ([f6035f7](https://github.com/jamesblack/thelastgate-godot/commit/f6035f771c65f6959ec28d85ad14414bf4bc6339))


### Bug Fixes

* **windows:** call the text window background setting Tutorial / MOTD ([68893e6](https://github.com/jamesblack/thelastgate-godot/commit/68893e6d800f8079447b9f3afa105469f206a958))
* **windows:** make the options window text brighter and easier to read ([68893e6](https://github.com/jamesblack/thelastgate-godot/commit/68893e6d800f8079447b9f3afa105469f206a958))
* **windows:** move through the waypoint search results with the arrow keys again ([c7dc1ac](https://github.com/jamesblack/thelastgate-godot/commit/c7dc1acd48479bf2f9361973fd9cda7261fe3921))
* **windows:** rename the Display Toggles section to Accessibility ([68893e6](https://github.com/jamesblack/thelastgate-godot/commit/68893e6d800f8079447b9f3afa105469f206a958))
* **windows:** reword the glide movement option ([68893e6](https://github.com/jamesblack/thelastgate-godot/commit/68893e6d800f8079447b9f3afa105469f206a958))

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
