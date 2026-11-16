## MODIFIED Requirements

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
