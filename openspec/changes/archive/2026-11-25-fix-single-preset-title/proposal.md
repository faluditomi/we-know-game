## Why

The Title screen requirement says PLAY shows the preset choice screen whenever at least one preset size is usable. But the Preset choice screen requirement, and the game, start the board straight away when exactly one preset is usable. The two requirements contradict each other.

## What Changes

- The Title screen requirement and its "Start choosing a preset board" scenario now apply when two or more preset sizes are usable.
- A new scenario covers a single usable preset: PLAY starts that board directly.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `game-menus`: the Title screen requirement now matches the single-preset behaviour already in the Preset choice screen requirement.

## Impact

Spec text only. The game already behaves this way, so no code, scene or saved data changes.

## Non-goals

- Any change to how presets work.
