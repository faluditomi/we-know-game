## Purpose

Developer tooling for choosing sound-effect clips in context: candidate clips per sound cue, an in-game overlay to swap and preview them while playing (in the Editor and on a device), carrying the chosen clips back into the shipping sound setup, and an Editor analyzer that seeds shortlists. None of it reaches release builds.

## ADDED Requirements

### Requirement: Candidate clips per cue
Every sound cue SHALL be able to hold an ordered list of candidate clips for auditioning, kept separately from the clip the cue ships with. A cue's audition choices SHALL be its shipping clip (if any), then its candidates in list order, then silence. Clips appearing more than once SHALL be offered only once. A cue with no shipping clip and no candidates SHALL offer only silence. When the audition starts, a cue with a shipping clip SHALL start on that clip, and a cue without one SHALL start on its first candidate, so every cue with candidates is audible without stepping.

#### Scenario: Cue with a shipping clip and candidates
- **WHEN** a cue ships with clip A and has candidates B and C
- **THEN** its audition choices are A, B, C and silence, in that order, and A is active when play starts

#### Scenario: Cue without a shipping clip
- **WHEN** a cue has no shipping clip and has candidates B and C
- **THEN** its audition choices are B, C and silence, B is active when play starts, and the game plays B for that cue

#### Scenario: Cue with no clips
- **WHEN** a cue has no shipping clip and no candidates
- **THEN** its only audition choice is silence and the cue stays silent

#### Scenario: Duplicate candidate
- **WHEN** a cue's candidate list contains its shipping clip or the same clip twice
- **THEN** that clip appears once among the audition choices

### Requirement: Audition overlay availability
The audition overlay SHALL be available in Editor play mode and in development builds, including on a phone. It SHALL start hidden and SHALL be opened and closed by a deliberate gesture that ordinary play does not trigger: a keyboard shortcut in the Editor, and a small corner button on touch devices. While the overlay is open, taps on it SHALL NOT reach the board or menus underneath.

#### Scenario: Open on a phone
- **WHEN** the player taps the audition corner button in a development build on a phone
- **THEN** the audition overlay opens and taps on it do not flip tiles or press menu buttons

#### Scenario: Normal play with the overlay closed
- **WHEN** the overlay is closed
- **THEN** the board and menus respond to input exactly as without the audition tooling, apart from the corner button itself

#### Scenario: Open in the Editor
- **WHEN** the developer presses the audition toggle shortcut during Editor play mode
- **THEN** the audition overlay opens, and pressing it again closes it

### Requirement: Cycle and preview candidates
The overlay SHALL list every sound cue with the name of its active choice and its position among the choices (for example "3 / 7"). For any cue, the developer SHALL be able to step to the next or previous choice, wrapping at the ends, and SHALL be able to replay the active choice. Stepping SHALL immediately preview the newly active choice. From then on, every time the game plays that cue it SHALL play the active choice, using the cue's existing volume, pitch variation and the player's Sound volume.

#### Scenario: Step to the next candidate
- **WHEN** the developer steps TileFlip from choice 2 of 5 to the next
- **THEN** choice 3 plays immediately and every later tile flip plays choice 3

#### Scenario: Wrap around
- **WHEN** the developer steps past the last choice of a cue
- **THEN** the first choice becomes active

#### Scenario: Silence chosen
- **WHEN** the active choice of a cue is silence
- **THEN** the game plays nothing for that cue

### Requirement: Follow the last played cue
The overlay SHALL show the most recently played cue prominently, with next, previous and replay controls for it, so the sound just heard can be swapped without searching the full list. These controls SHALL also be available on touch devices while the full list is collapsed.

#### Scenario: Swap the sound just heard
- **WHEN** a mismatch sound has just played and the developer presses next on the last-played controls
- **THEN** the Mismatch cue's next choice becomes active and is previewed

### Requirement: Audition changes are temporary
Audition choices SHALL NOT change the shipping sound setup on their own. In the Editor, leaving play mode SHALL restore every cue to its shipping clip unless the picks were applied. In a development build, choices SHALL last for the current session only.

#### Scenario: Leave play mode without applying
- **WHEN** the developer changes choices and leaves Editor play mode without applying them
- **THEN** every cue still has its original shipping clip

### Requirement: Carry picks back into the shipping setup
The developer SHALL be able to turn the active choices into the cues' shipping clips. In the Editor, an apply action SHALL write the picks so they survive leaving play mode and can be undone. On a device, the overlay SHALL copy the picks as text (cue name and clip name per line) to the clipboard, and the Editor SHALL be able to import that text and apply it. Importing SHALL report any cue or clip name it does not recognise and SHALL leave those cues unchanged. Choosing silence SHALL apply as no clip.

#### Scenario: Apply in the Editor
- **WHEN** the developer applies picks during Editor play mode and then leaves play mode
- **THEN** each cue's shipping clip is its picked clip, and the change can be undone

#### Scenario: Picks from a phone
- **WHEN** the developer copies picks on a phone, pastes the text into the Editor import and applies it
- **THEN** each listed cue's shipping clip is set to the named clip

#### Scenario: Unknown clip in imported picks
- **WHEN** the imported text names a clip that does not exist in the project
- **THEN** the import reports that line and leaves that cue unchanged

### Requirement: Clip analyzer and shortlist seeding
The Editor SHALL provide a clip analyzer that lists every audio clip under the project's sounds folder with its file duration, audible length (ignoring a silent tail), peak level, RMS loudness over the audible part and a brightness measure, can be sorted by each column, and can preview any clip. It SHALL be able to export the measurements as a CSV file. It SHALL be able to seed each cue's candidate list from per-cue rules (clip name families and limits on duration and loudness). Each rule SHALL also name one proposed clip for its cue, and seeding SHALL make that clip the cue's first candidate, adding it if missing or moving it to the front if present. Seeding SHALL otherwise only add candidates, never remove existing ones or change shipping clips, and SHALL cap each cue's seeded candidates.

#### Scenario: Analyze the pack
- **WHEN** the developer opens the clip analyzer
- **THEN** every clip under the sounds folder is listed with duration, audible length, peak, RMS and brightness values

#### Scenario: One sound in several formats
- **WHEN** a sound pack holds the same sound as several same-named clips (for example mono and stereo, in .ogg, .wav and .mp3)
- **THEN** the analyzer, seeding and picks import use only one of them, preferring mono over stereo and .ogg over .wav over .mp3, and the picks import does not report that name as ambiguous

#### Scenario: Clip padded with silence
- **WHEN** a clip's file is 2 seconds long but its sound fades out after 0.3 seconds
- **THEN** the duration limits in seeding rules use its 0.3 second audible length

#### Scenario: Seed shortlists
- **WHEN** the developer seeds shortlists
- **THEN** each cue gains candidates that match its rules, up to the cap, its proposed clip is its first candidate, and no candidate is removed and no shipping clip changes

#### Scenario: Clip cannot be measured
- **WHEN** a clip's sample data cannot be read
- **THEN** the analyzer still lists it, marks its measurements as unavailable, and leaves it out of seeding

### Requirement: Excluded from release builds
Release (non-development) builds SHALL NOT contain the audition overlay, its corner button, or any clip that is referenced only as an audition candidate. Sound playback in release builds SHALL behave exactly as without the audition tooling.

#### Scenario: Release build
- **WHEN** a non-development player build is made
- **THEN** the game shows no audition controls and the build contains only the cues' shipping clips

#### Scenario: Development build
- **WHEN** a development player build is made
- **THEN** the audition corner button and overlay are available and every candidate clip can be played
