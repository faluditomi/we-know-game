## Context

See proposal.md for the motivation. How a win works today:

- `GameManager.ReturnMatch` marks the last pair matched and raises `BoardCompleted`. `MatchFlow` is then completed, so the board takes no more selections.
- `MenuController.ReturnAfterCompletion` waits `completionReturnDelay` (1 s, counted only while not paused), calls `AbandonGame()` and `flow.Complete()`, shows the success title and starts the background tile waves (`TileWallBackground.Celebrate`).
- `GameManager.Animate` already holds while the game is paused. Tiles are animated frame by frame through `TileController` pose methods.
- Tile screens use `WeKnow/Hologram Screen`, which is self-lit. The matched-tile pose is logo side up, flip angle 180.
- The title is a sprite (`titleImage`), with a TMP fallback (`titleText`).
- Vibration exists for blocked input (`Handheld.Vibrate`, gated by `GameSettings.Vibration`).
- The project runs on URP's mobile renderer with no post-processing in use, and that must stay so.

## Goals / Non-Goals

**Goals:**
- One timeline drives the board wave, screen surge, confetti and camera punch, so pausing holds all of them together.
- No allocations or instantiation during the moment: the confetti system and its material exist in the scene from the start.
- The pure parts can be tested in EditMode: the stats, their formatting, and the wave delay order.

**Non-Goals:**
- Changing the tile-wall background celebration on the title. It stays as it is.
- Any audio.

## Decisions

### 1. GameManager owns the victory timeline
After `MarkMatched` on the final pair, `GameManager` starts a `PlayVictory(origin)` coroutine and raises `VictoryFinished` when it ends. `origin` is the centre of the last pair. The coroutine starts just before `BoardCompleted` fires (still only once), so listeners already see `IsVictoryPlaying`. `MenuController.ReturnAfterCompletion` now waits for `VictoryFinished` instead of a fixed delay. `completionReturnDelay` becomes a short optional hold after the moment, defaulting to 0.
*Why:* `GameManager` already owns the tiles, the camera and pause-aware `Animate`. *Alternative:* a separate director object. Rejected because it would need the tile list, camera and pause state that `GameManager` keeps private.

`AbandonGame()` (MAIN MENU, timeout) stops the coroutine and calls `ResetVictoryEffects()`. That clears the surge global, restores the camera's field of view or orthographic size, and clears the particles.

### 2. Board wave through a new tile pose
`TileController.SetVictoryHop(float t)`: for `t` in 0..1, the tile rises towards the camera by `hopHeight · sin(πt)` along the board normal, scales by `1 + hopScale · sin(πt)`, and turns one full flip from 180° to 540°. At `t = 1` it is exactly the matched board pose (`MarkMatched` restores it as well). Each tile's start delay is `waveSpread · distance(tile, origin) / maxDistance`, computed by a pure `VictoryWave.Delays(positions, origin, spread)` that is unit-tested for order and range. The defaults are a hop of 0.55 s and a spread of 0.7 s, so the wave takes about 1.25 s.

### 3. Screen surge as one shader global
`HologramScreen` reads a global `_VictorySurge` (0..1) that is not in its Properties or CBUFFER, so the SRP Batcher stays intact. It brightens by `1 + 0.6·surge`, adds `0.35·surge` to the glitch strength, and doubles the glitch rate while surging. `GameManager` sets it with `Shader.SetGlobalFloat`: an ease-out ramp up in 0.15 s, a hold, then an ease back to 0 by the end of the moment. The cost is one float per frame, with no material changes.
*Alternative:* a per-renderer `MaterialPropertyBlock`. Rejected because it means writing to every tile every frame.

### 4. Confetti: one pre-built particle system
A `VictoryEffects` component on a scene object holds one `ParticleSystem` and its material (`URP/Particles/Unlit`, a small white quad texture or the default particle). Settings:
- one burst of 220 particles, max particles 400
- lifetime 1.1–1.5 s, gravity 1.2, random start rotation and spin
- start colour random between We-Know teal `#128580`, light teal and white
- simulation in world space, no collision, no lights, no shadows

`VictoryEffects.Burst(camera, depth)` fires two "cannons" from the one system, using `ParticleSystem.Emit(EmitParams, count)`: half the particles start at the lower-left edge of the view and half at the lower-right edge, at about 70% of the board distance, with velocities aimed up and inward (randomised angle and speed) so the two streams arc over the board and fall. Using one system with per-burst emit parameters keeps the "one particle system" budget. `SetPaused(bool)` calls `Pause()`/`Play()`, and `Clear()` stops and clears it. `MenuController` already forwards pause to `GameManager.SetPaused`, so `GameManager` forwards it on to the effects. The longest particle lifetime is shorter than the moment, so the confetti is gone before the title shows.

### 5. Camera punch through the field of view
The punch scales the gameplay camera's field of view (or orthographic size) by `1 − punch · curve(t)`, with punch 0.05 over 0.45 s: ease in, overshoot slightly, settle at exactly the stored base value. The base is captured when the moment starts and written back at the end and on reset. The camera position doesn't move, so board framing and safe-area insets are untouched.

### 6. Win vibration
At the start of the moment: `if(settings.Vibration) Handheld.Vibrate()`, inside the same `UNITY_ANDROID || UNITY_IOS` guard as the blocked-input vibration. It is not rate-limited by the blocked-input cooldown.

### 7. Round statistics
- `MatchFlow` gains `Misses`. It counts up when `TrySelect` returns `Mismatch` and resets in `StartBoard`.
- `GameManager` adds `Time.unscaledDeltaTime` to a play-time counter while a board is active, not paused, and not yet completed.
- At completion it stores a `RoundStats { Pairs, Seconds, Misses }` snapshot, exposed as `LastRoundStats`.
- A pure `RoundStatsText.Format(GameLanguage, RoundStats)` builds the line:
  - It uses new `LocalizedText` keys: `StatsPairsOne`, `StatsPairsMany`, `StatsMissesOne`, `StatsMissesMany` and `StatsSeparator`.
  - English: "1 pair", "{0} pairs", "1 miss", "{0} misses".
  - Hungarian: "{0} pár" and "{0} tévesztés" for every count.
  - Time is shown as `m:ss`, rounded down to whole seconds.
- These are covered by EditMode tests.

### 8. Stats line and title entrance
- **Stats label:** a TMP label, "Round Stats", sits under the title on the title screen. It is driven from code and allow-listed in `SceneLocalizationTests`. `MenuController.RefreshTitle` shows it only while the success title shows, refills it when the language changes, and hides it on revert or when no stats exist (an abandoned game).
- **Pop-in:** the visible title (image or text) scales 0.82 → 1.06 → 1.0 over 0.45 s, unscaled time.
- **Glow:** a "Title Glow" image behind the title uses the same sprite, tinted with the accent colour, at scale 1.2 so it reaches well past the opaque artwork. Its alpha pulses 0 → 0.95 → 0 over 0.9 s. That makes a cheap halo without an extra shader or post-processing.
- **Stats readability:** the stats label sits on its own dark, cut-corner backing panel that sizes itself to the text, in white at 44 pt. `MenuController` shows and hides the panel together with the label.
- **Shared animation:** both run from `MenuController.Update`, the same way the success-title countdown does.

### 9. Tunables
A `VictorySettings` section in `TileAnimationSettings` holds the hop duration, height and scale, the wave spread, the surge strength, the punch amount and duration, and the confetti depth fraction, with the defaults above. The menu-side values (pop-in and glow timings) are fields on `MenuController`.

### Serialization edits
- **Script changes:** no YAML involved.
- **Scene (via the Unity MCP server, saved through the Editor API):**
  - the `VictoryEffects` object with its `ParticleSystem`, configured in code once and saved
  - the "Round Stats" label and the "Title Glow" image on the title screen
  - field wiring
- **New assets:** a confetti particle material.
- **Untouched:** the tile prefab, and `*.csproj`/`*.sln`.

## Risks / Trade-offs

- **[The hop overlaps neighbouring tiles at large board sizes]** → The hop moves towards the camera, so tiles pass in front of their neighbours rather than through them. The scale bump stays at 0.12 or less.
- **[A FOV punch changes perspective, and the presentation code reads the FOV]** → The punch only runs after the board is completed, when no presentation can start, and the base value is always restored on finish or abandon.
- **[The surge global leaks into the menu background tiles]** → That is intended: it ends before the title shows. `ResetVictoryEffects` sets it to 0 on every exit path.
- **[Confetti is too sparse or too dense across aspect ratios]** → The emitter shape is sized from the visible width at its depth when it bursts.
- **[Win vibration on tablets without a vibration motor]** → `Handheld.Vibrate` does nothing there, which is acceptable.

## Migration Plan

No data changes. The stats are not saved. Rolling back restores the fixed one-second hold.
