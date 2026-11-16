## 1. Round statistics

- [x] 1.1 `MatchFlow`: add `Misses` (counted on `Mismatch`, reset in `StartBoard`); extend `MatchFlowTests`
- [x] 1.2 `GameManager`: accumulate unpaused play time for an active, uncompleted board; store a `RoundStats { Pairs, Seconds, Misses }` snapshot at completion and expose `LastRoundStats`; reset on a new board
- [x] 1.3 Add `RoundStatsText.Format(GameLanguage, RoundStats)` and the stats keys in `LocalizedText` (English singular/plural, Hungarian), time as `m:ss`
- [x] 1.4 EditMode tests: formatting in both languages, singular wording, `m:ss` rounding, every new key translated

## 2. Victory timeline

- [x] 2.1 Add `VictorySettings` to `TileAnimationSettings` with the defaults from design.md
- [x] 2.2 Add pure `VictoryWave.Delays(positions, origin, spread)` with tests for ordering, range and a single-tile board
- [x] 2.3 `TileController.SetVictoryHop(t)`: hop towards the camera, scale bump, one full flip, ending exactly in the matched pose
- [x] 2.4 `GameManager.PlayVictory`: run the wave, surge, camera punch, confetti and vibration on one pause-aware timeline after the final `MarkMatched`; raise `VictoryFinished`
- [x] 2.5 `GameManager.AbandonGame` and teardown: stop the victory and reset the surge global, camera base FOV/size and particles
- [x] 2.6 `MenuController.ReturnAfterCompletion`: wait for `VictoryFinished` (plus `completionReturnDelay`, default 0) instead of the fixed hold

## 3. Effects

- [x] 3.1 `HologramScreen.shader`: read global `_VictorySurge` (outside Properties/CBUFFER) for extra brightness and glitch
- [x] 3.2 Add `VictoryEffects` component: `Burst(position)`, `SetPaused(bool)`, `Clear()`, emitter width sized to the visible width at its depth
- [x] 3.3 Win vibration at the start of the moment, gated by `GameSettings.Vibration` and the mobile platform guard

## 4. Success title

- [x] 4.1 `MenuController`: stats label shown only with the success title, refreshed on language change, hidden on revert and for abandoned games
- [x] 4.2 `MenuController`: success-title pop-in (scale 0.82 → 1.06 → 1.0) and glow pulse driven from `Update`
- [x] 4.3 Allow-list the stats label in `SceneLocalizationTests`

## 5. Scene and assets (via Unity MCP / Editor)

- [x] 5.1 Create the confetti particle material (URP Particles/Unlit) and a `VictoryEffects` object in `MainScene` with its `ParticleSystem` configured per design.md (220-particle burst, max 400, world space, no collision/lights/shadows)
- [x] 5.2 Add the "Round Stats" TMP label and "Title Glow" image to the title screen, and wire them, plus `VictoryEffects`, to `MenuController`/`GameManager`

## 6. Validation

- [x] 6.1 Run all EditMode tests
- [x] 6.2 Play Mode: win a board and confirm the order (wave from the last pair, surge, confetti, camera punch, settle) and that the title follows within 2.5 s
- [x] 6.3 Play Mode: pause mid-moment, confirm everything holds, CONTINUE resumes; MAIN MENU from that pause shows the normal title with no stats and leaves no surge/confetti/camera offset behind
- [x] 6.4 Check the stats line in English and Hungarian, singular wording, and that it disappears when the title reverts
- [x] 6.5 On an Android device: vibration on/off, and frame rate during the moment shows no visible drop compared with normal play

## 7. Polish after review

- [x] 7.1 Confetti: fire from both lower sides of the screen, up and inward, from the one particle system
- [x] 7.2 Title glow: larger (scale 1.2) and stronger (alpha 0.95, 0.9 s pulse)
- [x] 7.3 Success title on the main menu lasts longer (success-title duration 4 s → 6 s)
- [x] 7.4 Stats line: larger white text on a dark backing panel that sizes to the text
