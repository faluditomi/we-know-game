## MODIFIED Requirements

### Requirement: Pause menu
During a game the player SHALL be able to open a pause menu that offers CONTINUE, SETTINGS, and MAIN MENU. While the pause menu, or the settings screen opened from it, is shown, board input and match animations SHALL be suspended.

#### Scenario: Open the pause menu
- **WHEN** the player activates the on-screen pause control, or the platform back/escape action, during a game
- **THEN** the pause menu is shown and tile selection and match animations are suspended

#### Scenario: Continue the game
- **WHEN** the player activates CONTINUE
- **THEN** the pause menu closes and the game resumes from the same state, including any animation that was in progress

#### Scenario: Open settings during a game
- **WHEN** the player activates SETTINGS in the pause menu
- **THEN** the settings screen is shown centred on the screen over the paused board, like the pause menu, and the game stays suspended

#### Scenario: Return from settings to the pause menu
- **WHEN** the player activates the settings screen's back action, or the platform back/escape action, after opening settings from the pause menu
- **THEN** the pause menu is shown again with the same board, still paused

#### Scenario: Return to the main menu
- **WHEN** the player activates MAIN MENU in the pause menu
- **THEN** the current board is discarded without counting as completed and the title screen is shown

#### Scenario: Paused input does not reach the board
- **WHEN** the player taps where a tile is visible behind the pause menu or the settings screen opened from it
- **THEN** no tile is selected

### Requirement: Settings
The settings screen SHALL be reachable from the title screen and from the pause menu, and its back action SHALL return to the screen it was opened from. It SHALL list, from top to bottom, a language dropdown, a vibration on/off toggle, sound and music volume controls, and an inactivity timeout control, with every label and control aligned to shared columns and each control vertically centred on its label. Changes SHALL apply immediately and SHALL be kept between sessions. The vibration setting SHALL control the game's blocked-input vibration. The inactivity timeout SHALL default to 60 seconds, SHALL be adjustable between 15 and 300 seconds in 5-second steps, SHALL show its current value in seconds, and SHALL be followed by a short explanation in smaller text of what the timeout does. The language dropdown SHALL offer English and Hungarian, SHALL list each language by its own name ("English", "Magyar"), and SHALL show the language currently in use.

#### Scenario: Turn vibration off
- **WHEN** the player turns vibration off and later taps while board input is locked
- **THEN** the device does not vibrate

#### Scenario: Settings persist
- **WHEN** the player changes vibration, a volume, the inactivity timeout, or the language and restarts the game
- **THEN** the settings screen shows the changed values

#### Scenario: Default inactivity timeout
- **WHEN** the game runs for the first time on a device
- **THEN** the inactivity timeout is 60 seconds

#### Scenario: Change the inactivity timeout
- **WHEN** the player sets the inactivity timeout to 30 seconds
- **THEN** the settings screen shows 30 seconds and the next game returns to the title after 30 seconds without input

#### Scenario: Setting order and explanation
- **WHEN** the settings screen is shown
- **THEN** the language control is first, the inactivity timeout is last, and a smaller line of text below the inactivity timeout explains that the game returns to the title after that long without input

#### Scenario: Language options
- **WHEN** the player opens the language dropdown
- **THEN** it lists "English" and "Magyar", whatever the current language is

#### Scenario: Choose a language
- **WHEN** the player selects "Magyar" in the language dropdown
- **THEN** the dropdown shows "Magyar" and the game's text switches to Hungarian immediately

#### Scenario: Leave settings
- **WHEN** the player activates the settings screen's back action after opening settings from the title screen
- **THEN** the title screen is shown

### Requirement: Inactivity return to the title
While a game is ongoing, playing, paused, or showing the settings screen opened from the pause menu, the game SHALL return to the title screen once no player input has been received for the configured inactivity timeout. The return SHALL happen immediately, without a warning, countdown, or confirmation, SHALL discard the board without counting it as completed, and SHALL show the normal title. Any touch, pen, mouse, or keyboard input SHALL restart the timeout. The timeout SHALL NOT apply on the title or picker screens, or on the settings screen opened from the title.

#### Scenario: Idle during a game
- **WHEN** a game is being played and no input is received for the configured timeout
- **THEN** the board is discarded and the title screen is shown with "We Know... Do You?"

#### Scenario: Idle while paused
- **WHEN** the pause menu, or the settings screen opened from it, is shown and no input is received for the configured timeout
- **THEN** the board is discarded and the title screen is shown

#### Scenario: Input restarts the timeout
- **WHEN** the player touches the screen, uses a mouse or pen, or presses a key before the timeout elapses, including taps the board ignores during an animation
- **THEN** the timeout starts again from zero and the game continues

#### Scenario: Menus are not reset
- **WHEN** the title or picker screen, or the settings screen opened from the title, is shown and no input is received for longer than the timeout
- **THEN** the screen stays as it is

#### Scenario: No prompt before returning
- **WHEN** the timeout elapses during a game
- **THEN** no warning, countdown, or confirmation is shown before the title screen appears
