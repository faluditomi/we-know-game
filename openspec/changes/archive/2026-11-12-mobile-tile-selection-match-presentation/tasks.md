## 1. Runtime Foundations

- [x] 1.1 Refactor Addressables setup into an explicit ready/failed lifecycle that validates the board root and both load operations before generation or gameplay input can begin.
- [x] 1.2 Correct tile-variation random selection so every available variation can be selected, and make board replacement cancel active gameplay before old instances are destroyed.
- [x] 1.3 Add a compact match-flow state model, sequence-generation token, selected-pair storage, confirmed-pair count, and exactly-once board-completion outcome to `GameManager`.
- [x] 1.4 Add serialized, validated configuration for tile raycasting, tap movement threshold, animation timings/curves, presentation framing, and optional blocked-input feedback cooldown.

## 2. Tile Runtime and Prefab Structure

- [x] 2.1 Extend `TileController` to retain its assigned `Tile`, expose read-only match identity and description data, track selectable/revealed/matched state, and cache original visual properties.
- [x] 2.2 Implement cancellable tile-local operations for reveal, conceal, mismatch shake/tint, focused presentation movement/spin, exact board-pose restoration, and permanent logo-up matched state.
- [x] 2.3 Use cached `MaterialPropertyBlock`s for per-instance logo and color changes and verify that cancellation restores a consistent visual state without material-instance churn.
- [x] 2.4 In Unity Prefab Mode, restructure the tile into a board-placement root with one selection BoxCollider and a separate animated visual root; remove child MeshColliders and the dynamic Rigidbody, or make a required root Rigidbody kinematic.
- [x] 2.5 In Unity Prefab Mode, configure the face-down visuals (face and WE-KNOW logo at its texture aspect), an upright logo-up quad, and a `DescriptionText` 3D TextMeshPro child (twice the face width, inactive by default) on the face-down side, then wire all typed `TileController` references.
- [x] 2.6 Fit each revealed logo to its sprite's aspect ratio inside a configurable `logoBounds` box using the sprite's texture rect, keep transparent logo areas fully invisible (no preserved specular on the logo material), and assign the black logo variants to the company tiles.

## 3. Mobile-First Input

- [x] 3.1 In the Unity Input Actions Editor, add one board interaction map with pointer-position and primary-press actions bound for primary touch, pen, and mouse without duplicating gameplay activation routes.
- [x] 3.2 In the Unity Scene Editor, assign the board action references and tile layer mask to `GameManager`, and repair the `InputSystemUIInputModule` action references against the active Input Actions asset.
- [x] 3.3 Implement lifecycle-safe action subscription and one-pointer press/release tap recognition that validates movement tolerance, same-tile release, UI consumption, tile eligibility, and child-hit ownership.
- [x] 3.4 Route valid completed taps by match-flow state, reject simultaneous or stale contacts deterministically, and arm tap-anywhere continuation only after all pre-existing pointers have released.

## 4. Selection and Incorrect Resolution

- [x] 4.1 Implement first- and second-tile selection transitions, preventing the same tile, a matched tile, an animating tile, or a third tile from entering the pair.
- [x] 4.2 Compare retained tile identity rather than names or visual fields and enter the locked resolution state synchronously before awaiting animation.
- [x] 4.3 Implement the complete incorrect-pair sequence: readable reveal, simultaneous shake and red tint, original-color restoration, face-down return, pair clearing, and input restoration exactly once.
- [x] 4.4 Verify that taps during loading and incorrect resolution cannot alter the pair or unlock the state early.

## 5. Camera, Board Framing, and Presentation UI

- [x] 5.1 Implement perspective board-framing calculations from generated board bounds, field of view, aspect ratio, and configured padding, including viewport-change recalculation and near-plane-safe presentation depth.
- [x] 5.2 In the Unity Scene Editor, switch the gameplay camera to perspective, configure initial field of view and clipping planes, and wire framing references without adding a separate presentation camera.
- [x] 5.3 Implement one focused `MatchPresentationView` that controls the dimmer, continuation prompt, safe-area root, and the canvas depth between the presented tiles and the board.
- [x] 5.4 In the Unity Scene Editor, create and wire the safe-area-aware dimmer and prompt hierarchy using TMP/uGUI, with no description panels, jagged masks, or tile slots.
- [x] 5.5 Show complementary halves of the description on the two matched tiles' face-down sides (TMP clip rect in text-local space with a small seam overlap, bounded wrapping/autosizing), hiding the face-down logo while presented and restoring it afterwards.

## 6. Correct Match and Completion Flow

- [x] 6.1 Implement the correct-pair sequence that saves both generated board poses, moves and spins the real tiles toward the screen centre, shows each tile's description half, and slides the tiles together edge to edge so the halves form the complete description.
- [x] 6.2 Enter the awaiting-continue state only after the presentation rests, display the prompt, and consume exactly one fresh primary activation to begin dismissal.
- [x] 6.3 Return both tiles to their exact generated positions, rotations, and scales, leave their logos visible, mark them matched, and restore selection only when unmatched pairs remain.
- [x] 6.4 Emit the completed-board outcome exactly once after the final pair returns and keep matched or completed tiles unavailable for further selection.

## 7. Resilience and Optional Feedback

- [x] 7.1 Cancel active selection and presentation work safely on board regeneration, setup failure, disablement, destruction, and application pause without leaving stale callbacks or permanently locked input.
- [x] 7.2 Add optional throttled blocked-input feedback with a safe unsupported-device no-op and visible feedback that remains sufficient when vibration is unavailable or disabled.
- [x] 7.3 Verify Addressables handles remain owned and released exactly once and that no `Resources.Load`, new global singleton, or save architecture is introduced.

## 8. Verification

- [x] 8.1 Add automated source-level tests where practical for match identity, same-tile rejection, locked-state rejection, state transitions, confirmed-pair counting, completion idempotence, and cancellation-token behavior.
- [x] 8.2 Run Unity compilation and relevant automated tests, confirm no generated IDE files were intentionally modified, and resolve all errors and warnings introduced by the change.
- [x] 8.3 In Play Mode, validate successful and failed setup, first/second selection, simultaneous input rejection, incorrect resolution, correct presentation, fresh-tap continuation, regeneration cancellation, and final completion.
- [x] 8.4 Validate mouse and pen behavior in the Editor plus primary-touch behavior on at least one physical device, including touches over blocking UI and touches attempted during locked states.
- [x] 8.5 Validate board framing, safe-area layout, text readability, tile target size, and presentation clipping at representative 4:3 tablet, 16:9, and wide landscape phone aspect ratios in both supported landscape orientations.
- [x] 8.6 Reopen the scene and prefab after serialization/domain reload and confirm all Unity Editor-authored references, Input Actions, layers, collider setup, camera settings, and presentation bindings remain valid.
