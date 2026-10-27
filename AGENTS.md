# Project Instructions

## Unity safety

- Do not edit Unity serialization or generated project files directly: `*.asset`, `*.prefab`, `*.unity`, `*.csproj`, `*.sln`, or `*.slnx`.
- Make gameplay and tooling changes through source files and Unity-supported editor workflows only.
- Preserve the project's existing asset-loading, singleton, and save-data patterns. Inspect the relevant code before extending them.

## OpenSpec workflow

- OpenSpec is the planning workflow for this repository. Its root is `openspec/`.
- Keep active-change artifacts in `openspec/changes/<change-name>/`; do not edit `openspec/specs/` manually.
- Use the OpenSpec CLI as the source of truth for change status, artifact instructions, and validation.
- Planning commands must not modify game code or assets. Use `/opsx-apply <change-name>` only after the plan is ready and implementation is explicitly authorized.

## Change authorization

- Do not modify project files merely because a request describes a feature, problem, or idea.
- A file-changing action requires either an explicit implementation/edit/create/delete/configure request from the user or an `/opsx-*` command whose workflow calls for that action.
- For requests to investigate, explain, review, plan, brainstorm, or diagnose, remain read-only and report the recommended next OpenSpec command.
- When implementation authority or the requested scope is ambiguous, provide the proposed scope and wait for explicit approval before changing files.
