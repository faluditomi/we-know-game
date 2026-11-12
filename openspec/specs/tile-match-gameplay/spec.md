## Purpose

Defines the player-visible behavior for selecting generated tiles, resolving pairs, presenting matched content, and completing a board across supported landscape phones and tablets.

## Requirements

### Requirement: Cross-device tile selection
The game SHALL allow a player to select an eligible board tile with a primary touch, pen, or mouse activation through one authoritative interaction pathway. An activation consumed by blocking user interface content SHALL NOT select a board tile.

#### Scenario: Select a face-down tile
- **WHEN** the board is ready and the player completes a valid primary activation on an eligible face-down tile
- **THEN** exactly that tile is selected and begins revealing its logo

#### Scenario: Ignore non-tile activation
- **WHEN** the player activates empty board space or blocking user interface content while selecting tiles
- **THEN** no tile is selected and the current selection remains unchanged

#### Scenario: Serialize simultaneous touch input
- **WHEN** multiple pointers attempt to select tiles during the same interaction window
- **THEN** the game accepts at most one valid tile selection and preserves a deterministic selection state

### Requirement: Selection eligibility and pair formation
The game SHALL form a candidate pair from two different eligible tiles and SHALL reject tiles that are already matched, already selected, or currently animating.

#### Scenario: Select a second eligible tile
- **WHEN** one tile is selected and the player selects a different eligible tile
- **THEN** the second tile reveals and the two tiles become the candidate pair

#### Scenario: Reject the same tile twice
- **WHEN** one tile is selected and the player activates that same tile again
- **THEN** the game does not place the tile in the second selection slot

#### Scenario: Reject a confirmed tile
- **WHEN** the player activates a tile that has already been confirmed as matched
- **THEN** the tile remains in its matched state and the current selection remains unchanged

### Requirement: Resolution input gating
The game SHALL prevent board selections while loading, resolving a pair, presenting a correct match, or returning matched tiles. Blocked input SHALL NOT alter gameplay state, and optional device feedback SHALL be rate-limited and SHALL NOT be required to understand the lock.

#### Scenario: Attempt selection during resolution
- **WHEN** the player activates the screen while a pair-resolution animation is in progress
- **THEN** no tile selection or pair-resolution state changes occur

#### Scenario: Unsupported or disabled haptics
- **WHEN** blocked-input feedback is disabled or unsupported by the active device
- **THEN** resolution continues correctly using visible feedback alone

### Requirement: Incorrect pair resolution
The game SHALL visibly identify an incorrect candidate pair, return both tiles face-down, clear the pair, and restore tile selection.

#### Scenario: Resolve an incorrect pair
- **WHEN** the second revealed tile does not share the first tile's match identity
- **THEN** both tiles visibly shake and receive temporary incorrect-color feedback before returning face-down

#### Scenario: Resume after incorrect feedback
- **WHEN** both incorrect tiles finish returning face-down
- **THEN** neither tile remains selected and the player can begin a new pair selection

### Requirement: Correct match presentation
The game SHALL present a correct candidate pair as a focused sequence in which each of the two selected tiles shows one visual half of the matched tile's complete description in place of its face-down logo, and the two tiles join so the halves form the complete description. No additional tile-like panels SHALL appear beside the presented tiles.

#### Scenario: Present a correct pair
- **WHEN** the second revealed tile shares the first tile's match identity
- **THEN** both tiles move from their board positions into a focused presentation, each shows one half of the description on the side that faced down, and the complete matched description is revealed when the tiles join edge to edge

#### Scenario: Preserve readable content
- **WHEN** a correct match is presented on any supported landscape phone or tablet aspect ratio
- **THEN** the matched description and continuation prompt remain legible and unobstructed by display safe-area exclusions

### Requirement: Fresh activation to continue
The game SHALL wait after the correct-match presentation is ready and SHALL advance only after a fresh primary activation made after continuation becomes available.

#### Scenario: Show continuation affordance
- **WHEN** the correct-match presentation reaches its readable resting state
- **THEN** the game displays a clear tap-anywhere continuation prompt

#### Scenario: Do not reuse the selection gesture
- **WHEN** the pointer gesture that selected the second tile remains active as the presentation starts
- **THEN** that gesture does not dismiss the presentation

#### Scenario: Continue from the presentation
- **WHEN** continuation is available and the player performs a fresh primary activation anywhere in the presentation area
- **THEN** the continuation prompt is consumed once and the matched tiles begin returning to the board

### Requirement: Persistent matched board state
The game SHALL return confirmed matching tiles to their original board placements with their logos visible and SHALL keep them unavailable for later selections.

#### Scenario: Return a confirmed pair
- **WHEN** the player continues from a correct-match presentation
- **THEN** both tiles return to their saved board placements, remain logo-up, and become confirmed matches

#### Scenario: Display revealed logos correctly
- **WHEN** a tile's logo is revealed or remains visible after a confirmed match
- **THEN** the logo reads upright and unmirrored and keeps its source aspect ratio

#### Scenario: Preserve generated layout
- **WHEN** confirmed tiles return after presentation
- **THEN** their position, orientation, and generated board scale are restored without disturbing unmatched tiles

### Requirement: Board completion
The game SHALL detect completion after every generated pair has been confirmed and SHALL emit the completion outcome no more than once for that board.

#### Scenario: Continue playing with pairs remaining
- **WHEN** a confirmed pair returns and at least one unmatched pair remains
- **THEN** the game restores normal tile selection for the next pair

#### Scenario: Complete the final pair
- **WHEN** the final unmatched pair is confirmed and returned
- **THEN** the board enters its completed state and the completion outcome occurs exactly once

### Requirement: Responsive board interaction
The game SHALL keep generated tiles visible and practically selectable across supported landscape aspect ratios, and presentation behavior SHALL NOT depend solely on camera-distance enlargement.

#### Scenario: Adapt to a supported display
- **WHEN** a board is generated or the supported display dimensions change
- **THEN** the board and interactive tile targets are framed within the playable area with space reserved for required presentation UI

#### Scenario: Focus tiles independently of projection
- **WHEN** matched tiles enter the focused presentation
- **THEN** their visible emphasis is achieved through explicit composition, scale, framing, or equivalent presentation behavior that works with the configured camera projection

### Requirement: Safe initialization and reset
The game SHALL accept gameplay input only after required tile data, visual assets, board references, and generated tile instances are ready. Regeneration or teardown SHALL cancel active interaction and presentation state without leaving input permanently locked.

#### Scenario: Input before setup completes
- **WHEN** the player activates the screen before the board has completed successful setup
- **THEN** no tile selection occurs

#### Scenario: Setup failure
- **WHEN** a required board reference or required asset fails to load
- **THEN** the game remains non-interactive and reports the failure without beginning a partially configured match flow

#### Scenario: Reset during an active sequence
- **WHEN** the board is regenerated or torn down during selection or pair resolution
- **THEN** active selections and presentation work are cancelled and the replacement board starts from a consistent non-selected state
