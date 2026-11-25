## MODIFIED Requirements

### Requirement: Title screen
The game SHALL open on a title screen that shows the game title "We Know... Do You?" and offers PLAY, SETTINGS, and EXIT actions. No board SHALL be generated or selectable before the player chooses PLAY and a board size. PLAY SHALL lead to the board-size screen for the current board size mode: the grid size picker in free choice, or the preset choice screen in preset mode when two or more preset sizes are usable. In preset mode with exactly one usable preset size, PLAY SHALL start a game on that board straight away.

#### Scenario: Launch the game
- **WHEN** the game starts and its tile assets have loaded
- **THEN** the title screen is shown with the title "We Know... Do You?" and the PLAY, SETTINGS, and EXIT actions, and no game board is interactive

#### Scenario: Start choosing a board
- **WHEN** the player activates PLAY on the title screen while the board size mode is free choice
- **THEN** the grid size picker is shown

#### Scenario: Start choosing a preset board
- **WHEN** the player activates PLAY on the title screen while the board size mode is preset sizes and two or more preset sizes are usable
- **THEN** the preset choice screen is shown

#### Scenario: Start the only preset board
- **WHEN** the player activates PLAY on the title screen while the board size mode is preset sizes and exactly one preset size is usable
- **THEN** a game starts on that board without the preset choice screen being shown

#### Scenario: Assets fail to load
- **WHEN** the tile assets required for a game fail to load
- **THEN** the title screen remains available, PLAY does not start a game, and the failure is reported
