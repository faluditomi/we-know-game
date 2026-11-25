## Purpose

Defines the menu flow around a match: the title screen and its success variant, choosing a board size, pausing, settings, exiting, and the menu's live background and visual identity on landscape phones and tablets.

## Requirements

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

### Requirement: Replaceable title presentation
The title SHALL be readable as styled text when no title artwork is configured and SHALL display configured title artwork in its place when artwork is provided, without changing the menu flow.

#### Scenario: No artwork configured
- **WHEN** the title screen is shown and no title artwork is configured
- **THEN** the title is displayed as text in the menu style

#### Scenario: Artwork configured
- **WHEN** title artwork is configured for the normal or success title
- **THEN** that artwork is shown for that title state instead of the text

### Requirement: Success title after a completed game
After the player completes a board and the victory moment has finished, the game SHALL return to the title screen showing "We Know... And Now You Do Too!" for a configurable duration and SHALL then revert to "We Know... Do You?". The success title SHALL enter with a short pop-in and a clearly visible glow pulse that extends beyond the title artwork. While it shows, a clearly readable line on its own dark backing SHALL show the round's number of pairs, play time in minutes and seconds, and number of misses, in the chosen language, and that line SHALL disappear when the title reverts. The title screen SHALL stay usable while the success title shows.

#### Scenario: Return after completing a board
- **WHEN** the final pair of a board is confirmed, returned to the board, and the victory moment has finished
- **THEN** the game returns to the title screen and the title reads "We Know... And Now You Do Too!"

#### Scenario: Success title entrance
- **WHEN** the success title appears
- **THEN** it grows in from slightly smaller than its final size, overshoots a little, settles, and glows briefly

#### Scenario: Round statistics shown
- **WHEN** the success title shows after an 8-pair board finished in 92 seconds with 3 misses and the language is English
- **THEN** the line below the title reads "8 pairs · 1:32 · 3 misses"

#### Scenario: Round statistics in Hungarian
- **WHEN** the same round finishes and the language is Hungarian
- **THEN** the line below the title reads "8 pár · 1:32 · 3 tévesztés"

#### Scenario: Singular wording
- **WHEN** the language is English and the round had 1 miss
- **THEN** the line reads "1 miss" rather than "1 misses"

#### Scenario: Success title reverts
- **WHEN** the configured success-title duration has elapsed on the title screen
- **THEN** the title reverts to "We Know... Do You?" and the statistics line is hidden

#### Scenario: Configure the duration
- **WHEN** the success-title duration is changed in the game's configuration
- **THEN** the success title shows for the new duration on the next completed game

#### Scenario: Abandoned games do not celebrate
- **WHEN** the player leaves a board through MAIN MENU before completing it
- **THEN** the title screen shows "We Know... Do You?" with no statistics line

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
