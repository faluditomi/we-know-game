## MODIFIED Requirements

### Requirement: Board completion
The game SHALL detect completion after every generated pair has been confirmed and SHALL emit the completion outcome no more than once for that board. After the completion outcome, the game SHALL play the victory moment on the completed board and SHALL then leave the board and return the player to the title screen.

#### Scenario: Continue playing with pairs remaining
- **WHEN** a confirmed pair returns and at least one unmatched pair remains
- **THEN** the game restores normal tile selection for the next pair

#### Scenario: Complete the final pair
- **WHEN** the final unmatched pair is confirmed and returned
- **THEN** the board enters its completed state and the completion outcome occurs exactly once

#### Scenario: Leave a completed board
- **WHEN** the completion outcome has occurred and the victory moment has finished
- **THEN** the board is cleared and the title screen is shown

## ADDED Requirements

### Requirement: Round statistics
For each board the game SHALL record the number of pairs, the play time, and the number of misses. Play time SHALL run from the start of the board to the completion outcome and SHALL exclude time spent paused. A miss SHALL be counted for every revealed pair that did not match.

#### Scenario: Count misses
- **WHEN** the player reveals three non-matching pairs before completing a board
- **THEN** the round's miss count is 3

#### Scenario: Paused time excluded
- **WHEN** the player pauses for 20 seconds during a board that is otherwise played in 60 seconds
- **THEN** the round's play time is 60 seconds

#### Scenario: New board resets the statistics
- **WHEN** a new board starts
- **THEN** its time and miss count start from zero
