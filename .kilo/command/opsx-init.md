---
description: Initialize OpenSpec (spec-driven development) in the current project
agent: game-orchestrator
---
Set up OpenSpec in the current project:

1. Verify the CLI: run `openspec --version`. If it is not installed, install it globally with `npm install -g @fission-ai/openspec@latest`, then re-check.
2. If `openspec/` does not exist at the repo root, initialize it with `openspec init --tools none --no-animation --force`.
3. Confirm `openspec/specs/`, `openspec/changes/`, and `openspec/config.yaml` exist.
4. Do not create a change or spec artifact. Recommend `/opsx-explore` for a fuzzy idea or `/opsx-propose <name>` for a clear change.
