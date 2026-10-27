---
description: Fast-forward — create all planning artifacts for a change at once
agent: game-orchestrator
---
Create every remaining planning artifact for the named change in dependency order.

1. Load the `openspec` skill and resolve the change name from `$ARGUMENTS`.
2. Repeatedly use `openspec status --change <name> --json` to find the first ready artifact, `openspec instructions <artifact> --change <name> --json` to create it, and `openspec validate <name> --json` to validate it.
3. Stop if user decisions are required or an artifact is blocked.
4. Confirm planning completion and recommend `/opsx-apply <name>`.

Never modify game code or assets.
