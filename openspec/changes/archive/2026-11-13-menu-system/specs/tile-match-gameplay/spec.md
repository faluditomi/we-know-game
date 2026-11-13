## MODIFIED Requirements

### Requirement: Board completion
The game SHALL detect completion after every generated pair has been confirmed and SHALL emit the completion outcome no more than once for that board. After the completion outcome, the game SHALL leave the board and return the player to the title screen.

#### Scenario: Continue playing with pairs remaining
- **WHEN** a confirmed pair returns and at least one unmatched pair remains
- **THEN** the game restores normal tile selection for the next pair

#### Scenario: Complete the final pair
- **WHEN** the final unmatched pair is confirmed and returned
- **THEN** the board enters its completed state and the completion outcome occurs exactly once

#### Scenario: Leave a completed board
- **WHEN** the completion outcome has occurred
- **THEN** the board is cleared and the title screen is shown

### Requirement: Responsive board interaction
The game SHALL generate boards of the width and height the player chose, SHALL keep generated tiles visible and practically selectable across supported landscape aspect ratios for every supported width and height, and presentation behavior SHALL NOT depend solely on camera-distance enlargement.

#### Scenario: Generate a rectangular board
- **WHEN** a game starts with a chosen width and height
- **THEN** a board with exactly that many columns and rows is generated, containing width × height ÷ 2 distinct pairs

#### Scenario: Adapt to a supported display
- **WHEN** a board is generated or the supported display dimensions change
- **THEN** the board and interactive tile targets are framed within the playable area with space reserved for required presentation UI

#### Scenario: Frame wide and tall boards
- **WHEN** the board is wider than it is tall, or taller than it is wide
- **THEN** the whole board fits within the playable area without clipping on either axis

#### Scenario: Focus tiles independently of projection
- **WHEN** matched tiles enter the focused presentation
- **THEN** their visible emphasis is achieved through explicit composition, scale, framing, or equivalent presentation behavior that works with the configured camera projection
