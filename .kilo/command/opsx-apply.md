---
description: Implement tasks from the change's tasks.md, checking them off as you go
agent: game-orchestrator
---
Implement an explicitly authorized OpenSpec change.

1. Load the `openspec` skill and resolve the change name from `$ARGUMENTS`.
2. Run `openspec instructions apply --change <name> --json`, then read its unchecked `tasks.md` items and planning artifacts.
3. Implement tasks in order, run appropriate checks, and tick each task only after it is genuinely complete.
4. Run `openspec validate <name> --json`, summarize implementation and validation, then recommend `/opsx-verify <name>`.

Respect AGENTS.md, including all Unity serialization and generated-file restrictions.
