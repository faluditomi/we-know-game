## Context

- `SoundEffects` (`Assets/Scripts/SoundEffects.cs`) is a single MonoBehaviour in `MainScene` with one serialized `Cue` (clip, volume, pitchVariation) per `SoundCue`, played through a small `AudioSource` pool. `GameManager` and `MenuController` find it and call `Play(SoundCue)`.
- All cue clips are currently empty. The Kenney Interface Sounds `.ogg` clips sit in `Assets/Sounds/sounds/Audio`, imported as Decompress On Load (so `AudioClip.GetData` works in the Editor).
- The project uses the new Input System only (`activeInputHandler: 1`). Edit-mode tests live in `Assets/Editor/Tests` (no asmdefs; runtime code is in Assembly-CSharp).
- `openspec/config.yaml` asks that `.unity`/`.prefab`/`.asset` files not be hand-edited. All scene and asset changes here are made by Editor menu commands written in C#, or by the user in the Editor.

See proposal.md for motivation and specs/sound-audition/spec.md for the required behaviour.

## Goals / Non-Goals

**Goals:**
- Zero behaviour change and zero extra content in release builds.
- Usable one-handed on a phone during a real round.
- Keep the core logic (choice lists, stepping, picks text) in plain C# so it can be covered by edit-mode tests.

**Non-Goals:**
- A polished or localized UI. The overlay is developer-facing, English-only, IMGUI.
- Persisting audition choices across device sessions.
- Changing how `GameManager`/`MenuController` trigger cues.

## Decisions

### 1. Candidates live in a separate dev-only ScriptableObject
`SoundAuditionCandidates : ScriptableObject` holds `List<CueCandidates { SoundCue cue; List<AudioClip> clips; }>`. It is created by an Editor menu (`Tools/Sound Audition/Create Candidates Asset`, via `AssetDatabase.CreateAsset`) at `Assets/Sounds/SoundAuditionCandidates.asset`.
- *Alternative*: candidate lists as fields on `SoundEffects.Cue`. Rejected because any clip referenced from the shipping component is always included in builds, and `#if`-guarded serialized fields cause serialization-layout mismatches in players.

### 2. Runtime hook in `SoundEffects` is code-only and conditionally compiled
Inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, `SoundEffects` gains:
- `event Action<SoundCue> Played`, raised in `Play()` after the throttle check, for any cue that passes it (including silent ones), so the overlay can follow the last played cue.
- `SetAuditionClip(SoundCue, AudioClip)`, `HasAuditionClip`/`ClearAuditionClips()`: a per-cue override slot (`bool[]` "overridden" plus `AudioClip[]`). When overridden, `Play` uses the override clip (null = silent) instead of `cue.clip`; volume, pitch variation, Sound volume, voices and throttling are unchanged.
- `ShippingClip(SoundCue)` read accessor.
Only methods and non-serialized fields are conditional, so the serialization layout is the same in every build. Release builds compile none of it.
- *Alternative*: the audition writes directly into the serialized `Cue.clip` at runtime. Rejected: on exit from play mode this happens to revert in the Editor, but it couples the tool to field internals and makes "restore shipping clip" fragile.

### 3. Plain C# model for choices and picks
`SoundAuditionModel` (runtime, no `MonoBehaviour`) builds each cue's choice list (shipping clip, then distinct candidates, then silence), tracks the active index, and steps with wrap-around. It starts on the shipping clip, or on the first candidate when there is none (all cues ship empty at first, and starting on silence made the un-stepped cues inaudible). On start the overlay pushes every non-shipping starting choice to `SoundEffects` as an audition override. `SoundAuditionPicks` formats and parses picks text, one line per cue: `CueName = clip_name` (`(none)` for silence). Parsing returns per-line results with errors for unknown cues or clips, using a name-to-clip lookup supplied by the caller. Both are edit-mode tested.

### 4. Overlay: `SoundAudition` MonoBehaviour drawing with IMGUI
A `SoundAudition` component on its own `MainScene` GameObject references `SoundEffects` and the candidates asset. It draws with `OnGUI`, scaled by `Screen.dpi` so buttons are finger-sized.
- Closed: a small semi-transparent corner button (top-left, inside the safe area).
- Open: a "last played" strip at the top (cue name, active clip, `◀`, `▶`, replay, list toggle), an optional scrollable full cue list with the same controls per row, and `Copy picks` / `Apply picks` (Editor only) / `Reset all` buttons.
- Editor keys (new Input System `Keyboard.current`): `F8` toggles the overlay; `[` / `]` step the last played cue; `\` replays it.
- Previewing calls `SoundEffects.Play(cue)` so it uses the same path and volume as the game (the throttle is irrelevant at tap speed).
- *Alternative*: a uGUI/UI Toolkit panel. Rejected: it would require prefab/scene UI authoring and localization wiring for a throwaway dev tool. IMGUI is self-contained and works in development players.

**Blocking input under the overlay:** IMGUI does not stop uGUI or the board from receiving the same tap. `SoundAudition` therefore builds, at runtime in code, a screen-space overlay `Canvas` (high sorting order, with a `GraphicRaycaster`) holding transparent raycast-target `Image` blockers that it keeps sized to the rects it draws (corner button, strip, list). Menus then don't receive those taps, and `GameManager` already ignores board taps over uGUI graphics hit by a `GraphicRaycaster`. No changes to `GameManager` or `MenuController` are needed, and nothing is authored in the scene.
- *Alternative*: a `CapturesPointer` check added to `GameManager` and `MenuController` input paths. Rejected: it touches gameplay code and would not stop uGUI buttons, which take their input from the `EventSystem`.

### 5. Carrying picks back
- **Editor, during play mode**: `Apply picks` stores the picks text in `SessionState`. An Editor script listening to `EditorApplication.playModeStateChanged` (`EnteredEditMode`) finds the scene's `SoundEffects`, and writes each picked clip into `Cue.clip` through `SerializedObject` with `Undo.RecordObject`, then marks the scene dirty. Writing after leaving play mode is necessary because play-mode changes are discarded.
- **Device**: `Copy picks` sets `GUIUtility.systemCopyBuffer`. The user moves the text to the PC (e.g. message to themselves) and pastes it into `Tools/Sound Audition/Import Picks…`, an Editor window with a text area that shows parse results and an Apply button (same Undo-backed write).
- Clip names are matched against `AudioClip` assets under `Assets/Sounds` by asset name. Some packs ship each sound several times under one name (mono/stereo × ogg/wav/mp3). Same-named clips are reduced to one preferred variant: mono before stereo, then .ogg, .wav (SD before HD), .mp3. Only if two clips tie on that ranking does the import report the name as ambiguous and skip it. The analyzer and seeding use the same reduction. Family names take the last " - " segment, so "Pack - Popup Open - 1" belongs to the "popup open" family. The overlay shows only the last two " - " segments of long names; copied picks keep the full name.

### 6. Clip analyzer window
`Tools/Sound Audition/Clip Analyzer` (Editor window) loads every `AudioClip` under `Assets/Sounds`, and computes from `GetData` (mixed to mono):
- duration (`clip.length`) and audible length (until the signal stays 40 dB under its peak; some packs pad every file to the same length), peak (dBFS), RMS over the audible part (dBFS),
- brightness as the spectral centroid (Hz) of an averaged FFT over the clip (a small radix-2 FFT in the Editor script; no dependency).
A clip whose data cannot be read is listed with "n/a" and skipped by seeding. Rows are sortable, have a play button (via the Editor's internal `AudioUtil.PlayPreviewClip` by reflection, falling back to a temporary hidden `AudioSource` if it is unavailable), and can be exported to CSV at a location the user picks with `EditorUtility.SaveFilePanel`.

**Seeding** applies a static rule table in the Editor script, per cue: allowed name prefixes (e.g. TileFlip → `tick`, `click`, `select`, `pluck`; Mismatch → `drop`, `error`, `pluck`; Victory → `confirmation`, `maximize`), a maximum duration, and a sort key (shortest/quietest first for frequent cues, longest/brightest for rare ones). It appends up to 6 matching clips not already present, via `SerializedObject` with Undo. Each rule also names one proposed clip, chosen from clip names and the Ogg durations (the clips could not be listened to). Seeding moves it to the front of the cue's list, or inserts it there, so it is the clip the audition starts on. The table is a starting point and is meant to be edited in code.

### 7. Release stripping
An Editor `IProcessSceneWithReport` callback destroys every `SoundAudition` GameObject when building a player with `EditorUserBuildSettings.development == false`. Scene processing happens before the build collects dependencies, so the candidates asset and its clips are not included. The `SoundAudition` class itself also compiles to an empty, inert component outside `UNITY_EDITOR || DEVELOPMENT_BUILD`, as a second safety net.
- *Alternative*: the `EditorOnly` tag. Rejected because it also strips development builds, which are needed for on-device auditioning.

## Risks / Trade-offs

- [Scene-processing strip doesn't remove dependencies as expected] → A validation task checks a release build's Editor.log build report for audition candidate clips and the candidates asset.
- [IMGUI taps leak through to the board or menus] → `CapturesPointer` checks in input paths, plus a manual on-device check in tasks.
- [Internal `AudioUtil` preview API changes between Unity versions] → Reflection with a hidden-`AudioSource` fallback.
- [Development builds get bigger because of all the candidate clips] → Acceptable for dev builds; seeding caps candidates per cue.
- [Clip names collide across packs] → Import reports ambiguous names and skips them.
- [Moving picks text from phone to PC is manual] → Acceptable for an occasional dev task; the Editor apply path covers most iteration.

## Migration Plan

Additive tooling. Setup in the Editor: create the candidates asset, add the `SoundAudition` GameObject to `MainScene` (an Editor menu command does both and wires references), seed shortlists, then play. Rollback: delete the GameObject, asset and scripts. Shipping cue data is only changed when picks are explicitly applied, and those writes are undoable.
