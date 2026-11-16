## Why

The game is played by Hungarian and English speakers, but every menu label, message and tile description is English-only. Players need to switch the game to Hungarian from the settings screen.

## What Changes

- Add a **Language** setting with two options, English and Hungarian. It defaults to English, is saved between sessions like the other settings, and applies as soon as it is changed.
- Add a language dropdown to the settings screen, labelled in each language's own name ("English", "Magyar").
- Show every player-facing text in the chosen language: menu headings and buttons, settings labels, the board picker's prompt and error messages, the pause menu, the match presentation's "tap to continue" hint, the load-failure message, and the inactivity value suffix.
- Give each tile a Hungarian description next to the English one. If a tile has no Hungarian description, the English one is shown.
- The game title ("We Know... Do You?" and the success title) stays English in both languages, as does title artwork.
- Reorder the settings screen: language first, then vibration, sound, music, and inactivity last, with a short explanation in smaller text below the inactivity timeout. Line up all labels and controls on shared columns.
- Add SETTINGS to the pause menu. Its back action returns to the pause menu with the game still paused. Because the language can now change mid-game, tile descriptions on the board update immediately.
- Make sure the menu font can show Hungarian accented letters (á, é, í, ó, ö, ő, ú, ü, ű and their capitals).

Non-goals:
- No other languages yet. The design leaves room for more, but only English and Hungarian ship.
- No automatic language detection from the device locale. The default is always English.
- No Unity Localization package or external translation tooling.
- No translation of editor-only text, tooltips, logs or developer error messages.
- No audio or image localization.

## Capabilities

### New Capabilities
- `localization`: choosing the display language, which text follows it and which stays fixed, the English fallback, and glyph coverage for Hungarian.

### Modified Capabilities
- `game-menus`: Settings gains the language dropdown, the new order, the inactivity explanation and access from the pause menu; Pause menu gains SETTINGS; Inactivity return also covers settings opened from the pause menu.

## Impact

- **Scripts**: `GameSettings` (new persisted language value), `MenuFlow` (settings reachable from the pause menu), `MenuController` (dropdown binding, re-applying text on language change, pause-menu SETTINGS button), `GridSizePicker` and `BoardSelection` (messages come from the string table), `MatchPresentationView` / `TileController` (description picked by language), `Tile` ScriptableObject (new Hungarian description field). New string-table and localized-label scripts.
- **Scene / prefabs**: `MainScene.unity` settings screen gets a TMP dropdown, a reordered and realigned layout and an inactivity explanation; the pause menu gets a SETTINGS button; and the existing static labels get a localized-label component with a text key. The tile prefab's description text follows the language.
- **Assets**: the 8 tile assets under `Assets/ScritptableObjects/ActiveTiles/` get Hungarian descriptions. The Jost SDF font asset is dynamic, so the Hungarian glyphs come from the source TTF; this needs checking on device.
- **Tests**: new EditMode tests for the language setting and the string table; existing `GameSettingsTests` extended.
- **Dependencies**: none added.
