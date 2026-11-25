## Context

- Menus are uGUI under one `Menu Canvas` (Screen Space Overlay, `CanvasScaler` with a 1920×1080 reference resolution, match 0.5) and a `SafeArea` root. Each screen (`Title`, `Picker`, `Settings`, `Pause`, `Game HUD`) is a full-stretch child, and its content uses fixed top-left-anchored positions.
- Settings `Panel`: 760×880 at (120, −110). Labels sit at x 60 (300 wide) and controls at x 380 (sliders 320 wide, the inactivity slider 200 plus a 110 value label). Rows are 80 px apart, from −120 (language) to −520 (inactivity). Below them come the hint (−590), Back (−680) and Credits (−795).
- Picker `Panel`: 760×940 at (120, −70), containing the Heading, a 560×560 `Grid` (`GridSizePicker`, which builds its cells at runtime), the Message and Back.
- `MenuFlow` is a pure state model (`Loading/Title/Settings/Picker/Playing/Paused`), and `MenuController` binds buttons and shows screens per state. `GridSizePicker.SelectionConfirmed(columns, rows)` fires on a valid release. `BoardSelection.Validate` holds the rules.
- `GameSettings` persists primitives through `ISettingsStorage` (ints/floats, `PlayerPrefs` in the game, a fake in tests) and raises `Changed`.
- `LocalizedText` is a static EN/HU table. `LocalizedLabel` applies keys to static labels, and the scene-localization edit-mode test checks every label key exists.

See proposal.md for motivation and specs/game-menus/spec.md for required behaviour.

## Goals / Non-Goals

**Goals:**
- Reuse the existing picker, button style, dropdown, and hint text style, so the new UI looks native.
- Keep the new rules (preset ordering, validity, dedupe, cap, persistence) in plain C# with edit-mode tests.
- Keep scene edits few and predictable. Repeated items (preset buttons, editor rows, grid icons) are built at runtime from one template each.

**Non-Goals:**
- Responsive layout. The panels stay fixed-size on the 1920×1080 reference, like today.
- Changing free-choice behaviour or the picker's validation messages.

## Decisions

### 1. Wider settings panel, same rows
The panel becomes 1100 px wide; height and position are unchanged except where row 2 is added (see 2).
- **Labels:** x 60, 300 wide.
- **Controls:** start at x 380 and extend to x 1040, 660 wide.
  - Sound and music sliders: 660 wide.
  - Inactivity slider: 520 wide, with its value label at x 920 (120 wide).
  - Language dropdown: 400 wide.
- **Hint:** 980 wide.
- **Back button:** centred at x 315.
- **Credits:** x 40, 1020 wide.

The heading and panel background widen to 1100. The panel's 120 px left offset is kept. 120 + 1100 = 1220 still fits the 1920 reference width, and the title art is hidden while Settings is shown.
- *Alternative*: scale the whole panel up. Rejected: it makes the text bigger without making the sliders longer relative to the screen.

### 2. Board sizes row in Settings
A new row 2, "BOARD SIZES", has the label at x 60, a dropdown with two options at x 380 (400 wide, a copy of the language dropdown), and an EDIT SIZES button at x 800 (240×60, a smaller copy of the Back button style). The empty-list note ("Players choose freely until you add sizes") uses the hint style below the row. It is shown only in preset mode with no usable presets.

To fit the extra row (80 px) and the note (40 px) without leaving the 1080 reference height, the row pitch drops from 80 to 72 px and the panel grows to 960 tall. Its top edge stays at −110, so its bottom is at −1070. The pause-menu centring already recomputes from the panel size.
- **Rows:** language −110, board sizes −182, note −238 (32 tall), vibration −278, battery −350, sound −422, music −494, inactivity −566.
- **Below the rows:** hint −632, Back −712, Credits −830.

EDIT SIZES is non-interactable (dimmed) when Settings was opened from the pause menu (`MenuFlow` exposes where Settings returns to).
- *Alternative*: a two-option segmented toggle. Rejected: it would need a new control style, and the dropdown already exists and is localized.

### 3. Data model: `BoardPresets` (plain C#)
- `BoardSize` is a readonly struct (columns, rows) with `Pairs`.
- `BoardPresets` holds a sorted, de-duplicated list, capped at `MaxPresets = 8`.
  - `TryAdd(size, out AddResult)`, where `AddResult` is `Added / Duplicate / Full / Invalid`.
  - `Remove(size)`.
  - `Usable(designCount)` filters with `BoardSelection.Validate`.
  - The sort order is pairs ascending, then columns ascending.

`GameSettings` gains `BoardSizeMode` (`FreeChoice = 0`, `Presets = 1`, key `settings.boardSizeMode`) and the preset list, stored as `settings.boardPresets.count` plus `settings.boardPresets.{i}` = `columns * 100 + rows`. Mutations save at once and raise `Changed`. Malformed stored entries (out of range, duplicates, beyond the cap) are dropped on load.
- *Alternative*: a single delimited string key. Rejected: `ISettingsStorage` has no string API, and adding one touches the existing test fakes for little gain.

### 4. Menu flow
`MenuState` gains `PresetEditor`.
- `MenuFlow.OpenPresetEditor()` is allowed only from `Settings` when Settings returns to `Title`.
- `ClosePresetEditor()` returns to `Settings`, keeping the Title return.
- `IsInGame` is unchanged, so the inactivity timeout doesn't apply in the editor.

PLAY keeps going to `MenuState.Picker`, and `MenuController` decides which panel to show from the effective mode: `Presets` with at least one usable preset shows the preset panel, otherwise the grid. Platform back in `PresetEditor` calls `ClosePresetEditor`.
- *Alternative*: a separate `PresetChoice` state. Rejected: it behaves exactly like `Picker` (same back target, same game start), so it would duplicate transitions.

### 5. Picker screen panels
- **Grid panel (existing):** used for free choice and for the editor. In edit mode the heading switches to "EDIT BOARD SIZES", and `MenuController` handles `SelectionConfirmed` by calling `TryAdd` instead of starting a game.
- **`GridSizePicker` additions:**
  - `ShowMessage(string)`, so the controller can report Duplicate/Full on the existing Message line.
  - `ResetSelection()` after each add, so the next drag starts fresh.
- **Preset list panel (new):** 560×940 at (900, −70), shown only in the editor. It contains the heading "SIZES" and a vertical stack of up to 8 rows, 88 px apart. Each row has a grid icon, the text "C × R · N pairs", and a small "✕" remove button. Unusable entries show at 45 % alpha with "(not available)". An empty list shows "Drag on the grid to add a size".
- **Preset buttons panel (new):** occupies the grid panel's place (760 wide) in preset choice mode, which hides the Grid and Message. Buttons are 330×96 in two columns, up to 4 rows, starting at y −120. BACK stays at its current position.
- **Grid icon:** a small `GridLayoutGroup` of cell images built from the picker's `cellSprite`, scaled to a 64×64 box with the board's aspect ratio.

Rows and buttons are cloned at runtime from a single inactive template child in each panel. The scene gets the panels and templates once; the code fills them.

### 6. Text
New `LocalizedText` keys, in EN and HU:
- `BoardSizes`: label "BOARD SIZES".
- `FreeChoice` / `PresetSizes`: the two dropdown options.
- `EditSizes`: the "EDIT SIZES" button.
- `NoPresetsNote`.
- `EditBoardSizes`: the editor heading.
- `PresetListHeading`: "SIZES".
- `PresetListEmpty`.
- `PresetDuplicate`, `PresetListFull`.
- `PresetUnavailable`.
- `PresetButton`: format "{0} × {1} · {2} pairs", reusing the existing `PickerSize` wording.

The dropdown options are relabelled in code on a language change, as the language dropdown is handled today.

### 7. Sounds
These reuse existing cues; no new cue is needed:
- Preset button: `BoardStart`.
- EDIT SIZES: `ButtonClick`.
- Editor add: `ButtonClick`.
- Remove: `ButtonBack`.
- Board size dropdown: `DropdownSelect`.

### 8. Scene editing
`MainScene` changes are made in the Unity Editor (or, if the Editor isn't available, by careful YAML edits that keep GUIDs and fileIDs, as in recent changes):
- resize and reposition the settings panel and its children;
- duplicate the language row as the board-size row;
- add the EDIT SIZES button and the empty-list note;
- add the two picker panels with one template each;
- wire the new `MenuController` fields.

The new `MenuController` fields are optional at runtime (null-checked), so the game keeps working until the scene is wired.

### 9. Panels fit short screens
With `CanvasScaler` matching 0.5, a 20:9 phone's canvas is only about 966 units tall, so the fixed 1080-tall layout runs off the bottom. This affected the Picker panel even before this change.

`MenuController.FitPanel` keeps a panel at its scene position when it fits. Otherwise it moves the panel up to a 24-unit margin and scales it down, never below half size, until it fits the safe area. Centred panels (Settings over the pause menu) only scale.
- The Settings panel is fitted when it is placed.
- The picker panel and the preset list are fitted together, so they stay side by side at the same scale.
- Both are fitted again whenever the safe area or screen size changes.

Preset button and list labels use a compact `PresetSize` format ("4 × 1 · 2 pairs") with no extra letter spacing.

## Risks / Trade-offs

- [Taller settings panel nearly fills the 1080 reference height; on wide phones the safe area may clip the credits] → The 72 px pitch keeps a 10 px margin. Validation checks at 16:9 and 20:9 in the Game view and on a device, and the credits auto-size already shrinks text before clipping.
- [Two-column preset buttons crowd the screen at 8 presets] → They fit by construction (4 rows × 96 px + gaps < 560 px grid area). The cap enforces it.
- [Adding a preset by dragging is less obvious than a "+" button] → The editor heading and the empty-list text explain it, and the Message line confirms each add ("Added 3 × 4").
- [Presets become unusable if tile designs are removed later] → They are filtered with `Usable(designCount)`, marked in the editor, and the game falls back to free choice when none are usable.
- [Hand-editing the scene YAML for many UI objects is error-prone] → Prefer doing it in the Editor. If done in YAML, validate references and IDs with a script, as for the credits line.

## Migration Plan

Additive. Existing players load `FreeChoice` with an empty list, so nothing changes until someone switches modes. Rollback: revert the commit. The unused PlayerPrefs keys are harmless.
