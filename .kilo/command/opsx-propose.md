---
description: Create a change and generate all planning artifacts (proposal, specs, design, tasks)
agent: game-orchestrator
---
Create an OpenSpec change and all planning artifacts, stopping before implementation.

1. Load the `openspec` skill. Resolve a valid kebab-case name from `$ARGUMENTS`; ask only if intent is ambiguous.
2. Run `openspec new change <name> --description "<one-line description>" --json`.
3. In dependency order, use `openspec status --change <name> --json` and `openspec instructions <artifact> --change <name> --json` to write each ready artifact.
4. Run `openspec validate <name> --json` after each artifact and resolve warnings.
5. Confirm planning is complete, summarize created paths, and recommend `/opsx-apply <name>`.

Never write game code or assets during this command. Never edit `openspec/specs/` directly.
