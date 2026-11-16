## 1. Language setting

- [x] 1.1 Add `GameLanguage` enum (English = 0, Hungarian = 1) with each language's native display name ("English", "Magyar")
- [x] 1.2 Add `GameSettings.Language` stored under `settings.language`; default English; out-of-range stored values read as English; setting saves and raises `Changed`
- [x] 1.3 Extend `GameSettingsTests`: default is English, value persists through storage, invalid stored value falls back to English, unchanged value does not raise `Changed`

## 2. String table

- [x] 2.1 Add `LocalizedText` with key constants and English and Hungarian tables using the strings from design.md; `Get` and `Format` fall back to English, and to the key itself (with one Editor log) when the key is unknown
- [x] 2.2 Add EditMode tests: every key has an English entry, every key has a Hungarian entry, fallback works for a missing translation, `Format` substitutes arguments
- [x] 2.3 Change `BoardSelection.Describe` to return a text key; update `BoardSelectionTests` and add a test that every `BoardSelectionResult` maps to a key that exists in both tables

## 3. Applying the language

- [x] 3.1 Add `LocalizedLabel` component (serialized key, `Apply(GameLanguage)` sets the sibling `TMP_Text`)
- [x] 3.2 `MenuController`: collect all `LocalizedLabel`s in the scene including inactive ones, apply the current language on start and on `GameSettings.Changed`
- [x] 3.3 `MenuController`: localize `SetStatus` load-failure text and the inactivity value suffix; leave title labels English
- [x] 3.4 `GridSizePicker`: add `SetLanguage`, look up hint, size readout and rejection messages from `LocalizedText`, re-render the current message on change; remove the serialized `hint` string
- [x] 3.5 `MenuController`: add a `TMP_Dropdown` field, fill options from `GameLanguage` native names, show the current language without notifying, and set `settings.Language` on change

## 4. Tile descriptions

- [x] 4.1 Add `foundDescriptionHungarian` and `GetDescription(GameLanguage)` to `Tile` (blank Hungarian falls back to `foundDescription`), with an EditMode test
- [x] 4.2 Pass the current language from `GameManager` into `TileController.Setup` and use `GetDescription` for the description text and `TileController.Description`
- [x] 4.3 Add Hungarian descriptions to the 8 tile assets in `Assets/ScritptableObjects/ActiveTiles/` (YAML scalar field, GUIDs untouched) and list them for the user's review

## 5. Scene wiring (Unity Editor)

- [x] 5.1 Add a LANGUAGE row with a TMP_Dropdown to the settings screen in `MainScene`, styled like the other settings rows, and assign it to `MenuController`
- [x] 5.2 Add `LocalizedLabel` with the matching key to every static menu label (title-screen buttons, picker heading and back, settings labels and back, pause heading and buttons, match presentation prompt); leave the title text and the "II" pause glyph alone
- [x] 5.3 Add an EditMode test that opens `MainScene` and fails if any `TMP_Text` lacks a `LocalizedLabel` and is not on the allow-list (title, pause glyph, code-driven value/status/picker message texts)

## 6. Validation

- [x] 6.1 Run all EditMode tests in the Unity Test Runner
- [x] 6.2 Play Mode: switch to Magyar on the settings screen and confirm the screen updates immediately; check title, picker (hint, size readout, each rejection message), pause menu, match presentation hint and a Hungarian tile description; confirm the title stays English
- [x] 6.3 Play Mode: restart and confirm Hungarian persists; switch back to English and confirm everything returns
- [x] 6.4 Check Hungarian layouts for overflow at the smallest supported landscape phone aspect and fix sizing in the Editor if needed
- [x] 6.5 On an Android device build, confirm Ő, Ű, ő and ű render in the Jost font; if not, switch the font asset to a static character set that includes Hungarian letters

## 7. Settings layout and pause access

- [x] 7.1 `MenuFlow`: allow `OpenSettings` from `Paused`, return there on `CloseSettings`, and count settings opened from the pause menu as in-game; add `MenuFlowTests` for both routes and the timeout from pause settings
- [x] 7.2 `MenuController`: add the pause-menu SETTINGS button binding and keep the board paused while settings are open from the pause menu
- [x] 7.3 `GameManager` / `TileController`: update the description of every board tile when the language changes during a game
- [x] 7.4 Add the `InactivityHint` string (English and Hungarian)
- [x] 7.5 Scene: reorder settings rows (language first, inactivity last), align labels and controls on shared columns with centred rows, and add the smaller inactivity explanation with a `LocalizedLabel`
- [x] 7.6 Scene: add a SETTINGS button to the pause menu between CONTINUE and MAIN MENU, with a `LocalizedLabel`, and assign it to `MenuController`
- [x] 7.7 Run EditMode tests and check both settings routes in Play Mode
- [x] 7.8 Centre the settings panel when it is opened from the pause menu, keeping its scene position when opened from the title
