---
description: Scaffold a new OpenSpec change folder (artifacts come later)
agent: game-orchestrator
---
Start a new change scaffold only.

1. Resolve a kebab-case change name from `$ARGUMENTS`.
2. Run `openspec new change <name> --description "<one-line description>" --json`.
3. Show `openspec status --change <name> --json` and recommend `/opsx-continue`, `/opsx-ff`, or `/opsx-propose <name>`.
