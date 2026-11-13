## Context

See `proposal.md` for motivation and `specs/` for the behavior contract.

`GameManager` currently loads the Addressable tile prefab and tile data, then immediately generates a random square board (`GenerateMemoryGrid(Random.Range(1, 3) * 2)`). It owns board generation, perspective framing (`BoardFraming.Fit` already takes a 2D board extent), board input through the `Board` action map, and the match flow. `MatchPresentationView` lives on a Screen Space - Camera canvas that sits between the board and presented tiles. Animations run as coroutines on unscaled time. `TileController` already exposes `Setup`, `CaptureBoardPose`, and `SetFlipAngle`, which are enough to drive tiles outside a game.

The game is touch-first, landscape, and must stay compact: one orchestrator for the match, no global singletons, Addressables as the only asset-loading path.

## Goals / Non-Goals

**Goals:**

- One small menu orchestrator with an explicit screen state, kept separate from the match flow but able to start, pause, abandon, and receive completion from it.
- Width × height boards end to end: generation, framing, and the picker.
- Pause that freezes both input and in-flight match animations without touching `Time.timeScale`.
- A cheap, reusable live background built from the real tile prefab.
- Configuration (accent colour, success-title duration, optional title artwork) editable in the Inspector.

**Non-Goals:**

- Inactivity reset, leaderboard, audio playback, localization, portrait layouts, final artwork.
- A generic UI or screen-navigation framework.
- Changing match rules or the correct-match presentation.

## Decisions

### 1. A `MenuController` with a plain `MenuFlow` state model

A new `MenuController` MonoBehaviour owns the screen state and the menu canvas. Its states are equivalent to:

```text
Loading → Title ⇄ Settings
          Title → Picker → Playing ⇄ Paused
          Playing → Title (completed, success title)
          Paused  → Title (abandoned)
```

Transitions live in a plain `MenuFlow` class (no Unity types), like `MatchFlow`, so they can be unit-tested. `MenuController` maps states to screen visibility and calls into `GameManager`.

`GameManager` stops generating a board on its own. It exposes a narrow API: `IsReady` / a ready-or-failed event after Addressables setup, `MaxBoardSize` (derived from the loaded tile data), `StartGame(int columns, int rows)`, `SetPaused(bool)`, `AbandonGame()`, and the existing `BoardCompleted` event. Completion still fires exactly once; `MenuController` reacts to it by clearing the board through `AbandonGame()` semantics without counting it as abandoned, and shows the success title.

**Alternatives considered:** Putting menu states into `GameManager` would grow an already central class with unrelated UI concerns. Separate scenes per menu add scene loading and asset lifetime handling that this small game does not need.

### 2. Width × height boards

`GenerateMemoryGrid(int size)` becomes `GenerateMemoryGrid(int columns, int rows)`. Validation requires columns, rows ≥ 1, an even cell count, at least 4 cells, and no more pairs than loaded tile designs. Tiles are centred using separate column and row offsets. Framing passes `(columns × stepX − spacing, rows × stepY − spacing)` plus padding to the existing `BoardFraming.Fit`, which already fits both axes. `boardSize` is replaced by `boardColumns` and `boardRows`.

The picker's maximum is the largest square `N × N` with `N² ≤ 2 × designs` (4×4 for 8 designs). Larger rectangles that fit the pair limit but not the square (for example 2×8) are not offered, which keeps the picker a simple square grid.

### 3. Grid picker as a uGUI grid driven by the EventSystem

The picker is a `GridSizePicker` component on the menu canvas: an `N × N` grid of cell images in a `GridLayoutGroup`, sized to fit the safe area. It implements `IPointerDownHandler`, `IDragHandler`, and `IPointerUpHandler` on the grid container and maps pointer positions to cells with `RectTransformUtility`. The selection is the rectangle between the press cell and the current cell, clamped to the grid. Valid selections use the accent colour; invalid ones use a muted or red tint plus a short message line (too small / odd count). Only the first pointer is tracked.

Using the existing `InputSystemUIInputModule` keeps touch, pen, and mouse on the same Input System path as gameplay without new board actions.

To stop the release that starts a game from also tapping a tile, `GameManager.StartGame` arms board input only after every pointer has been released, reusing the existing "no pointer held" check used for continuation.

**Alternatives considered:** Raycasting a 3D preview grid would look closer to the game but duplicates board framing for a menu. Separate +/− steppers for width and height are simpler but lose the drag interaction the design calls for.

### 4. Pause without `Time.timeScale`

`GameManager.SetPaused(true)`:

- makes `OnPrimaryPressed`/`OnPrimaryReleased` ignore board input and drops any half-finished gesture;
- freezes the `Animate` and `Wait` helpers, which only accumulate elapsed time while not paused, so a reveal, mismatch, presentation, or return resumes exactly where it stopped;
- hides the match presentation's continue prompt while paused.

The pause menu canvas sits above everything and blocks raycasts. A small pause button sits in the top corner of the safe area during play. A new `Board/Pause` action bound to `<Keyboard>/escape` covers desktop Escape and the Android back button (delivered as Escape).

`AbandonGame()` cancels the active sequence with `confirmPendingMatch: false` and `raiseCompletion: false`, clears the board, and resets the match flow to `Loading`, so an abandoned game never raises completion.

**Alternatives considered:** `Time.timeScale = 0` does not stop the unscaled-time animations already used for presentation and would also freeze the menu background.

### 5. Settings stored as primitive preferences

A plain `GameSettings` class reads and writes `PlayerPrefs` keys (`settings.vibration` as int, `settings.soundVolume` and `settings.musicVolume` as floats in 0–1), raises a change event, and applies changes immediately. `GameManager` reads the vibration value instead of only its serialized `blockedInputHaptics` flag (the serialized flag stays as the default for a fresh install). Volumes are stored for future audio; nothing plays yet.

This follows the existing persistence guidance: primitive values only, no object references, no new save system.

### 6. Live tile-wall background

A `TileWallBackground` component spawns a wall of tiles from the tile prefab and tile data that `GameManager` already loaded, so no second Addressables load is made. As in the supplied mockup, the wall is tilted in perspective (rotated around all three axes so it recedes towards the upper left and its rows rise slightly to the right), mostly face down, and fills the screen behind the left-hand menu column. The wall root is placed in front of the gameplay camera beyond the presentation depth and set to a non-`Tiles` layer so board raycasts never hit it. A dark unlit backdrop plane sits just behind the tiles so the gaps read as grout rather than sky.

- **Coverage:** the wall sizes itself to the view. It casts a grid of viewport rays onto its tilted plane, takes the lopsided area they hit, adds one spare tile on every side, and spawns just enough columns and rows to cover that area, centred on it (a configurable minimum and maximum apply). It re-checks when the screen size changes and grows if the new view shows more. Typical counts are about 140–340 tiles depending on the aspect ratio.
- **Cheap tiles:** wall tiles drop their colliders, shadows, and the grey side quads (those only hide tile colours from players in a game), leaving four renderers per tile.
- **Drift:** tiles move slowly along one diagonal and wrap to the opposite edge of the grid; the spare tiles keep the wrap off-screen.
- **Idle flips:** the wall keeps a configurable share of tiles showing their logo (`idleRevealedShare`). While fewer are open, it starts new flips at a short randomised interval; each revealed tile holds for a random time, then flips back, so tiles keep turning over.
- **Celebration:** while the success title shows, tiles flip in repeating waves that spread outward from one corner of the screen (`waveOrigin`, bottom-left by default) and take `waveSweepSeconds` to reach the opposite corner. Each tile's delay is its current on-screen distance from that corner, not its grid index, because drifting tiles wrap and index order no longer matches position, and because a wall-space origin would sit off-screen on the tilted wall.
- **Lifecycle:** the wall is active only in menu states and is disabled (renderers off, no updates) during `Playing` and `Paused`. It runs on unscaled time.

Readability comes from the supplied overlay (`weknow_overlay.png`, 1920×1080, transparent) stretched full-screen on the menu canvas between the wall and the menu content. It darkens the left side behind the title and buttons, a top band, and the edges, and leaves the centre-right clear so the wall stays visible.

**Alternatives considered:** A static or video background needs bespoke art and does not reuse the game's look. A separate camera for the wall adds render ordering and lifecycle complexity.

### 7. Menu style, layout, and title artwork

`MenuController` exposes a serialized style block: `accentColor` (default #128580), text and panel colours, the menu font, `successTitleDuration` (seconds, default 4), the two title strings, and `titleSprite` / `successTitleSprite`. The sprites default to the supplied lockups, `weknow_title_normal.png` and `weknow_title_success.png` (2048×640, transparent). The two are aligned so they swap in place, and the success lockup already contains its teal ray burst and tumbling tiles. When a sprite is assigned for the current title state, an `Image` shows it with preserved aspect and the TMP title text is hidden; otherwise the text is shown. Buttons, toggle, sliders, and the picker's valid selection all take their accent from `accentColor` at runtime, so a colour change applies everywhere.

The menu canvas is Screen Space - Overlay with a `CanvasScaler` (scale with screen size, 1920×1080 reference, match 0.5) and a safe-area root, so it draws above the board, the presentation canvas, and the background wall. The title screen follows `weknow_mockup_normal.png`: the title lockup at the top left, PLAY, SETTINGS, and EXIT stacked below it and aligned with the wordmark's left edge, and the wall visible mainly to the centre-right. The picker and settings screens reuse the same left-anchored column, panel style, and buttons. The pause menu uses the same panel and buttons but sits in the centre of the screen over a full-screen dim, since it appears over a board rather than the wall. The mockup PNGs and the SVG exports are reference material only and are never referenced at runtime.

**Buttons** use the mockup spec at the 1920×1080 reference: 470×100 px with 28 px vertical gaps, `accentColor` fill, white Jost SemiBold label at 38 px with 0.16 em letter spacing, 20 px cut corners at the top-left and bottom-right, and a thin dark edge so they stand out from the tiles. The shape comes from a small cut-corner sprite generated once in the Editor, imported as a 9-sliced sprite (borders matching the cut), and tinted by `accentColor`, so it scales to any button size. Pressed and disabled states darken or desaturate the accent.

**Font:** menu text uses Jost SemiBold through the TextMeshPro font asset `Assets/Fonts/Jost/Jost-600-Semi SDF.asset` (dynamic atlas generated from `Jost-600-Semi.ttf`). Jost is licensed under the SIL Open Font License 1.1, so `OFL.txt` stays next to the font files and ships with the project. Gameplay text keeps its current font.

**Imports:** the title and overlay PNGs are imported as uncompressed or high-quality sprites without mipmaps, with a maximum size of at least 2048, so they stay sharp on tablets.

### 8. Exit

EXIT calls `Application.Quit()` on platforms that allow it, sets `EditorApplication.isPlaying = false` in the Editor, and is hidden under `UNITY_IOS`.

### 9. Unity content changes

The menu canvas hierarchy, background root, pause button, and the new `Board/Pause` action are created with Unity Editor APIs through the Unity MCP connection (or in the Editor by hand), not by hand-writing YAML. Where a serialized file must be touched directly, existing GUIDs and fileIDs are preserved and new objects get fresh IDs, as `AGENTS.md` allows. Serialized references are wired the same way and verified after a scene reload.

## Risks / Trade-offs

- **[Background cost on low-end phones]** → The self-sizing wall needs roughly 140–340 tiles, so wall tiles carry only four renderers each, no colliders, and no shadows, and the wall is fully disabled during play. If a target phone struggles, a flatter tilt reduces the count by about a quarter; validate on a physical device in a later change.
- **[Picker grid too small to drag accurately on phones]** → Size cells to the safe area with a minimum cell size, and track the selection from the press cell so small drifts do not change it.
- **[Pausing mid-animation leaves odd visual states]** → Freeze the animation clock rather than cancelling, so CONTINUE resumes exactly; MAIN MENU uses the existing cancellation path, which restores consistent tile state.
- **[Wide boards shrink tiles too much]** → The 4×4 square limit keeps aspect ratios moderate; framing already fits both axes and is validated at 4:3, 16:9, and wide ratios.
- **[Second press both starts a game and taps a tile]** → Arm board input only after all pointers are released.
- **[Title and overlay artwork are fixed at 16:9]** → Anchor the title to the top-left of the safe area with preserved aspect, and stretch the overlay; check 4:3 and wide ratios so the darkened area still sits behind the left column.
- **[Dynamic font atlas grows at runtime]** → Pre-populate the menu's characters in the Jost atlas; the dynamic mode only covers rare extra characters.
- **[GameManager API grows]** → Keep the menu-facing surface to start, pause, abandon, readiness, and completion; menu UI logic stays in `MenuController`.

## Migration Plan

1. Add `MenuFlow`, `GameSettings`, board-selection validation, and the width × height generation/framing changes with EditMode tests; remove the startup board call.
2. Add pause and abandon support to `GameManager`.
3. Add `MenuController`, `GridSizePicker`, and `TileWallBackground`.
4. Build the menu canvas, pause button, and background root in `MainScene`, add the `Board/Pause` action, and wire references.
5. Validate the full loop in Play Mode and at representative aspect ratios.

Rollback is reverting the source and scene/input changes together; the only persisted data is the settings keys, which are safe to leave behind.
