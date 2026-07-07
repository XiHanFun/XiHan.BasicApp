# XiHan.BasicApp Contribution Conventions Design

## Goal

Extract durable code contribution requirements from the XiHanFun contributing guide and place only the requirements that should affect future AI-assisted work in XiHan.BasicApp.

The update must avoid promoting optional local tools, editor choices, GUI clients, or tutorial-only setup steps into project rules.

Source reviewed:

- `https://docs.xihanfun.com/cosmos/contributing`

## Approved Approach

Use the existing AI guidance structure instead of creating a new constitution file.

```text
AGENTS.md
.ai/
  skills/
    xihan-basicapp-backend/
      SKILL.md
    xihan-basicapp-frontend/
      SKILL.md
```

`AGENTS.md` stays as the root router and adds a short cross-cutting contribution note. The backend and frontend skill files receive side-specific contribution conventions where future agents already load the relevant guidance before editing.

## Requirements To Keep

These requirements should be added because they are durable project conventions rather than personal tool choices:

- C# files added to the backend should follow the existing copyright and MIT license header style used throughout the repository.
- Backend formatting should follow the repository's checked-in `backend/.editorconfig` and existing backend formatting configuration.
- Frontend formatting should follow `frontend/.editorconfig`, `frontend/.prettierrc.mjs`, and the scripts declared in `frontend/package.json`.
- Commit messages should use the observed conventional prefixes: `feat`, `fix`, `refactor`, `perf`, `docs`, `chore`, `revert`, `style`, `test`, `build`, and `ci`.
- Before reporting contribution work as complete, agents should run the verification commands appropriate to the files they changed and keep local work synchronized with the current branch state.

## Requirements To Exclude

These items from the external guide should not become hard requirements in this repository:

- Installing Visual Studio 2022.
- Installing VS Code.
- Installing SourceTree.
- Installing the Visual Studio CodeMaid extension.
- Installing a VS Code Prettier extension.
- Editing Visual Studio item templates.
- Copying templates into Windows-specific Visual Studio directories.
- Treating fork, clone, push, and pull request walkthrough steps as AI implementation rules.

These are valid contributor conveniences or human onboarding instructions, but they are not necessary for AI agents or for all contributors.

## File-Level Design

### `AGENTS.md`

Add a short contribution note after the current skill-routing guidance:

- Use conventional commit prefixes when committing.
- Treat editor, IDE, and GUI Git tools mentioned in external docs as optional.
- Defer backend and frontend contribution details to the corresponding `.ai/skills` file.

This keeps the root file brief and avoids duplicating side-specific rules.

### `.ai/skills/xihan-basicapp-backend/SKILL.md`

Add a `Contribution Conventions` section near the existing backend verification guidance:

- Follow the existing C# copyright header style when adding C# files.
- Preserve the MIT license line in new C# headers.
- Use `backend/.editorconfig` and checked-in backend formatting config as the source of truth.
- Do not require Visual Studio 2022 or CodeMaid; they may be useful locally, but they are not project gates.
- Use the conventional commit prefixes listed in `AGENTS.md` when a backend-only change is committed.

### `.ai/skills/xihan-basicapp-frontend/SKILL.md`

Add a `Contribution Conventions` section near the existing frontend verification guidance:

- Use project scripts from `frontend/package.json` for linting, formatting, type checking, and builds.
- Use `frontend/.editorconfig` and `frontend/.prettierrc.mjs` as formatting sources of truth.
- Do not require VS Code or editor plugins; they may be useful locally, but they are not project gates.
- Use the conventional commit prefixes listed in `AGENTS.md` when a frontend-only change is committed.

## Error Handling And Safety

- The change is documentation-only and must not alter runtime backend or frontend behavior.
- Do not stage unrelated local files such as `.DS_Store`.
- Do not create a new `.ai/constitution.md` unless the repository later adopts a broader governance file.
- Do not create a human-facing `CONTRIBUTING.md` in this pass; the approved scope is AI guidance for future coding agents.

## Verification

After implementation, verify:

- `AGENTS.md` still points to both local skills.
- The backend skill includes the contribution section and still preserves XiHan.BasicApp backend boundaries.
- The frontend skill includes the contribution section and still preserves XiHan.BasicApp frontend boundaries.
- No required rule mentions Visual Studio 2022, VS Code, SourceTree, CodeMaid, editor extensions, or Windows template paths as mandatory.
- `git diff --check` passes for the changed Markdown files.

## Acceptance Criteria

- `AGENTS.md` contains a concise contribution note without becoming a long contribution guide.
- `.ai/skills/xihan-basicapp-backend/SKILL.md` documents backend contribution conventions and optionalizes IDE-specific tooling.
- `.ai/skills/xihan-basicapp-frontend/SKILL.md` documents frontend contribution conventions and optionalizes IDE-specific tooling.
- The implementation includes only documentation changes.
- Existing untracked `.DS_Store` files remain unstaged.
