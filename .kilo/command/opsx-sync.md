---
description: Merge a change's delta specs into the main specs
agent: game-orchestrator
---
Synchronize delta specs from the named active change into main specs.

1. Load the `openspec` skill and resolve the change name from `$ARGUMENTS`.
2. Follow `openspec instructions` and CLI guidance for synchronization when available.
3. Apply deltas without duplicating requirements, run `openspec validate --json`, and report the result.

Prefer `/opsx-archive` when the change is complete because archive syncs specs automatically.
