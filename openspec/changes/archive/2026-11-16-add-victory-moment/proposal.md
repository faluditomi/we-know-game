## Why

Finishing a board is the high point of a round, but right now the finished board just stays on screen for one second and then the title returns. A short, clearly celebratory moment makes the win feel earned. Showing how the round went gives it some meaning, and none of this should cost noticeable performance on mid-range phones.

## What Changes

- **Victory moment on the finished board.** It plays when the last pair is back on the board and replaces the current one-second hold. It lasts about 1.5–2 seconds, then the game goes to the title as before. It combines:
  - **Board wave:** every matched tile does a quick hop and spin, rippling outward from the last pair found.
  - **Screen surge:** all tile screens flash brighter and glitch in sync, then settle.
  - **Confetti burst:** one short burst of We-Know teal and white particles.
  - **Camera punch:** a small zoom in and settle as the moment starts.
  - **Win vibration:** one short vibration on phones and tablets, only when the Vibration setting is on.
- **Round stats on the success title.** While "We Know... And Now You Do Too!" shows, a line below it shows the number of pairs, the time taken and the number of misses (for example "8 pairs · 1:32 · 3 misses"), in the chosen language.
- **Success title entrance.** The success title pops in with a short glow pulse instead of simply appearing.

Non-goals:
- No sounds or music, and no pair-streak celebrations during play.
- No bloom, full-screen post-processing or screen-space distortion.
- No saved records or best times. The stats describe only the round just finished.
- The normal title ("We Know... Do You?") and abandoned or timed-out games are unchanged: there's no celebration and no stats.

## Capabilities

### New Capabilities
- `victory-celebration`: the victory moment on a completed board, covering the board wave, screen surge, confetti, camera punch, win vibration, timing, interaction with pause and inactivity, and the performance limits.

### Modified Capabilities
- `tile-match-gameplay`: Board completion now plays the victory moment before leaving the board.
- `game-menus`: the success title gains the round stats line and the entrance animation.

## Impact

- **Scripts:**
  - `GameManager`: round stats (time without pauses, misses) and the victory sequence on the board.
  - `TileController`: a hop pose for the wave.
  - `MatchFlow`: a miss count.
  - `MenuController`: starts the victory moment, the stats line and the title entrance.
  - `LocalizedText`: the stats wording in English and Hungarian.
  - New: a victory-effects component for the confetti, camera punch and screen surge.
- **Shaders:** `HologramScreen` reads one global surge value. There's no new pass and no per-material change.
- **Scene / prefabs:**
  - `MainScene`: a confetti particle system and a stats label under the title.
  - The tile prefab is unchanged.
- **Tests:** EditMode tests for the stats, the miss count, the stats formatting, and the wave delay ordering.
- **Performance:** no new render passes or post-processing. The cost is one particle burst of a few hundred particles, unlit and with no collisions, plus per-frame transform updates on the existing tiles.
