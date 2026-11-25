## Why

The settings sliders are short (320 px on a 1920-wide canvas, 200 px for the inactivity timeout), which makes them fiddly to drag on a phone. Separately, whoever runs the game (for example at a stand) wants to limit players to a few chosen board sizes instead of the free drag grid. The sizes should be picked with the same grid the players already know.

## What Changes

- **Wider settings panel**: the panel grows from 760 to about 1100 px wide, and the control column with it. The volume and inactivity sliders become roughly twice as long. Labels and controls stay on shared columns, and the credits line widens to match.
- **New "Board sizes" setting** with two modes:
  - **Free choice** (default): today's behaviour, the drag grid picker.
  - **Preset sizes**: PLAY opens a "CHOOSE YOUR BOARD" screen with one large button per preset size. Each button has a small grid icon and reads "C × R · N pairs". One tap starts the game, and BACK returns to the title.
- **Preset editor**: an EDIT SIZES button next to the setting opens the grid picker in an edit mode.
  - Every valid drag adds that size to a list shown beside the grid. Duplicates are ignored and the list stays sorted.
  - Each list entry has a remove button. Changes are saved immediately and kept between sessions.
  - BACK returns to Settings. The editor is only available when Settings was opened from the title, not during a paused game.
- **Empty and invalid presets**: the preset list starts empty. While preset mode has no usable sizes, PLAY uses the free grid picker, and the settings screen says so. Presets that are no longer valid (for example, fewer tile designs are available) are skipped on the choice screen and marked in the editor.
- Every new piece of text exists in English and Hungarian.
- The Settings requirement is rewritten to include the battery saver toggle, which already exists in the game but was missing from the spec.

## Capabilities

### New Capabilities
None. The new behaviour belongs to the menu flow.

### Modified Capabilities
- `game-menus`: PLAY now leads to the board-size screen for the current mode. The Settings requirement gains the Board sizes control, the wider sliders, and the battery saver toggle. New requirements cover the board size mode, the preset choice screen, and the preset editor.

## Impact

- **Scenes**: `MainScene`. The Settings panel gets wider and gains a Board sizes row (label, dropdown, EDIT SIZES button, empty-list note). The Picker screen gets a preset buttons panel and an edit-mode list panel.
- **Code**:
  - `GameSettings` (board size mode and preset list, persisted through the existing `ISettingsStorage` ints).
  - `MenuFlow` (a preset editor state reached from Settings).
  - `MenuController` (mode-dependent PLAY, the editor, building preset buttons and list rows).
  - `GridSizePicker` (an edit mode that reports added sizes instead of starting a game).
  - `BoardSelection` (preset ordering and validity helpers).
  - `LocalizedText` (new strings).
- **Tests**: edit-mode tests for preset storage, ordering and validity, menu flow states, and localization keys.
- **Saves**: new PlayerPrefs keys. Existing installs default to free choice with an empty preset list, so nothing changes until someone opts in.
- **Not affected**: gameplay, the free-choice picker's rules, sound, and inactivity behaviour.

## Non-goals

- Password- or PIN-protecting the settings or the preset editor.
- Presets that also choose specific tile designs, a time limit, or difficulty.
- Reordering presets by hand. The list is always sorted.
- A scrolling list for very large preset sets. The editor caps the list at 8 sizes.
