## Purpose

Lets the player show the game in English or Hungarian, defines which text follows the chosen language and which stays fixed, and guarantees Hungarian text renders correctly.

## Requirements

### Requirement: Supported display languages
The game SHALL offer English and Hungarian as display languages. English SHALL be the language on first launch, regardless of the device's language. The chosen language SHALL be kept between sessions.

#### Scenario: First launch
- **WHEN** the game runs for the first time on a device whose system language is Hungarian
- **THEN** the game is shown in English

#### Scenario: Language persists
- **WHEN** the player chooses Hungarian and restarts the game
- **THEN** the game starts in Hungarian

#### Scenario: Unknown stored language
- **WHEN** the saved language value is missing or not one of the supported languages
- **THEN** the game is shown in English

### Requirement: Player-facing text follows the language
Every player-facing text SHALL be shown in the chosen language, except the game title. This includes menu headings and buttons, settings labels, values and explanations, the board picker's prompt, selection size readout and rejection reasons, the pause menu, the match presentation's continue hint, and the asset-load failure message. Changing the language SHALL update all visible and not-yet-visible text immediately, without restarting the game or leaving the current screen.

#### Scenario: Switch to Hungarian on the settings screen
- **WHEN** the player selects Hungarian on the settings screen
- **THEN** the settings screen's heading, labels and back button are immediately shown in Hungarian while the settings screen stays open

#### Scenario: Other screens follow
- **WHEN** the player has chosen Hungarian and then opens the board picker
- **THEN** the picker's heading, prompt and back button are in Hungarian

#### Scenario: Picker messages
- **WHEN** the language is Hungarian and the player releases an odd-sized selection in the picker
- **THEN** the reason the selection was rejected is shown in Hungarian

#### Scenario: Inactivity value
- **WHEN** the language is changed while the settings screen shows the inactivity timeout
- **THEN** the timeout value is still shown in seconds, using that language's unit text

### Requirement: Title stays English
The game title and the success title SHALL be shown in English in every language, and configured title artwork SHALL be used unchanged.

#### Scenario: Title in Hungarian mode
- **WHEN** the language is Hungarian and the title screen is shown
- **THEN** the title reads "We Know... Do You?"

#### Scenario: Success title in Hungarian mode
- **WHEN** the language is Hungarian and the player completes a board
- **THEN** the success title reads "We Know... And Now You Do Too!"

### Requirement: Tile descriptions follow the language
Each tile SHALL be able to carry a description per supported language. A matched tile SHALL show the description for the chosen language, and SHALL show its English description when it has none for that language.

#### Scenario: Hungarian description present
- **WHEN** the language is Hungarian and the player matches a pair whose tile has a Hungarian description
- **THEN** the match presentation shows the Hungarian description

#### Scenario: Hungarian description missing
- **WHEN** the language is Hungarian and the matched tile has no Hungarian description
- **THEN** the match presentation shows the tile's English description

#### Scenario: Language changed mid-game
- **WHEN** the player changes the language from the settings screen opened from the pause menu and returns to the board
- **THEN** descriptions of already matched tiles and of pairs matched afterwards are shown in the newly chosen language

### Requirement: Missing translations fall back to English
Any player-facing text that has no translation in the chosen language SHALL be shown in English rather than blank or as an internal identifier.

#### Scenario: Untranslated label
- **WHEN** the language is Hungarian and a label has no Hungarian text
- **THEN** its English text is shown

### Requirement: Hungarian characters render
Text in the menu font SHALL display all Hungarian letters, including á, é, í, ó, ö, ő, ú, ü, ű and their capitals, on every supported platform, without missing-glyph boxes or substituted letters.

#### Scenario: Accented capitals
- **WHEN** the language is Hungarian and a label containing "Ő" or "Ű" is shown
- **THEN** the letters are drawn in the menu font with their double acute accents
