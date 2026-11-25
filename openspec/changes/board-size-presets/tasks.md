## 1. Preset data and settings

- [x] 1.1 Add `BoardSize` and `BoardPresets` (sorted by pairs then columns, de-duplicated, capped at 8, `TryAdd` with `Added/Duplicate/Full/Invalid`, `Remove`, `Usable(designCount)`)
- [x] 1.2 Extend `GameSettings` with `BoardSizeMode` (default free choice) and the preset list, persisted as `settings.boardSizeMode`, `settings.boardPresets.count` and `settings.boardPresets.{i}` (`columns * 100 + rows`). Saves immediately, raises `Changed`, and drops malformed entries on load
- [x] 1.3 Add edit-mode tests: ordering, duplicates, cap, usable filtering, persistence round-trip, malformed stored entries, and the default (free choice, empty)

## 2. Menu flow

- [x] 2.1 Add `MenuState.PresetEditor` with `OpenPresetEditor` (only from Settings opened from the title) and `ClosePresetEditor` (back to Settings, keeping the title return). Expose whether Settings was opened from the title
- [x] 2.2 Extend `MenuFlowTests` for the editor transitions, including that it can't open from the pause menu's Settings and that inactivity doesn't apply

## 3. Text

- [x] 3.1 Add the new `LocalizedText` keys in English and Hungarian (board sizes label, the two mode options, EDIT SIZES, empty-list note, editor heading, list heading, empty list, duplicate, full, unavailable, preset button format) and register them in `AllKeys`

## 4. Picker and controller

- [x] 4.1 `GridSizePicker`: add `ShowMessage(string)` for controller messages, and keep the rest of its behaviour unchanged
- [x] 4.2 `MenuController`: add optional fields for the board-size dropdown, EDIT SIZES button, empty-list note, preset buttons panel and template, preset list panel and row template, and picker heading. All are null-checked
- [x] 4.3 `MenuController`: bind the board-size dropdown to `GameSettings`, relabel its options on a language change, show the empty-list note, and make EDIT SIZES interactable only when Settings came from the title
- [x] 4.4 `MenuController`: on PLAY, show the preset buttons panel when the mode is presets and at least one preset is usable, otherwise the grid. Build one button per usable preset, each with a grid icon and localized text, starting the game on tap with no stray tile selection
- [x] 4.5 `MenuController`: the editor state shows the grid with the "EDIT BOARD SIZES" heading and the list panel. A valid release calls `TryAdd` and reports Added/Duplicate/Full. Rows show size, pairs and unavailable marking, each with a remove button. BACK and the platform back action return to Settings
- [x] 4.6 Play the existing sound cues for preset taps, EDIT SIZES, add, remove and the dropdown

## 5. Scene (Unity Editor, or verified YAML edits)

- [x] 5.1 Widen the Settings panel to 1100 and re-lay its rows at a 72 px pitch with the new Board sizes row and note, following the design's coordinates. Widen the sliders, hint, heading and credits
- [x] 5.2 Add the EDIT SIZES button and the board-size dropdown (copied from the language dropdown), and wire them to `MenuController`
- [x] 5.3 Add the preset buttons panel and the preset list panel to the Picker screen, each with one inactive template, and wire them to `MenuController`
- [x] 5.4 Check scene references and IDs (no missing references, no duplicate fileIDs) and keep the scene-localization test green
- [x] 5.5 Fit the Settings and picker panels to short screens (scale down and move up within the safe area) and use the compact preset label without letter spacing
- [x] 5.6 With exactly one usable preset, PLAY starts that board directly; size labels use compact letter spacing in code; the picker panel falls back to the grid's parent when not wired

## 6. Validation (Unity Editor / device)

- [ ] 6.1 Run all edit-mode tests in the Test Runner
- [ ] 6.2 In play mode, check the wider Settings layout at 16:9 and 20:9: rows aligned, sliders easy to drag, note and credits visible, and the pause-menu Settings centred and not clipped
- [ ] 6.3 Free choice: PLAY still opens the grid and behaves as before
- [ ] 6.4 Preset mode: switch to presets with an empty list (note shown, PLAY opens the grid), add sizes in the editor (sorted, duplicates and the full list rejected, remove works), then PLAY shows the buttons in order and a tap starts the right board; with a single preset, PLAY starts it directly
- [ ] 6.5 Restart the game: the mode and presets persist. Open Settings from the pause menu: EDIT SIZES is disabled, and a mode change applies to the next board
- [ ] 6.6 Check the Hungarian text on all new UI and on a phone
