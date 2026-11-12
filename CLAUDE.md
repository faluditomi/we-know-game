@AGENTS.md

## Claude Code notes

- OpenSpec commands for Claude Code are installed under `.claude/commands/opsx/` and `.claude/skills/openspec-*`. In Claude Code they are invoked as `/opsx:<name>` (for example `/opsx:apply <change-name>`); `AGENTS.md` references like `/opsx-apply` mean the same workflow.
- Unity Editor-only steps in a change's `tasks.md` (Prefab Mode, Scene Editor, Input Actions Editor, Play Mode validation) cannot be performed from the CLI. Implement the source side, leave those tasks unchecked, and report what must be done in the Editor.
