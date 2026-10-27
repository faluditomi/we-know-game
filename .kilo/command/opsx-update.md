---
description: Revise a change's planning artifacts and keep them coherent
agent: game-orchestrator
---
Revise planning artifacts for an existing change; never modify implementation code.

1. Load the `openspec` skill and resolve the change name and requested revision from `$ARGUMENTS`.
2. Read `openspec status --change <name> --json` and all existing artifacts.
3. Present proposed artifact edits for user confirmation when product intent is not explicit.
4. Reconcile related artifacts, run `openspec validate <name> --json`, and recommend the next command.
