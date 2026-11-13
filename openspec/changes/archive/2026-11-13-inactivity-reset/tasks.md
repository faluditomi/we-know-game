## 1. Model and Settings

- [x] 1.1 Add a plain `InactivityTimer` class (reset, tick with running flag, configurable timeout, expired state) with EditMode tests for expiry, reset, and not counting while stopped.
- [x] 1.2 Add `InactivityTimeoutSeconds` to `GameSettings` (int key `settings.inactivityTimeout`, default 60, clamped to 15–300 and snapped to 5-second steps, raises `Changed`) with EditMode tests for the default, clamping, snapping, and round-tripping.
- [x] 1.3 Add a `MenuFlow.TimeOut()` transition from Playing or Paused to Title that clears the success title, with EditMode tests.

## 2. Runtime Behaviour

- [x] 2.1 In `MenuController`, tick the timer on unscaled time only while in a game, reset it when a game starts and on every meaningful pointer or keyboard input event from the Input System event stream (ignoring other devices and sub-threshold pointer movement).
- [x] 2.2 On expiry, return to the title through the abandon path (unpause, `GameManager.AbandonGame()`, `MenuFlow.TimeOut()`), with no warning or confirmation and the normal title.
- [x] 2.3 Bind a settings slider and value label for the timeout (15–300 s in 5 s steps, label like "60 s") and push changes into the timer immediately; remove the inactivity TODO from `GameManager`.

## 3. Scene Setup (Unity Editor workflow)

- [x] 3.1 Through Unity Editor APIs, add the INACTIVITY row (label, slider, value label) to the settings panel in the existing style, resize the panel and move BACK down, and wire the new `MenuController` references.
- [x] 3.2 Reopen the scene and confirm the new references and existing settings bindings survive serialization.

## 4. Verification

- [x] 4.1 Run Unity compilation and all EditMode tests; resolve any new errors or warnings.
- [x] 4.2 In Play Mode with a short timeout: idle while playing returns to the title with the normal title; idle while paused returns too; input (taps on tiles, taps during an animation, keyboard) restarts the timer; no reset on title, picker, or settings screens; the setting persists and the default is 60 s on a fresh install.
