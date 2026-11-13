## Why

The game currently starts straight into a random 2×2 or 4×4 board (a temporary `TODO: REMOVE THIS` call) and has no way to choose a board, pause, adjust settings, or come back after finishing. It needs a small, touch-first menu flow so it can run as a standalone experience with a proper start, end, and a recognisable We Know look.

## What Changes

- Add a **title screen** with the game title ("We Know... Do You?") and PLAY, SETTINGS, and EXIT actions, laid out as in the supplied mockup: title lockup top-left, buttons stacked beneath it, the tile wall visible mainly centre-right. The title uses the supplied lockup artwork, with styled text as a fallback when no artwork is assigned.
- Add a **success title**: after a completed game the player returns to the title screen, which briefly reads "We Know... And Now You Do Too!" before reverting. How long it shows is configurable.
- Add a **grid size picker** after PLAY: the player presses and drags across a grid of the largest supported size to draw a rectangle of cells. Releasing starts a game of that size, unless the selection is too small or has an odd tile count. Rectangles of any shape are allowed, up to the size the available tile designs can fill (8 active designs today, so up to 4×4).
- **Board generation and framing support width × height boards** instead of square-only boards. **BREAKING** for any caller of the square-only generation entry point; the temporary random startup board is removed.
- Add a **pause menu** during a game with CONTINUE and MAIN MENU (MAIN MENU abandons the current board).
- Add a **settings screen** with vibration on/off (driving the existing blocked-input haptics) and sound and music volume sliders. No audio exists yet; the values are stored for later use. Settings persist between sessions.
- Add a **live menu background**: a slowly drifting, tilted wall of real tiles, mostly face down, with random tiles flipping to show their logos, under the supplied readability overlay. It pauses during gameplay and plays a celebratory flip wave while the success title shows.
- Use the We Know logo teal (**#128580**) as the accent colour across all menu screens, and Jost SemiBold (SIL Open Font License 1.1, added with its license) for menu text, with the button style from the supplied mockup.
- EXIT quits where the platform supports quitting and is hidden where it does not (iOS); in the Editor it stops Play Mode.
- Explicit non-goals: inactivity return to the main menu (separate change), leaderboard, audio content and playback, localization, portrait layouts, and the jagged-edge effect.

## Capabilities

### New Capabilities

- `game-menus`: Title screen and success title, grid size picker, pause menu, settings with persistence, live tile-wall background, exit behaviour, and the menu visual identity.

### Modified Capabilities

- `tile-match-gameplay`: Boards become width × height rectangles chosen by the player instead of square boards generated at startup, and completing a board returns the player to the title screen.

## Impact

- Runtime source: `GameManager` (board generation for width × height, framing, startup flow, pause hook, completion hand-off), `BoardFraming`, and new menu components (a menu flow controller, screen views, the grid picker, the tile-wall background, and a settings store).
- Unity content: `MainScene` gains a menu canvas hierarchy (title, picker, pause, settings) and a background tile-wall root; the Input Actions asset may gain a pause/back action. Changes are made through Unity Editor workflows or careful serialized edits per `AGENTS.md`.
- Data: settings persist as primitive key/value preferences; no other save data is introduced.
- Asset loading: the existing Addressables tile prefab and tile data are reused for both the background and the picker's maximum size; no `Resources.Load` path or global singleton is introduced.
- Art and fonts: uses the supplied PNGs in `Assets/Sprites/MainMenuBackground/` (normal and success title lockups, readability overlay; the mockups and SVG exports are reference only) and `Assets/Fonts/Jost/` (Jost SemiBold, its TextMeshPro font asset, and `OFL.txt`, which must stay with the font). A cut-corner button sprite is generated for the button style.
