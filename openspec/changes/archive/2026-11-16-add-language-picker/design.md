## Context

See proposal.md for the motivation. Current state:

- `GameSettings` is a plain C# class over `ISettingsStorage` (PlayerPrefs in the game, a fake in `GameSettingsTests`). `MenuController` creates it in `Awake` and hands it to `GameManager.UseSettings`. Each change is saved and raises `Changed`. The project has no static singletons; `MenuController` finds `GameManager` with `FindFirstObjectByType`.
- Player-facing text comes from three places:
  - static TMP labels typed into `MainScene.unity` (PLAY, SETTINGS, BACK, PAUSED, …),
  - serialized or hard-coded strings in code (`GridSizePicker.hint`, the size readout in `GridSizePicker`, `BoardSelection.Describe`, `MenuController.SetStatus`, the `"{n} s"` inactivity suffix),
  - `Tile.foundDescription`, written into the tile prefab's `DescriptionText` by `TileController.Setup`.
- `MenuController` already walks every `TMP_Text` under itself to apply `MenuStyle.font`.
- The Jost SDF font asset uses dynamic atlas population from `Jost-600-Semi.ttf`, so glyphs are added at runtime from the TTF.

## Goals / Non-Goals

**Goals:**
- A single place that answers "what is this text in the current language", which can be tested in EditMode.
- Changing the language updates everything immediately, with no scene reload.
- Adding a third language later means adding an enum value, a table column and a dropdown entry, with no structural change.

**Non-Goals:**
- Plural rules, right-to-left text, or per-language fonts.
- Letting non-programmers edit UI strings outside the code (tile descriptions stay editable in their assets).

## Decisions

### 1. Language is a `GameSettings` value
Add `enum GameLanguage { English = 0, Hungarian = 1 }` and a `GameSettings.Language` property stored as an int under `settings.language`. Reading an out-of-range value falls back to English, the same way the other properties clamp. Setting it saves and raises `Changed`, like the other properties.
*Why:* this reuses the existing persistence path and test setup. *Alternative:* a separate PlayerPrefs key read by a localization singleton. Rejected because it would duplicate the storage pattern and add the project's first singleton.

### 2. Strings live in a code string table
A static `LocalizedText` class holds constant keys (for example `LocalizedText.Play`) and a table per language: `Get(GameLanguage, key)` and `Format(GameLanguage, key, args)`. A missing translation returns the English entry. A missing key returns the key and logs once in the Editor.
*Why:* there are about 25 strings and two languages. A code table needs no asset loading, can't fall out of sync with the keys, and can be fully covered by EditMode tests. *Alternatives:* Unity Localization package (heavy setup, Addressables string tables and a new dependency for 25 strings), or a ScriptableObject/CSV table (editable by non-coders, but it needs loading and its keys can drift). Either could replace the table later behind the same `Get` API.

### 3. Static scene labels use a `LocalizedLabel` component
`LocalizedLabel : MonoBehaviour` sits next to a `TMP_Text`, has a serialized `key` and an `Apply(GameLanguage)` method that sets the text. `MenuController` collects every `LocalizedLabel` in the scene with `FindObjectsByType(FindObjectsInactive.Include, …)` once in `Awake`, so labels on inactive screens and outside its hierarchy (the match presentation's prompt) are included. It applies the current language on start and again on `GameSettings.Changed`.
*Why:* labels stay dumb and need no reference to settings. One owner drives the refresh, which matches how `MenuController` already applies the font. *Alternative:* each label subscribes to a static event. Rejected because that requires a static.

### 4. Dynamic text reads the language when it is shown
- `GridSizePicker` gets a `SetLanguage(GameLanguage)` method called by `MenuController`. Its hint, size readout (`"{0} × {1}  ·  {2} pairs"`) and rejection messages are looked up from `LocalizedText`. It re-renders the current message when the language changes. The serialized `hint` field is removed in favour of the key.
- `BoardSelection.Describe(result)` returns a key instead of English text, so it stays a pure function. A test checks every result maps to a key that exists in both tables.
- `MenuController.SetStatus` and `RefreshInactivityLabel` use `LocalizedText`. The title labels are left as they are (English, serialized).

### 5. Tile descriptions: one extra field and a lookup
Add `public string foundDescriptionHungarian;` to `Tile` and `public string GetDescription(GameLanguage)`, which returns the Hungarian text when it is set and not blank, and `foundDescription` otherwise. The existing field keeps its name, so the 8 tile assets keep their serialized data. `GameManager` reads `settings.Language` (it already holds `GameSettings`) and passes it to `TileController.Setup`. Settings can be opened from the pause menu, so the language can change during a game: `GameManager` listens to `GameSettings.Changed` and calls `TileController.SetLanguage` on every board tile, which rewrites its description text.
*Alternative:* a per-language array of descriptions. Rejected for now because a named field reads clearly in the Inspector with two languages. Revisit if a third language arrives.

### 6. Dropdown
A `TMP_Dropdown` field on `MenuController`, wired in the scene. `BindSettingsControls` clears its options and fills them from the `GameLanguage` values, using each language's own name ("English", "Magyar") so it reads the same in either language. It then calls `SetValueWithoutNotify((int)settings.Language)`, and `onValueChanged` sets `settings.Language`. The dropdown's template labels are inside `MenuController`'s hierarchy, so they pick up the menu font and accent styling automatically.

### 8. Settings from the pause menu
`MenuFlow` keeps a single `Settings` state and remembers where it was opened from (title or pause). `OpenSettings` is allowed from `Title` and `Paused`; `CloseSettings` returns to that state. `IsInGame` is true for settings opened from the pause menu, so the inactivity timeout keeps running, the board stays paused (no `SetPaused(false)` call), the menu background stays hidden, and the back/escape action returns to the pause menu. Opened from the pause menu, the settings panel is centred on screen like the pause panel; opened from the title it keeps its scene position next to the title. `MenuController` caches the panel's scene anchors and switches between the two placements when it shows the screen.
*Alternative:* a separate `PausedSettings` state. Rejected because every place that shows the settings screen or handles its back action would need both states.

### 9. Settings layout
All rows share a label column (left, vertically centred text) and a control column of the same left and right edge, with a fixed row pitch; each control is centred on its row. Order: LANGUAGE, VIBRATION, SOUND, MUSIC, INACTIVITY, then the explanation line (`InactivityHint`, about two-thirds of the label size, muted colour) spanning both columns. The layout is applied by an Editor script through the Unity MCP server and saved into the scene.

### 7. Hungarian glyphs
Keep the dynamic font asset. Jost includes Latin Extended-A (ő, ű). Verify on device that capitals Ő and Ű render. If the dynamic atlas misses them on a platform, switch the asset to include those characters statically in the Editor, with the TMP Font Asset Creator using a custom character list. That doesn't change any code. The font asset is already modified in the working tree; this change won't touch it unless that check fails.

### Unity serialization edits
AGENTS.md allows direct YAML edits that preserve GUIDs and fileIDs. Even so, the scene work (adding the dropdown, adding `LocalizedLabel` to about 18 labels, wiring the dropdown field) is risky to do by hand in a 40k-line scene. It is planned as Editor tasks, or through the Unity MCP server when it is connected. The tile description text is a single scalar field per asset, so it can be added safely as a YAML edit to the 8 tile `.asset` files.

### Draft Hungarian strings (for review)

| Key | English | Hungarian |
|---|---|---|
| Play | PLAY | JÁTÉK |
| Settings | SETTINGS | BEÁLLÍTÁSOK |
| Exit | EXIT | KILÉPÉS |
| Back | BACK | VISSZA |
| ChooseBoard | CHOOSE YOUR BOARD | VÁLASSZ TÁBLÁT |
| PickerHint | Drag across the grid to choose your board | Húzd végig az ujjad a rácson a tábla méretezéséhez |
| PickerSize | {0} × {1}  ·  {2} pairs | {0} × {1}  ·  {2} pár |
| TooSmall | Too small: pick at least 4 tiles | Túl kicsi: válassz legalább 4 csempét |
| OddCount | Pick an even number of tiles | Páros számú csempét válassz |
| TooManyPairs | Not enough tile designs for that many pairs | Nincs elég csempe mintánk ennyi párhoz |
| OutOfGrid | Pick a board inside the grid | Maradj a rácson belül |
| Paused | PAUSED | SZÜNET |
| Continue | CONTINUE | FOLYTATÁS |
| MainMenu | MAIN MENU | FŐMENÜ |
| Vibration | VIBRATION | REZGÉS |
| Sound | SOUND | HANG |
| Music | MUSIC | ZENE |
| Inactivity | INACTIVITY | TÉTLENSÉG |
| Language | LANGUAGE | NYELV |
| SecondsValue | {0} s | {0} mp |
| TapToContinue | Tap anywhere to continue | Koppints bárhová a folytatáshoz |
| LoadFailed | The game could not be loaded. | A játékot nem sikerült betölteni. |
| InactivityHint | If nobody touches the game for this long, it returns to the title screen. | Ha ennyi ideig senki nem érinti meg a játékot, visszatér a kezdőképernyőre. |

The pause button glyph "II" is not text and stays unlocalized.

## Risks / Trade-offs

- [A scene label is missed and stays English] → An Editor validation test loads `MainScene` and asserts that every active-text `TMP_Text` either has a `LocalizedLabel` or is on an explicit allow-list (title, pause glyph, code-driven values).
- [Hungarian strings are longer, for example BEÁLLÍTÁSOK vs SETTINGS, and may overflow buttons] → Check every screen in Hungarian at the smallest supported phone aspect. Use TMP auto-size or wider buttons in the Editor if needed.
- [The dynamic atlas fails to add Ő/Ű on a platform] → Fall back to the static character list (Decision 7), and include a device check in the tasks.
- [A code string table means translators edit C#] → Acceptable for two languages. The `Get` API lets a data-driven table replace it later.

## Migration Plan

There is no data migration. Existing players have no `settings.language` key, so they get English, which matches today's behaviour. Rolling back drops one unused PlayerPrefs key and one unused Tile field.
