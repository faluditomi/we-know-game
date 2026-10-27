---
description: Archive a completed OpenSpec change (merges delta specs, moves to archive)
agent: game-orchestrator
---
Archive a completed change.

1. Load the `openspec` skill and resolve the change name from `$ARGUMENTS`.
2. Run `openspec validate <name> --json` and inspect unchecked tasks; report blockers before archiving.
3. Run `openspec archive <name> --yes` and confirm the archive path.

The command syncs delta specs automatically; do not manually edit `openspec/specs/`.
