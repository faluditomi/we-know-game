## Why

All 17 `SoundCue` slots in `SoundEffects` are still empty, and the Kenney Interface Sounds pack (~100 clips) is now in `Assets/Sounds`. Picking clips one at a time in the Inspector is slow and misleading: a cue only sounds right or wrong in context (in sequence with neighbouring cues, at game speed, through a phone speaker, after many repetitions). A dev-only audition mode lets the cues be chosen while actually playing, on the target device.

## What Changes

- Add a **candidate list per cue**, stored in a dev-only candidates asset kept separate from the shipping `SoundEffects` setup.
- Add a **dev audition overlay** (Editor play mode and development builds, including on-device) that:
  - shows each cue's currently active clip and its position in the candidate list,
  - cycles to the next/previous candidate for a cue and immediately previews it,
  - follows the most recently played cue so "the sound I just heard" can be swapped in one tap,
  - offers keyboard shortcuts in the Editor and touch controls on device.
- Add a minimal dev-only override hook to `SoundEffects` so the audition can substitute a clip for a cue at runtime, plus a notification when a cue plays. Normal playback (volume, pitch variation, voices, repeat throttling) is unchanged.
- Add a way to **carry picks back** into the serialized `SoundEffects` cue clips: apply directly from an Editor session, or copy the picks as text on device and import them in the Editor.
- Add an **Editor clip analyzer** that measures each clip under `Assets/Sounds` (duration, peak and RMS loudness, brightness) and can seed per-cue shortlists into the candidates asset using simple per-cue rules.
- Strip the audition object, overlay, and candidate clip references from non-development (release) builds.

## Capabilities

### New Capabilities
- `sound-audition`: Developer tooling for choosing sound-effect clips in context: per-cue candidate lists, an in-game dev overlay to cycle and preview candidates, carrying picks back into the shipping cue setup, an Editor clip analyzer that seeds shortlists, and exclusion from release builds.

### Modified Capabilities
None. Player-facing sound behaviour does not change; the override hook only exists in Editor and development builds.

## Impact

- **Code**: `Assets/Scripts/SoundEffects.cs` (dev-only override hook and play notification, compiled only under `UNITY_EDITOR || DEVELOPMENT_BUILD`); new runtime script(s) for the audition overlay and candidates asset type; new Editor scripts under `Assets/Editor` (clip analyzer window, shortlist seeding, apply/import picks, release-build stripping).
- **Scenes/assets**: `MainScene` gains an audition GameObject, and a new candidates asset is created. Both are created by Editor menu commands or in the Editor by the user, not by hand-editing YAML.
- **Builds**: release builds must not contain the overlay or the candidate clips. Development builds will be larger by the candidate clips.
- **Input**: uses the new Input System (`Keyboard.current`, touch) already used by the project; no new Input Actions asset entries.
- **Dependencies**: none new.

## Non-goals

- Choosing the final sounds. This change only provides the tooling; the picks are made by the user.
- Any player-facing sound settings or UI changes.
- Music, mixing groups, or `AudioMixer` setup.
- Automatic, analysis-only selection of final clips. The analyzer only seeds shortlists.
- Editing or adding clips to the sound pack.
