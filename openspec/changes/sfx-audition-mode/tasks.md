## 1. Core model (plain C#)

- [x] 1.1 Add `SoundAuditionModel` (runtime script): builds each cue's choice list (shipping clip, distinct candidates in order, then silence), tracks the active index, and steps next/previous with wrap-around
- [x] 1.2 Add `SoundAuditionPicks`: format picks as `CueName = clip_name` lines (`(none)` for silence) and parse them into per-line results, reporting unknown cues, unknown clips and ambiguous clip names
- [x] 1.3 Add edit-mode tests in `Assets/Editor/Tests/SoundAuditionTests.cs` covering choice ordering, de-duplication, the silence-only cue, wrap-around, and picks round-trip and parse errors

## 2. SoundEffects hook

- [x] 2.1 In `SoundEffects.cs`, inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, add the `Played` event, `SetAuditionClip` / `ClearAuditionClips` override slots and `ShippingClip(SoundCue)`, with no new serialized fields
- [x] 2.2 Make `Play` use the override clip (null = silent) when a cue is overridden, keeping volume, pitch variation, Sound volume, voices and repeat throttling unchanged, and raise `Played` after the throttle check
- [x] 2.3 Add an edit-mode test showing that with no overrides `Play` picks the shipping clip, and with an override it picks the override clip

## 3. Candidates asset and overlay

- [x] 3.1 Add `SoundAuditionCandidates` ScriptableObject (per-cue ordered clip lists)
- [x] 3.2 Add `SoundAudition` MonoBehaviour: references `SoundEffects` and the candidates asset, builds the model on start, applies the active choices as audition overrides, and follows the last played cue via `Played`. The class body compiles only under `UNITY_EDITOR || DEVELOPMENT_BUILD`
- [x] 3.3 Draw the IMGUI overlay, scaled by `Screen.dpi`: the closed corner button inside the safe area; the open view with the last-played strip (◀ ▶ replay, list toggle), the scrollable full cue list with per-row controls and `n / m` positions, and the `Copy picks`, `Apply picks` (Editor only) and `Reset all` buttons
- [x] 3.4 Build the runtime blocker canvas with transparent raycast-target images kept sized to the drawn overlay rects, so taps on the overlay reach neither menus nor the board
- [x] 3.5 Add the Editor keyboard shortcuts with the new Input System: `F8` toggles, `[` / `]` step and `\` replays the last played cue
- [x] 3.6 `Copy picks` writes the picks text to `GUIUtility.systemCopyBuffer`. `Apply picks` stores the picks in `SessionState` for the Editor to apply after leaving play mode

## 4. Editor tooling

- [x] 4.1 Add `Tools/Sound Audition/Set Up In Scene` menu: creates `Assets/Sounds/SoundAuditionCandidates.asset` if missing (`AssetDatabase.CreateAsset`), adds a `SoundAudition` GameObject to the open scene and wires its references through `SerializedObject` with Undo
- [x] 4.2 Add the play-mode-exit handler that applies pending picks from `SessionState` to the scene's `SoundEffects` cue clips through `SerializedObject` with Undo, and marks the scene dirty
- [x] 4.3 Add the `Import Picks…` Editor window: a paste text area, per-line parse results, and Apply using the same Undo-backed write. Clip names are resolved against `AudioClip` assets under `Assets/Sounds`
- [x] 4.4 Add the `Clip Analyzer` Editor window: lists clips under `Assets/Sounds` with duration, peak dBFS, RMS dBFS and spectral-centroid brightness (marking unreadable clips n/a), sortable columns, a per-row preview button, and CSV export
- [x] 4.5 Add shortlist seeding to the analyzer from a per-cue rule table (name prefixes, max duration, sort key). It appends up to 6 new candidates per cue with Undo, and never removes candidates or touches shipping clips
- [x] 4.6 Add the `IProcessSceneWithReport` callback that destroys `SoundAudition` GameObjects in non-development player builds

## 5. Audible defaults and proposed clips

- [x] 5.1 Start cues without a shipping clip on their first candidate, and apply the starting choices as audition overrides when the overlay starts
- [x] 5.2 Give every seeding rule a proposed clip and make seeding put it first in the cue's candidates (inserting or moving it, never removing)
- [x] 5.3 Update and extend edit-mode tests: first-candidate start, moving a candidate to the front, and every proposed clip exists under `Assets/Sounds`
- [x] 5.4 Reduce same-named clips to one preferred variant (mono, then ogg/wav/mp3) in the analyzer, seeding and picks import
- [x] 5.5 Measure audible length and RMS over the audible part; seeding limits and ordering use audible length; show it in the analyzer and CSV
- [x] 5.6 Read "Pack - Sound - N" names as the "sound" family, add the JDSherbert pack families to the seeding rules, and shorten long names in the overlay
- [x] 5.7 Add edit-mode tests for variant preference, audible length and the new family names

## 6. Validation (Unity Editor / device)

- [ ] 6.1 Run the edit-mode tests in the Test Runner and confirm they pass
- [ ] 6.2 In the Editor, run `Set Up In Scene` on `MainScene`, seed shortlists in the Clip Analyzer, and save the scene
- [ ] 6.3 In play mode: open the overlay with `F8`, step and replay cues, check that the game uses the active choice and that taps on the overlay don't flip tiles or press menu buttons
- [ ] 6.4 Leave play mode without applying and confirm the shipping clips are unchanged; then apply picks, leave play mode, confirm they were written, and confirm Undo reverts them
- [ ] 6.5 Make a development build on a phone: open the overlay with the corner button, audition during a round, use `Copy picks`, then import the text in the Editor and apply it
- [ ] 6.6 Make a release build and check the Editor.log build report: neither the candidates asset nor candidate-only clips are included, and no audition controls appear in the game
