## 1. Board Size Foundations

- [x] 1.1 Add a pure board-selection helper that computes the picker's maximum square size from the number of tile designs and validates a width × height selection (even cell count, at least 4 cells, within the pair limit), returning a reason when invalid.
- [x] 1.2 Change `GameManager.GenerateMemoryGrid` to take columns and rows, centre tiles on both axes, and replace `boardSize` with column and row counts in generation, framing, and presentation helpers.
- [x] 1.3 Remove the temporary random startup board and expose readiness (ready/failed), the maximum board size, and `StartGame(columns, rows)` from `GameManager`.
- [x] 1.4 Add EditMode tests for the board-selection helper and for framing of wide and tall boards.

## 2. Pause, Abandon and Completion Hand-off

- [x] 2.1 Add `SetPaused` to `GameManager`: ignore board input, drop half-finished gestures, and freeze the `Animate`/`Wait` clocks so in-flight sequences resume exactly on continue.
- [x] 2.2 Add `AbandonGame()` that cancels active sequences without confirming pending matches or raising completion, clears the board, and resets the match flow.
- [x] 2.3 Arm board input after `StartGame` only once all pointers have been released, so the picker release cannot also select a tile.
- [x] 2.4 Add the `Board/Pause` action bound to Escape (covering Android back) through the Input Actions editor workflow, and route it to the menu controller.

## 3. Settings

- [x] 3.1 Add a `GameSettings` class that stores vibration (int) and sound/music volume (float 0–1) as primitive preference keys with defaults, applies changes immediately, and raises a change event.
- [x] 3.2 Make blocked-input haptics read the vibration setting, keeping the serialized flag as the fresh-install default.
- [x] 3.3 Add EditMode tests for settings defaults, clamping, and round-tripping through storage.

## 4. Menu Flow and Screens

- [x] 4.1 Add a plain `MenuFlow` state model (Loading, Title, Settings, Picker, Playing, Paused) with valid transitions and EditMode tests.
- [x] 4.2 Add `MenuController` that maps menu states to screen visibility, starts/pauses/abandons games through `GameManager`, and reacts to board completion by clearing the board and returning to the title.
- [x] 4.3 Implement the title presentation: normal and success title sprites (defaulting to `weknow_title_normal.png` / `weknow_title_success.png`, swapped in place with preserved aspect), text fallback when a sprite is unassigned, and the configurable success-title duration before reverting.
- [x] 4.4 Implement `GridSizePicker`: an N×N cell grid sized to the safe area, press-and-drag rectangle selection from the press cell, valid/invalid highlighting, an invalid-reason message, back action, and release-to-start for valid selections.
- [x] 4.5 Implement the pause menu (CONTINUE, MAIN MENU) and the in-game pause button, and the settings screen (vibration toggle, sound and music sliders, back).
- [x] 4.6 Implement EXIT: quit on supported platforms, stop Play Mode in the Editor, hide on iOS.
- [x] 4.7 Apply the accent colour (#128580 by default) and the Jost SemiBold font asset from one serialized style setting to buttons, toggle, sliders, labels, and the valid picker selection at runtime.

## 5. Live Menu Background

- [x] 5.1 Implement `TileWallBackground`: spawn a perspective-tilted wall of mostly face-down tiles (as in `weknow_mockup_normal.png`) from the already-loaded tile prefab and data on a non-raycastable layer, with no colliders, shadows, or side quads, a dark backdrop behind it, and enough tiles to cover the whole view at the current aspect ratio (re-checked when the screen size changes).
- [x] 5.2 Add slow drift with wrap-around and idle flips that keep a configurable share of tiles showing their logo, using `TileController.SetFlipAngle` on unscaled time.
- [x] 5.3 Add the celebratory flip waves that spread outward from the bottom-left corner of the screen (configurable origin and sweep time, timed by on-screen tile position rather than grid index) for the success-title duration, and disable the wall entirely during Playing and Paused.

## 6. Scene Setup (Unity Editor workflow)

- [x] 6.1 Through Unity Editor APIs (MCP or by hand), set the import settings for the title and overlay PNGs (sprite, no mipmaps, max size ≥ 2048, high-quality compression), generate the cut-corner button sprite and import it 9-sliced, and pre-populate the Jost font asset with the menu's characters.
- [x] 6.2 Through Unity Editor APIs, build the Screen Space - Overlay menu canvas following `weknow_mockup_normal.png`: a safe-area root, the stretched `weknow_overlay.png`, the title lockup at the top left, PLAY/SETTINGS/EXIT stacked beneath it (470×100 at the 1920×1080 reference, 28 px gaps, Jost SemiBold 38 px, 0.16 em spacing), the picker, settings, and pause screens in the same left column, and the in-game pause button.
- [x] 6.3 Add the background wall root and the `MenuController`, and wire all serialized references, style values (accent colour, font, title sprites), and the `Board/Pause` action reference.
- [x] 6.4 Reopen the scene and confirm every new reference, action, and layer setting survives serialization, that no runtime object references the mockup PNGs or SVGs, and that `OFL.txt` is still next to the Jost font.

## 7. Verification

- [x] 7.1 Run Unity compilation and all EditMode tests; resolve any new errors or warnings.
- [x] 7.2 In Play Mode, validate: launch to title, PLAY to picker, valid and invalid releases, rectangular boards (e.g. 2×3, 3×4, 4×4) framed correctly, pause/continue mid-animation, MAIN MENU abandon without the success title, completion to success title and revert after the configured duration, settings persistence across a restart, EXIT in the Editor.
- [x] 7.3 Compare the title screen against `weknow_mockup_normal.png` and `weknow_mockup_success.png`, and check menu readability, title and overlay placement, button and picker cell sizes, and the moving background at 4:3, 16:9, and wide landscape aspect ratios, including the safe area.
Deferred: physical-device touch validation (picker drag, pause, no stray tile tap after starting a game, vibration on/off, and wall performance) moves to a later change, by decision on 2026-11-13.
