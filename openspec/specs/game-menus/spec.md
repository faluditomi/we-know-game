## Purpose

Defines the menu flow around a match: the title screen and its success variant, choosing a board size, pausing, settings, exiting, and the menu's live background and visual identity on landscape phones and tablets.

## Requirements

### Requirement: Title screen
The game SHALL open on a title screen that shows the game title "We Know... Do You?" and offers PLAY, SETTINGS, and EXIT actions. No board SHALL be generated or selectable before the player chooses PLAY and a board size.

#### Scenario: Launch the game
- **WHEN** the game starts and its tile assets have loaded
- **THEN** the title screen is shown with the title "We Know... Do You?" and the PLAY, SETTINGS, and EXIT actions, and no game board is interactive

#### Scenario: Start choosing a board
- **WHEN** the player activates PLAY on the title screen
- **THEN** the grid size picker is shown

#### Scenario: Assets fail to load
- **WHEN** the tile assets required for a game fail to load
- **THEN** the title screen remains available, PLAY does not start a game, and the failure is reported

### Requirement: Replaceable title presentation
The title SHALL be readable as styled text when no title artwork is configured and SHALL display configured title artwork in its place when artwork is provided, without changing the menu flow.

#### Scenario: No artwork configured
- **WHEN** the title screen is shown and no title artwork is configured
- **THEN** the title is displayed as text in the menu style

#### Scenario: Artwork configured
- **WHEN** title artwork is configured for the normal or success title
- **THEN** that artwork is shown for that title state instead of the text

### Requirement: Success title after a completed game
After the player completes a board, the game SHALL return to the title screen showing "We Know... And Now You Do Too!" for a configurable duration and SHALL then revert to "We Know... Do You?". The title screen SHALL stay usable while the success title shows.

#### Scenario: Return after completing a board
- **WHEN** the final pair of a board is confirmed and returned to the board
- **THEN** the game returns to the title screen and the title reads "We Know... And Now You Do Too!"

#### Scenario: Success title reverts
- **WHEN** the configured success-title duration has elapsed on the title screen
- **THEN** the title reverts to "We Know... Do You?"

#### Scenario: Configure the duration
- **WHEN** the success-title duration is changed in the game's configuration
- **THEN** the success title shows for the new duration on the next completed game

#### Scenario: Abandoned games do not celebrate
- **WHEN** the player leaves a board through MAIN MENU before completing it
- **THEN** the title screen shows "We Know... Do You?"

### Requirement: Grid size picker
The grid size picker SHALL show a grid of the largest supported board size and SHALL let the player choose a board by pressing on a cell and dragging to another cell, selecting the rectangle spanned by the two cells. Selected cells SHALL be highlighted while dragging. The largest supported size SHALL be the largest square grid whose cell count does not exceed twice the number of available tile designs.

#### Scenario: Show the largest grid
- **WHEN** the picker opens with 8 available tile designs
- **THEN** a 4×4 grid of cells is shown

#### Scenario: Drag a selection
- **WHEN** the player presses on one cell and drags across the grid
- **THEN** the rectangle between the press cell and the cell under the pointer is highlighted and updates as the pointer moves

#### Scenario: Leave the picker
- **WHEN** the player activates the picker's back action
- **THEN** the title screen is shown and no board is generated

### Requirement: Valid board selection
Releasing a drag SHALL start a game with a board of the selected width and height only when the selection contains an even number of cells and at least 4 cells (2 pairs). Any other release SHALL start nothing, keep the picker open, and tell the player why. The picker SHALL indicate during the drag whether the current selection is valid.

#### Scenario: Start a valid board
- **WHEN** the player releases a 3×4 selection
- **THEN** the menu closes and a game starts on a 3-column by 4-row board

#### Scenario: Selection too small
- **WHEN** the player releases a selection of fewer than 4 cells
- **THEN** no game starts, the picker stays open, and the player is told the selection is too small

#### Scenario: Odd number of cells
- **WHEN** the player releases a 3×3 selection
- **THEN** no game starts, the picker stays open, and the player is told the board needs an even number of tiles

#### Scenario: Validity shown while dragging
- **WHEN** the current drag selection would not be accepted on release
- **THEN** the highlight shows it as invalid, distinct from the valid highlight

### Requirement: Pause menu
During a game the player SHALL be able to open a pause menu that offers CONTINUE and MAIN MENU. While the pause menu is open, board input and match animations SHALL be suspended.

#### Scenario: Open the pause menu
- **WHEN** the player activates the on-screen pause control, or the platform back/escape action, during a game
- **THEN** the pause menu is shown and tile selection and match animations are suspended

#### Scenario: Continue the game
- **WHEN** the player activates CONTINUE
- **THEN** the pause menu closes and the game resumes from the same state, including any animation that was in progress

#### Scenario: Return to the main menu
- **WHEN** the player activates MAIN MENU in the pause menu
- **THEN** the current board is discarded without counting as completed and the title screen is shown

#### Scenario: Paused input does not reach the board
- **WHEN** the player taps where a tile is visible behind the pause menu
- **THEN** no tile is selected

### Requirement: Settings
The settings screen SHALL provide a vibration on/off toggle, sound and music volume controls, and an inactivity timeout control, SHALL apply changes immediately, and SHALL keep the values between sessions. The vibration setting SHALL control the game's blocked-input vibration. The inactivity timeout SHALL default to 60 seconds, SHALL be adjustable between 15 and 300 seconds in 5-second steps, and SHALL show its current value in seconds.

#### Scenario: Turn vibration off
- **WHEN** the player turns vibration off and later taps while board input is locked
- **THEN** the device does not vibrate

#### Scenario: Settings persist
- **WHEN** the player changes vibration, a volume, or the inactivity timeout and restarts the game
- **THEN** the settings screen shows the changed values

#### Scenario: Default inactivity timeout
- **WHEN** the game runs for the first time on a device
- **THEN** the inactivity timeout is 60 seconds

#### Scenario: Change the inactivity timeout
- **WHEN** the player sets the inactivity timeout to 30 seconds
- **THEN** the settings screen shows 30 seconds and the next game returns to the title after 30 seconds without input

#### Scenario: Leave settings
- **WHEN** the player activates the settings screen's back action
- **THEN** the title screen is shown

### Requirement: Inactivity return to the title
While a game is ongoing, playing or paused, the game SHALL return to the title screen once no player input has been received for the configured inactivity timeout. The return SHALL happen immediately, without a warning, countdown, or confirmation, SHALL discard the board without counting it as completed, and SHALL show the normal title. Any touch, pen, mouse, or keyboard input SHALL restart the timeout. The timeout SHALL NOT apply on the title, picker, or settings screens.

#### Scenario: Idle during a game
- **WHEN** a game is being played and no input is received for the configured timeout
- **THEN** the board is discarded and the title screen is shown with "We Know... Do You?"

#### Scenario: Idle while paused
- **WHEN** the pause menu is open and no input is received for the configured timeout
- **THEN** the board is discarded and the title screen is shown

#### Scenario: Input restarts the timeout
- **WHEN** the player touches the screen, uses a mouse or pen, or presses a key before the timeout elapses, including taps the board ignores during an animation
- **THEN** the timeout starts again from zero and the game continues

#### Scenario: Menus are not reset
- **WHEN** the title, picker, or settings screen is shown and no input is received for longer than the timeout
- **THEN** the screen stays as it is

#### Scenario: No prompt before returning
- **WHEN** the timeout elapses during a game
- **THEN** no warning, countdown, or confirmation is shown before the title screen appears

### Requirement: Exit
EXIT SHALL close the game on platforms where an application may quit itself, SHALL end Play Mode in the Unity Editor, and SHALL be hidden on platforms where self-quitting is not appropriate.

#### Scenario: Exit on a supported platform
- **WHEN** the player activates EXIT on a platform that supports quitting, such as Android or desktop
- **THEN** the game closes

#### Scenario: Platform without self-quit
- **WHEN** the title screen is shown on iOS
- **THEN** the EXIT action is not shown

### Requirement: Live menu background
Menu screens SHALL show an animated background made of game tiles that drift slowly while random tiles flip to reveal their logos and flip back. The background SHALL stay behind a readability treatment so menu text remains legible, SHALL stop animating and be hidden during gameplay, and SHALL play a celebratory wave of tile flips while the success title shows.

#### Scenario: Idle on a menu
- **WHEN** a menu screen is shown
- **THEN** the tile background drifts and individual tiles periodically flip to show a logo and flip back

#### Scenario: During gameplay
- **WHEN** a game board is active
- **THEN** the menu background is hidden and not animating

#### Scenario: Celebrate a completed game
- **WHEN** the success title is shown
- **THEN** the background plays waves of tile flips that spread outward from the bottom-left corner of the screen, for the duration of the success title, without fronts starting from other points

#### Scenario: Readable menus
- **WHEN** a menu screen is shown on any supported landscape phone or tablet aspect ratio
- **THEN** titles, buttons, and picker cells remain legible against the moving background and within the display safe area

### Requirement: Menu visual identity
Menu screens SHALL use the We Know logo teal (#128580) as their accent colour for interactive and highlighted elements, including buttons, the selected picker cells, toggles, and sliders.

#### Scenario: Accent colour on interactive elements
- **WHEN** any menu screen is shown
- **THEN** its buttons, toggles, sliders, and valid picker selection use #128580 as the accent colour

### Requirement: Touch-first menu input
Menu actions SHALL respond to touch, pen, and mouse through the same input system as gameplay, SHALL be large enough to activate reliably on a phone, and a single press SHALL NOT activate both a menu action and a board tile.

#### Scenario: Start a game without a stray tile tap
- **WHEN** the release that starts a game from the picker is over a cell where a tile will appear
- **THEN** no tile is selected by that release
