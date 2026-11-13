## Context

See `proposal.md` for motivation and `specs/game-menus/spec.md` for the behavior contract.

The menu flow already exists: `MenuController` owns the screens through the plain `MenuFlow` state model (Title, Settings, Picker, Playing, Paused), and its MAIN MENU path calls `GameManager.AbandonGame()` and `MenuFlow.Abandon()`, which discards the board without raising completion and shows the normal title. `GameSettings` stores vibration and volumes as primitive `PlayerPrefs` values behind an `ISettingsStorage` interface, with EditMode tests using an in-memory store. The settings screen is a uGUI panel on the Screen Space - Overlay menu canvas with a toggle and two sliders, all bound by `MenuController`.

## Goals / Non-Goals

**Goals:**

- Return to the title after the configured idle time during a game, reusing the existing abandon path.
- Detect "any input" robustly, including input the board ignores (locked states, blocked UI, paused).
- Add the timeout to the existing settings model and screen with minimal new UI.
- Keep the timer logic testable without Unity.

**Non-Goals:**

- Warnings, countdowns, or confirmation before returning.
- Returning from menu screens, or any attract mode.
- New input actions or changes to board input handling.
- Physical-device validation (its own later change).

## Decisions

### 1. Observe raw input events rather than gameplay actions

Inactivity is measured from the Input System's event stream: `InputSystem.onEvent` reports every state or delta event from every device. A press, touch, pen contact, key press, or mouse movement counts as activity; noise-only events (for example sensor or idle gamepad updates) are ignored by only counting events from `Pointer` (mouse, pen, touchscreen) and `Keyboard` devices whose controls actually changed in a meaningful way (`InputControlExtensions.EnumerateChangedControls` with a small noise threshold, or press/position changes for pointers).

This catches input the board deliberately ignores (taps during a mismatch, taps on UI, taps while paused), which a board-action subscription would miss.

**Alternatives considered:** Subscribing to `Board/PrimaryPress` misses keyboard, UI-only taps, and taps while locked. Polling `Touchscreen.current`/`Mouse.current` each frame needs per-device bookkeeping and can miss short taps between frames.

### 2. A plain `InactivityTimer` model owned by `MenuController`

A small class with `Reset()`, `Tick(float deltaSeconds, bool running)`, and `bool Expired` holds the elapsed idle time and the configured timeout. `MenuController` ticks it every frame with unscaled delta time and `running = MenuFlow.IsInGame`. When it expires, `MenuController` runs the same steps as MAIN MENU: unpause, `GameManager.AbandonGame()`, then move to the title. Any input event calls `Reset()`. Entering a game also resets it, so time spent in the menus never counts.

`MenuFlow` gains a `TimeOut()` transition from Playing or Paused to Title that clears the success title, so idle returns work from both in-game states. The existing `Abandon()` stays Paused-only for the MAIN MENU button.

Unscaled time keeps the timer running while paused, matching the spec that paused games also return.

**Alternatives considered:** Putting the timer in `GameManager` would mix menu navigation into match orchestration. A coroutine per game is harder to reset from input callbacks than a ticked model.

### 3. Timeout setting in `GameSettings`

`GameSettings` gains `InactivityTimeoutSeconds`, stored as an int under `settings.inactivityTimeout`, defaulting to 60 and clamped to 15–300 and to a multiple of 5. Changing it raises `Changed` like the other settings, and `MenuController` pushes the new value into the timer immediately. Existing keys and behaviour are unchanged, so players who already have settings saved simply get the default.

### 4. Settings screen row

The settings panel gets a fourth row under MUSIC: an "INACTIVITY" label, a slider configured as whole numbers (3–60, multiplied by 5 to give 15–300 seconds), and a value label showing for example "60 s". It reuses the existing slider styling (accent fill, Jost labels), and the panel and BACK button move down by one row height. `MenuController` binds the slider like the volume sliders and updates the value label on change.

### 5. Unity content changes

The new settings row and the panel resize are made through Unity Editor APIs over the Unity MCP connection (or by hand in the Editor), not by hand-editing the scene YAML. New serialized references on `MenuController` (slider and value label) are wired the same way and checked after a scene reload.

## Risks / Trade-offs

- **[Noisy devices keep the game alive]** → Count only pointer and keyboard devices, and require an actual control change, so connected sensors or controllers cannot block the reset.
- **[A player reading the description is reset]** → The timer runs during the match presentation too; 60 s is long enough to read, and the setting can be raised to 300 s.
- **[Mouse jitter in the Editor resets the timer]** → Treat small pointer movement below a threshold as noise; clicks and touches always count.
- **[Abandon during an animation]** → `AbandonGame()` already cancels running sequences and restores tiles; the same path is used by MAIN MENU and board regeneration.

## Migration Plan

1. Add `InactivityTimer` and the `GameSettings` timeout with EditMode tests.
2. Add `MenuFlow.TimeOut()` with tests, then the timer, input hook, and settings binding in `MenuController`; remove the GameManager TODO.
3. Add the settings row through Editor APIs and wire references.
4. Validate in Play Mode with a short timeout.

Rollback is reverting the source and the settings-row scene change; the extra preference key is harmless if left behind.
