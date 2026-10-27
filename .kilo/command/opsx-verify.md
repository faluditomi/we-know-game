---
description: Verify implementation matches the change's artifacts
agent: game-orchestrator
---
Verify an OpenSpec change without modifying files.

1. Load the `openspec` skill and resolve the change name from `$ARGUMENTS`.
2. Read all artifacts and the relevant implementation.
3. Report completeness, correctness, and coherence findings as CRITICAL, WARNING, or SUGGESTION, including whether it is ready to archive.
4. Run `openspec validate <name> --json` when useful.
