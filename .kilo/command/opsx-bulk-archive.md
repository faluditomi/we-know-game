---
description: Archive multiple completed OpenSpec changes at once
agent: game-orchestrator
---
Archive multiple completed changes safely.

1. Load the `openspec` skill and list changes with `openspec list --json`.
2. Validate each selected change, detect overlapping delta specs, and present the planned archive order.
3. Obtain explicit user confirmation for the batch.
4. Run `openspec archive <name> --yes` in the selected order and report each outcome.
