## Why

Board generation is complete, but players cannot yet interact with the generated tiles or resolve matches. The game now needs a mobile-first selection and match flow that remains readable and responsive on phones and tablets while supporting the intended dramatic correct-match presentation.

## What Changes

- Add pointer-driven tile selection for touch, pen, and mouse through the Unity Input System.
- Add an authoritative gameplay flow that reveals two tiles, determines whether they match, prevents invalid selections, and gates input while resolution is in progress.
- Add incorrect-match feedback that shakes and tints the selected tiles before returning them face-down.
- Add a correct-match presentation that brings the selected tiles into focus, replaces each tile's face-down logo with one half of the matched description, joins the tiles edge to edge so the halves form the complete description, and waits for a fresh tap to continue.
- Return confirmed matching tiles to their board positions with their logos visible and detect when all pairs are confirmed.
- Adapt the camera, board framing, input actions, tile prefab structure, colliders, and the presentation backdrop UI (dimmer and continuation prompt) as needed for reliable phone and tablet play.
- Show revealed logos upright and at their source aspect ratio, and use the black variants of the company logos.
- Keep the runtime structure compact: centralized match orchestration, tile-local visual behavior, and at most one focused presentation view rather than separate handlers for each interaction phase.
- Treat haptic feedback for blocked input as optional, rate-limited enhancement rather than required game feedback.
- Perform scene, prefab, Input Actions, and other Unity-serialized configuration through Unity Editor workflows rather than direct text edits.
- Explicit non-goals: scoring, timers, persistence, menus, localization infrastructure, portrait support, multiplayer, and a platform-specific advanced haptics package.

## Capabilities

### New Capabilities

- `tile-match-gameplay`: Mobile-first tile selection, pair resolution, correct and incorrect feedback, match presentation, continuation, and board completion behavior.

### Modified Capabilities

None. The project currently has no established OpenSpec capabilities.

## Impact

- Runtime source: `GameManager`, `TileController`, and potentially one presentation-view component; `TileView` remains responsible for generated sizing.
- Data and identity: each generated tile must retain reliable match identity and expose its complete description for presentation.
- Input: the Unity Input System actions and scene input wiring may be updated for a single authoritative pointer/select pathway across touch, pen, and mouse.
- Presentation: the game camera may remain orthographic or switch projection based on implementation validation; framing must work across supported landscape phone and tablet aspect ratios.
- Unity content: the tile prefab (including a 3D TextMeshPro description child on the face-down side), colliders/physics setup, scene UI, safe-area hierarchy, company tile logo assignments, and serialized references may change through supported Unity Editor workflows.
- Asset loading: existing Addressables usage remains the loading strategy; no `Resources.Load` path or new global singleton/save architecture is introduced.
- Dependencies: no third-party tweening, camera, or haptics package is required by this change.
