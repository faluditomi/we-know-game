## Why

The game is meant to run unattended, for example on a stand where people walk up and play. If a player leaves mid-game, the next visitor finds a half-played board instead of the title screen. The game should return to the main menu on its own after a period without input.

## What Changes

- While a game is ongoing (playing or paused), the game tracks the time since the last player input. When it reaches the configured timeout, the board is abandoned and the title screen is shown immediately, with no warning, countdown, or confirmation. The normal title shows, not the success title.
- Any touch, pen, mouse, or keyboard input resets the timer, including taps on in-game menu UI and taps that the board ignores during animations.
- Add an **inactivity timeout** control to the existing SETTINGS screen, saved with the other settings. Default: 60 seconds; range 15–300 seconds in 5-second steps.
- The timer does not run on the title, picker, or settings screens.
- Remove the corresponding TODO from `GameManager`.
- Explicit non-goals: warnings or countdowns before the reset, returning to the title from menu screens, a separate attract mode, and physical-device testing (tracked as its own later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `game-menus`: adds the inactivity return to the title during a game, and the inactivity timeout control on the settings screen.

## Impact

- Runtime source: `MenuController` (inactivity timer, reset trigger, settings control binding), `GameSettings` (new persisted timeout value), and `GameManager` (TODO removal only; the existing `AbandonGame` path is reused).
- Unity content: `MainScene` gains one slider row with a value label on the settings screen; the settings panel grows to fit. Made through Unity Editor APIs.
- Data: one new primitive preference key for the timeout; existing settings keys are unchanged.
- Asset loading and the input action asset are unchanged; input activity is observed through the Input System's event stream rather than new actions.
