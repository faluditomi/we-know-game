## Purpose

Defines the short victory moment that plays on a completed board before the game returns to the title, and the limits that keep it cheap on phones and tablets.

## Requirements

### Requirement: Victory moment on a completed board
When the final pair of a board is confirmed and back on the board, the game SHALL play a victory moment on the completed board lasting between 1.5 and 2.5 seconds, and SHALL then return to the title. The moment SHALL play exactly once per completed board and SHALL NOT play for boards left through MAIN MENU or the inactivity timeout.

#### Scenario: Win a board
- **WHEN** the final pair of a board is confirmed and returned to the board
- **THEN** the victory moment plays on the completed board and the title screen is shown once it ends

#### Scenario: Abandon a board
- **WHEN** the player leaves a board through MAIN MENU or the inactivity timeout
- **THEN** no victory moment plays

#### Scenario: Board input during the moment
- **WHEN** the player taps a tile while the victory moment plays
- **THEN** nothing is selected and the moment continues

### Requirement: Board wave
During the victory moment every tile on the board SHALL briefly hop towards the camera and spin once, starting at the last matched pair and rippling outward so tiles further from it start later. Every tile SHALL end in its matched board pose, logo side up.

#### Scenario: Ripple order
- **WHEN** the victory moment starts
- **THEN** the tiles of the last matched pair move first and each other tile starts later the further it is from them

#### Scenario: Tiles settle
- **WHEN** the board wave ends
- **THEN** every tile is back in its board position, at its board size, showing its logo

### Requirement: Screen surge
During the victory moment the screens of all tiles SHALL flash noticeably brighter with stronger glitch flashes, then return to their normal look before the moment ends.

#### Scenario: Surge and settle
- **WHEN** the victory moment starts
- **THEN** every tile screen brightens and flickers more, and by the end of the moment it looks the same as during normal play

### Requirement: Confetti burst
At the start of the victory moment the game SHALL fire confetti in We-Know teal and white from both lower sides of the screen, upward and inward like a pair of confetti cannons, in front of the board. The confetti SHALL be gone before the title screen is shown.

#### Scenario: Cannons
- **WHEN** the victory moment starts
- **THEN** confetti shoots in from the left and right edges of the screen, arcs over the board, falls, and has disappeared by the time the title screen is shown

### Requirement: Camera punch
At the start of the victory moment the view SHALL zoom in slightly and settle back to the board framing within the moment, so the visible part of the board at the end is the same as before.

#### Scenario: Punch and settle
- **WHEN** the victory moment starts
- **THEN** the board briefly appears slightly larger and then returns to exactly its normal framing

### Requirement: Win vibration
On devices that can vibrate, the game SHALL vibrate once, briefly, when the victory moment starts, and only while the Vibration setting is on.

#### Scenario: Vibration on
- **WHEN** a board is won on a phone with Vibration on
- **THEN** the device vibrates once

#### Scenario: Vibration off
- **WHEN** a board is won with Vibration off
- **THEN** the device does not vibrate

### Requirement: Pausing during the moment
The pause control SHALL stay available during the victory moment. While paused, every part of the moment SHALL hold and SHALL continue from the same point after CONTINUE. MAIN MENU from that pause SHALL leave without the success title.

#### Scenario: Pause and continue
- **WHEN** the player pauses during the victory moment and then activates CONTINUE
- **THEN** the moment continues from where it stopped and then the success title is shown

#### Scenario: Leave from pause
- **WHEN** the player pauses during the victory moment and activates MAIN MENU
- **THEN** the title screen shows "We Know... Do You?"

### Requirement: Performance budget
The victory moment SHALL NOT add post-processing, bloom, or additional full-screen render passes. It SHALL use at most one particle system with at most 400 live particles, unlit and without collisions, and SHALL NOT create or destroy objects while it plays.

#### Scenario: Effect cost
- **WHEN** the victory moment plays on a supported phone
- **THEN** no post-processing is enabled, at most one particle system emits, and no objects are instantiated or destroyed during the moment
