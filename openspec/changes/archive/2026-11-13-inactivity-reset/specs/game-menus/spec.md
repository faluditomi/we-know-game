## ADDED Requirements

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

## MODIFIED Requirements

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
