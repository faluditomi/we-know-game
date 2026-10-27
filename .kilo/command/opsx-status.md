---
description: Show OpenSpec changes and artifact status
agent: game-orchestrator
---
Show the current OpenSpec state.

1. Run `openspec list --json` and, when useful, `openspec list --specs --json`.
2. For a named change in `$ARGUMENTS`, run `openspec status --change <name> --json`.
3. Summarize active changes, artifact progress, blockers, and the recommended next command.
