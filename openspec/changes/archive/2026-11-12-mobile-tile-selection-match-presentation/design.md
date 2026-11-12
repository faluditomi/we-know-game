## Context

See `proposal.md` for motivation and `specs/tile-match-gameplay/spec.md` for the behavior contract.

The current Unity project loads an Addressable tile prefab and `Tile` ScriptableObjects, generates a square grid in `GameManager`, configures each instance in `TileController`, and sizes it through `TileView`. Selection, pair resolution, presentation UI, and completion are not implemented. The tile prefab currently distributes colliders across visual children and has a non-kinematic child Rigidbody, while the scene camera is orthographic and the overlay Canvas is empty.

The implementation must support touch-first landscape phones and tablets, remain usable with pen and mouse in the Editor, and stay compact. Unity serialization files and generated project files cannot be edited as text; scene, prefab, Input Actions, and serialized-reference changes must be made through supported Unity Editor workflows.

## Goals / Non-Goals

**Goals:**

- Establish one deterministic state machine for selection, resolution, presentation, continuation, and completion.
- Keep input routing centralized and prevent duplicate or multi-touch selection.
- Give tiles a clear separation between board placement, selection hit target, and animated visuals.
- Produce the intended depth-heavy correct-match sequence while retaining readable, safe-area-aware description text.
- Make animation timing and intensity tunable through serialized configuration without adding a tweening dependency.
- Recover consistently from setup failure, board regeneration, component disablement, or sequence cancellation.

**Non-Goals:**

- Creating a generic input, animation, camera, haptics, or UI framework.
- Adding persistence, scoring, timers, menus, localization services, or portrait layouts.
- Making tile objects physics-driven.
- Introducing a global `GameManager` singleton or replacing the existing Addressables strategy.
- Authoring the final polished art, audio, or advanced native haptic patterns.

## Decisions

### 1. Keep match orchestration in `GameManager`

`GameManager` will own loading readiness, input subscription, pointer gesture validation, pair selection, the match-flow state, sequence cancellation, confirmed-pair counting, and completion. `TileController` will own retained tile data, tile eligibility, cached visual references, visual state, and tile-local animation operations. `TileView` will remain responsible only for generated sizing. One optional `MatchPresentationView` will bind the dimmer, continuation prompt, and safe-area root, and place its Screen Space - Camera canvas between the board and the presented tiles. The description itself is rendered by the matched tiles (see Decision 5).

The flow will use an explicit state enum equivalent to:

```text
Loading
AwaitingFirstTile
AwaitingSecondTile
ResolvingMismatch
PresentingMatch
AwaitingContinue
ReturningMatch
Complete
Failed
```

State changes occur synchronously before asynchronous animation starts. A sequence generation/token check prevents stale coroutines from unlocking or mutating a replacement board.

**Alternatives considered:** A separate match-flow coordinator offers stronger separation but adds scene lifecycle and setup overhead for this small project. Per-tile pointer handlers scatter input gating and allow race conditions. Animator/Timeline-led orchestration adds serialized assets and runtime binding complexity for dynamic tile origins and descriptions.

### 2. Add a dedicated board action map and central completed-tap recognition

The Input Actions asset will gain a compact board interaction map with pointer position and primary press actions bound to touch, pen, and mouse. `GameManager` will consume serialized `InputActionReference`s configured in the Unity Editor and will subscribe/unsubscribe with its lifecycle. Existing UI actions will remain available to the `InputSystemUIInputModule` and their references will be repaired if currently stale.

A primary press records the pointer identifier, start screen position, and tile under the pointer. Release accepts a tile only if it is the same eligible tile and movement remains within a device-appropriate tap threshold. Only one active primary gameplay pointer is tracked. UI-consumed presses are rejected before board raycasting. Continue uses the same pathway but is armed only after the presentation reaches rest and all active pointers have released, ensuring that the second-selection gesture cannot also continue.

Input remains enabled during locked states so attempted input can be consumed and optionally trigger throttled feedback; gameplay routing is controlled by flow state rather than by repeatedly enabling and disabling action maps.

**Alternatives considered:** Reusing broad default actions makes duplicate bindings and selection semantics harder to reason about. `IPointerClickHandler` on each tile requires per-tile handlers and a camera physics raycaster. Polling legacy input is incompatible with the configured Input System backend.

### 3. Use one root selection collider and a separate visual pivot

The tile prefab will be updated in the Unity Editor to follow this conceptual structure:

```text
TileRoot
├── BoxCollider
└── VisualRoot
    ├── Body
    ├── FaceDown visuals (face + WE-KNOW logo)
    ├── LogoUp visuals (tile logo)
    └── DescriptionText (3D TextMeshPro on the face-down side, inactive until presented)
```

`TileRoot` keeps board placement, generated scale, and a single BoxCollider. `VisualRoot` performs flip, shake, spin, and temporary tint behavior. Child face MeshColliders and the unnecessary dynamic Rigidbody will be removed; if a Rigidbody is required by a Unity interaction constraint, it will be kinematic and placed at the root. Raycasts use a dedicated tile layer mask and resolve the owning `TileController` from the hit hierarchy.

The logo-up quad is oriented so the revealed logo reads upright and unmirrored after the 180° flip about the board's vertical axis. `TileController` shows the sprite's own texture rect on that quad and scales it to keep the sprite's aspect ratio inside a configurable `logoBounds` box, so non-square logos are not stretched. The face-down WE-KNOW logo quad is scaled to its texture's aspect ratio. The face-down side also carries a `DescriptionText` child (3D TextMeshPro, twice the face width) that is only active while the tile is presented.

**Alternatives considered:** Keeping all child colliders causes ambiguous hits and unnecessary physics cost. Animating a dynamic Rigidbody conflicts with deterministic transform animation. Per-sprite quad sizes authored in the prefab would need a prefab variant per logo; fitting at setup time keeps one prefab.

### 4. Switch to a perspective camera with deterministic framing

The main gameplay camera will switch to perspective to support the intended depth motion and make tiles approaching the camera feel physical. The board remains front-facing to minimize perspective distortion. After generation and when the viewport changes, framing code will choose a camera distance from the board bounds, vertical field of view, aspect ratio, and configured padding. The near clip plane and presentation depth will be chosen so focused tiles cannot clip the camera.

Correctly matched tiles will move toward camera-relative presentation anchors and also receive explicit scale/composition animation. The effect therefore does not rely solely on perspective enlargement and remains controllable on wide phones and 4:3 tablets. The camera remains static during pair resolution to avoid motion discomfort and to keep unmatched board context stable.

**Alternatives considered:** Orthographic projection simplifies framing but cannot naturally convey moving toward the camera. Animating camera position or field of view makes the entire board move, complicates readability, and can cause discomfort. A separate presentation camera adds render-order, layer, and lifecycle complexity that is unnecessary initially.

### 5. Show the description on the matched tiles themselves

The selected real tile instances fly toward the camera and spin so their face-down side faces it. On each tile the face-down WE-KNOW logo is hidden and its `DescriptionText` is shown instead: the tile further left on screen shows the left half of `foundDescription` and the other tile the right half. Both texts hold the complete string in a rect twice the face width, offset so the seam lies on the tile's inner edge, and are clipped to the tile's face with TextMeshPro's clip rect (`_ClipRect` with the `UNITY_UI_CLIP_RECT` keyword, in the text's local space, on a per-tile font material instance). Each half reaches 2% past the seam so the anti-aliased clip edges overlap instead of leaving a visible gap. The tiles start a tile width apart and slide together until their inner edges meet at the screen centre, at which point the full description reads as one text and the continuation prompt appears. This avoids duplicated data and brittle semantic string splitting.

The screen-space view only dims the board and shows the prompt inside a safe-area root. Because its canvas renders through the gameplay camera at a plane distance between the presented tiles and the board, the dimmer covers the board but not the presented tiles. Text uses bounded autosizing and wrapping inside the tile faces; layout will be checked at minimum on 4:3, 16:9, and wide landscape aspect ratios.

**Alternatives considered:** Screen-space description panels attached to the tiles' inner edges read as a second pair of tiles and must be kept aligned with moving 3D tiles. Proxy tiles in a cinematic prefab duplicate material and match state. Hiding whole characters per side instead of clipping avoids the shader keyword but breaks the continuous look across the seam. Separately authored description fragments complicate content authoring and localization without adding necessary behavior.

### 6. Use source-driven, cancellable animation sequences

A single resolution coroutine owned by `GameManager` coordinates tile-local coroutine/tween operations and the presentation view. Durations, curves, rotation counts, mismatch displacement, tint, focus scale, depth, and feedback cooldown will be serialized settings. Time source will be chosen consistently; presentation animation should use unscaled time if future pause behavior can change `timeScale`.

Tile animation starts from captured poses rather than accumulated Euler offsets. The board-local position, rotation, and `TileView`-computed scale are captured after generation and restored exactly. Mismatch tint uses cached `MaterialPropertyBlock`s and restores original values. Cancellation on regeneration, disablement, or destruction restores a known visual state and invalidates stale completion callbacks.

**Alternatives considered:** A third-party tween library is unnecessary for the small number of dynamic transitions. Animator clips are awkward for arbitrary board origins and runtime presentation anchors. Uncoordinated coroutines on multiple components make lock lifetime and cancellation nondeterministic.

### 7. Retain tile asset identity and the complete description

`TileController` will retain its assigned `Tile` reference and expose read-only match identity and presentation data. Generated pairs currently duplicate the same ScriptableObject reference, so reference identity is sufficient. Names, colors, sprites, and descriptions will not be used as match keys. If content loading later creates distinct equivalent assets or persistence is introduced, a separate stable ID can replace this rule in a future change.

The existing `foundDescription` remains the canonical complete string. The two halves are a visual clipping treatment, not separate data fields.

### 8. Preserve Addressables and add explicit setup failure handling

The existing direct Addressables strategy remains in place. Both tile-prefab and tile-data operations must be checked for success before generation or input activation. Handles remain owned by `GameManager` and are released once when valid. Setup moves to a controlled readiness/failure path instead of allowing an `async void` failure to leave partial state.

No save data is added. If progress persistence is introduced later, it must use serializable primitive DTO fields and stable IDs rather than runtime object or ScriptableObject references.

### 9. Treat haptics as an optional boundary

Blocked input may call one small, throttled feedback method. The initial implementation can use coarse supported device feedback and no-op elsewhere. Feedback stops or resets on disable and application pause. It is never the only indication that input is locked, and no platform-specific plugin is introduced.

## Risks / Trade-offs

- **[Perspective framing makes tile sizing less predictable]** → Calculate camera distance from generated board bounds and aspect ratio, then validate the largest supported grid at representative device ratios.
- **[Description halves can show a seam or misalign]** → Both halves come from the same string and rect size on identical tiles, joined at the screen centre; clip regions overlap slightly at the seam.
- **[Multiple touch contacts can race selection]** → Track one primary gameplay pointer and transition state before starting reveal or resolution animation.
- **[A press can activate both second selection and continue]** → Arm continuation only after the presentation rests and all existing pointers have released.
- **[Coroutines can outlive regenerated tiles]** → Cancel the owning sequence, increment a generation token, clear references, and reset state before replacing board instances.
- **[Long descriptions can overflow or become small]** → Constrain the tile text with margins, wrapping, and bounded autosizing, and validate representative maximum content.
- **[Prefab hierarchy changes can break name-based lookups]** → Replace repeated hierarchy searches with serialized or cached typed references configured and validated in the Editor.
- **[Material mutation can allocate instances]** → Use cached `MaterialPropertyBlock`s for per-tile logo and tint changes.
- **[Coarse device vibration may be intrusive]** → Keep it optional, short, rate-limited, and nonessential.
- **[Centralizing flow broadens `GameManager`]** → Keep tile visuals and UI bindings behind narrow component APIs; split out a coordinator only if later features materially expand the flow.

## Migration Plan

1. Add source APIs and flow state while keeping gameplay input disabled by default.
2. Through the Unity Input Actions Editor, add and bind the board actions and repair the scene UI module references.
3. Through Prefab Mode, restructure the tile hit target and visual pivot, remove conflicting physics components, and wire serialized references.
4. Through the Scene Editor, switch and configure the perspective camera, assign the tile layer mask and Input Action references, and add the safe-area-aware dimmer and continuation-prompt hierarchy.
5. Enable the flow after successful Addressables setup and validate generation, selection, both resolution branches, cancellation, and completion in Play Mode.
6. Validate touch behavior and layout on representative landscape phone/tablet resolutions and at least one physical touch device before considering the change complete.

Rollback consists of reverting the source changes and reverting the Unity Editor-authored Input Actions, prefab, and scene changes together. No data migration or save compatibility work is required.
