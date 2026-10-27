---
description: Create the next planning artifact for an existing change
agent: game-orchestrator
---
Create the next ready planning artifact for the named change.

1. Load the `openspec` skill and resolve the change name from `$ARGUMENTS`.
2. Run `openspec status --change <name> --json`; select the first ready artifact.
3. Use `openspec instructions <artifact> --change <name> --json`, write only that artifact, then validate with `openspec validate <name> --json`.
4. Report the artifact created and the next ready action.

Do not modify implementation code or assets. Ask before making unresolvable product decisions.
