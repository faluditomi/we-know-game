## MODIFIED Requirements

### Requirement: Title screen
The game SHALL open on a title screen that shows the game title "We Know... Do You?" and offers PLAY, SETTINGS, and EXIT actions. No board SHALL be generated or selectable before the player chooses PLAY and a board size. PLAY SHALL lead to the board-size screen for the current board size mode: the grid size picker in free choice, or the preset choice screen in preset mode when at least one preset size is usable.

#### Scenario: Launch the game
- **WHEN** the game starts and its tile assets have loaded
- **THEN** the title screen is shown with the title "We Know... Do You?" and the PLAY, SETTINGS, and EXIT actions, and no game board is interactive

#### Scenario: Start choosing a board
- **WHEN** the player activates PLAY on the title screen while the board size mode is free choice
- **THEN** the grid size picker is shown

#### Scenario: Start choosing a preset board
- **WHEN** the player activates PLAY on the title screen while the board size mode is preset sizes and at least one preset size is usable
- **THEN** the preset choice screen is shown

#### Scenario: Assets fail to load
- **WHEN** the tile assets required for a game fail to load
- **THEN** the title screen remains available, PLAY does not start a game, and the failure is reported

### Requirement: Settings
The settings screen SHALL be reachable from the title screen and from the pause menu, and its back action SHALL return to the screen it was opened from. It SHALL list, from top to bottom, a language dropdown, a board size mode control, a vibration on/off toggle, a battery saver on/off toggle, sound and music volume controls, and an inactivity timeout control, with every label and control aligned to shared columns and each control vertically centred on its label. The volume and inactivity sliders SHALL each span at least half of the settings panel's width so they are easy to drag on a phone. Changes SHALL apply immediately and SHALL be kept between sessions. The vibration setting SHALL control the game's blocked-input vibration. The inactivity timeout SHALL default to 60 seconds, SHALL be adjustable between 15 and 300 seconds in 5-second steps, SHALL show its current value in seconds, and SHALL be followed by a short explanation in smaller text of what the timeout does. The language dropdown SHALL offer English and Hungarian, SHALL list each language by its own name ("English", "Magyar"), and SHALL show the language currently in use.

#### Scenario: Turn vibration off
- **WHEN** the player turns vibration off and later taps while board input is locked
- **THEN** the device does not vibrate

#### Scenario: Settings persist
- **WHEN** the player changes vibration, battery saver, a volume, the inactivity timeout, the language, or the board size mode and restarts the game
- **THEN** the settings screen shows the changed values

#### Scenario: Default inactivity timeout
- **WHEN** the game runs for the first time on a device
- **THEN** the inactivity timeout is 60 seconds

#### Scenario: Change the inactivity timeout
- **WHEN** the player sets the inactivity timeout to 30 seconds
- **THEN** the settings screen shows 30 seconds and the next game returns to the title after 30 seconds without input

#### Scenario: Setting order and explanation
- **WHEN** the settings screen is shown
- **THEN** the language control is first, the board size mode control is second, the inactivity timeout is last, and a smaller line of text below the inactivity timeout explains that the game returns to the title after that long without input

#### Scenario: Wide sliders
- **WHEN** the settings screen is shown
- **THEN** the sound, music, and inactivity sliders are each at least half as wide as the settings panel

#### Scenario: Language options
- **WHEN** the player opens the language dropdown
- **THEN** it lists "English" and "Magyar", whatever the current language is

#### Scenario: Choose a language
- **WHEN** the player selects "Magyar" in the language dropdown
- **THEN** the dropdown shows "Magyar" and the game's text switches to Hungarian immediately

#### Scenario: Leave settings
- **WHEN** the player activates the settings screen's back action after opening settings from the title screen
- **THEN** the title screen is shown

## ADDED Requirements

### Requirement: Board size mode
The settings screen SHALL offer a board size mode with two options, "Free choice" and "Preset sizes", defaulting to free choice, and an EDIT SIZES action next to it. The game SHALL keep a list of preset board sizes (columns × rows) that starts empty and is kept between sessions. A preset size is usable when it is a valid board for the currently available tile designs. While preset sizes is selected but no preset size is usable, PLAY SHALL use the grid size picker as in free choice, and the settings screen SHALL say, in smaller text below the control, that players choose freely until sizes are added. Changing the mode during a paused game SHALL apply from the next board.

#### Scenario: Default mode
- **WHEN** the game runs for the first time on a device
- **THEN** the board size mode is free choice and the preset list is empty

#### Scenario: Preset mode without sizes
- **WHEN** preset sizes is selected and the preset list is empty
- **THEN** the settings screen shows a note that players choose freely until sizes are added, and PLAY opens the grid size picker

#### Scenario: Mode persists
- **WHEN** the board size mode is set to preset sizes with a 3 × 4 preset and the game is restarted
- **THEN** the mode is still preset sizes and the 3 × 4 preset is still in the list

#### Scenario: Change mode while paused
- **WHEN** the board size mode is changed in the settings screen opened from the pause menu
- **THEN** the current board continues unchanged and the next PLAY uses the new mode

### Requirement: Preset choice screen
In preset mode, the board-size screen SHALL show the heading "CHOOSE YOUR BOARD", one large button per usable preset size, and a BACK action. The buttons SHALL be ordered by pair count, then by number of columns. Each button SHALL show a small grid icon of the board's shape and the text "C × R · N pairs" in the current language. A single tap on a button SHALL start a game on a board of that size, and BACK SHALL return to the title screen without generating a board. Preset sizes that are not usable SHALL NOT be shown. When exactly one preset size is usable, PLAY SHALL start a game on that board straight away, without showing the preset choice screen.

#### Scenario: Single preset starts immediately
- **WHEN** preset mode is selected, the preset list holds only 3 × 4, and the player activates PLAY
- **THEN** a game starts on a 3-column by 4-row board without the preset choice screen being shown

#### Scenario: Show the presets
- **WHEN** the preset list holds 4 × 4, 2 × 2 and 3 × 4 and the player activates PLAY in preset mode
- **THEN** buttons for 2 × 2 · 2 pairs, 3 × 4 · 6 pairs and 4 × 4 · 8 pairs are shown in that order

#### Scenario: Start a preset board
- **WHEN** the player taps the 3 × 4 button
- **THEN** the menu closes and a game starts on a 3-column by 4-row board, and no tile is selected by that tap

#### Scenario: Leave the preset choice
- **WHEN** the player activates BACK on the preset choice screen
- **THEN** the title screen is shown and no board is generated

#### Scenario: Unusable preset skipped
- **WHEN** a preset needs more tile designs than are available
- **THEN** it is not shown on the preset choice screen

### Requirement: Preset size editor
The EDIT SIZES action SHALL be available only when the settings screen was opened from the title screen. It SHALL open the grid size picker in an edit mode with the heading "EDIT BOARD SIZES" and the current preset list beside the grid. In edit mode, releasing a valid selection SHALL add that size to the preset list instead of starting a game. A size already in the list SHALL NOT be added again, and the list SHALL hold at most 8 sizes. Invalid selections SHALL be rejected with the same messages as the picker. Each list entry SHALL show its size and pair count with a remove action. The list SHALL stay sorted as on the preset choice screen, and every change SHALL be saved immediately. Entries that are not usable SHALL be marked as unavailable but can still be removed. The editor's BACK action, and the platform back/escape action, SHALL return to the settings screen. The inactivity timeout SHALL NOT apply in the editor.

#### Scenario: Open the editor
- **WHEN** the player activates EDIT SIZES in the settings screen opened from the title
- **THEN** the grid is shown with the heading "EDIT BOARD SIZES" and the current preset list beside it

#### Scenario: Editor unavailable during a game
- **WHEN** the settings screen is opened from the pause menu
- **THEN** the EDIT SIZES action cannot be activated

#### Scenario: Add a size
- **WHEN** the player releases a valid 2 × 4 selection in the editor
- **THEN** 2 × 4 · 4 pairs appears in the list in sorted position, no game starts, and the editor stays open

#### Scenario: Add a duplicate
- **WHEN** the player releases a selection whose size is already in the list
- **THEN** the list is unchanged and the player is told the size is already in the list

#### Scenario: List full
- **WHEN** the list holds 8 sizes and the player releases a valid new selection
- **THEN** the list is unchanged and the player is told the list is full

#### Scenario: Invalid selection in the editor
- **WHEN** the player releases a 3 × 3 selection in the editor
- **THEN** nothing is added and the player is told the board needs an even number of tiles

#### Scenario: Remove a size
- **WHEN** the player activates the remove action of the 2 × 4 entry
- **THEN** 2 × 4 is removed from the list and the change is kept between sessions

#### Scenario: Return to settings
- **WHEN** the player activates the editor's BACK action or the platform back/escape action
- **THEN** the settings screen is shown again, opened from the title
