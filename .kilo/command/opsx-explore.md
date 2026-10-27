---
description: Explore an idea before committing to a change (no artifacts created)
agent: game-orchestrator
---
Run an exploratory investigation. No artifacts and no code changes are made.

1. Load the `openspec` skill.
2. Investigate the user's topic (`$ARGUMENTS`), relevant code, existing specs, and active changes.
3. Compare realistic options with trade-offs and recommend one.
4. End by recommending `/opsx-propose <kebab-case-name>` when the user is ready.

Never create files or an OpenSpec change during this command.
