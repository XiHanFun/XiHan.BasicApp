# XiHan.BasicApp Contribution Conventions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add durable contribution conventions from the XiHanFun contributing guide to the existing XiHan.BasicApp AI guidance files without making optional tools mandatory.

**Architecture:** Keep `AGENTS.md` as the root router and add only a short cross-cutting contribution note there. Put backend-specific and frontend-specific contribution details in their existing local skill files so agents load the rules in the same place they already load implementation guidance.

**Tech Stack:** Markdown, Codex-style local AI skills, git.

## Global Constraints

- The implementation is documentation-only and must not alter runtime backend or frontend behavior.
- Use the existing AI guidance structure: `AGENTS.md`, `.ai/skills/xihan-basicapp-backend/SKILL.md`, and `.ai/skills/xihan-basicapp-frontend/SKILL.md`.
- Do not create `.ai/constitution.md`.
- Do not create `CONTRIBUTING.md` in this pass.
- Treat Visual Studio 2022, VS Code, SourceTree, CodeMaid, editor extensions, and Windows Visual Studio template paths as optional local tooling, not project gates.
- Commit message prefixes should include `feat`, `fix`, `refactor`, `perf`, `docs`, `chore`, `revert`, `style`, `test`, `build`, and `ci`.
- Do not stage unrelated local files such as `.DS_Store`.

---

## File Structure

- Modify `AGENTS.md`: keep the root file brief, preserve the backend/frontend skill routing, and add one contribution section with conventional commit prefixes plus the optional-tool boundary.
- Modify `.ai/skills/xihan-basicapp-backend/SKILL.md`: add backend contribution conventions near verification, with C# header, MIT license header line, and backend formatting-source rules.
- Modify `.ai/skills/xihan-basicapp-frontend/SKILL.md`: add frontend contribution conventions near verification, with package scripts and frontend formatting-source rules.

---

### Task 1: Root Contribution Note

**Files:**
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: Existing links to `.ai/skills/xihan-basicapp-backend/SKILL.md` and `.ai/skills/xihan-basicapp-frontend/SKILL.md`.
- Produces: A root `Contribution Notes` section that later skill sections can reference for commit prefixes.

- [ ] **Step 1: Verify current root guidance**

Run:

```bash
sed -n '1,120p' AGENTS.md
```

Expected output includes:

```text
# XiHan.BasicApp AI Agent Guide
Before making backend changes, read:
.ai/skills/xihan-basicapp-backend/SKILL.md
Before making frontend changes, read:
.ai/skills/xihan-basicapp-frontend/SKILL.md
```

- [ ] **Step 2: Add concise contribution notes**

Apply this patch:

```patch
*** Begin Patch
*** Update File: AGENTS.md
@@
 For work that touches both sides, read both skills and preserve the backend API contracts, permission codes, route/component paths, and DTO shapes used by the current frontend and backend.
 
+## Contribution Notes
+
+Use the existing repository conventions as the source of truth. Commit messages should use the conventional prefixes `feat`, `fix`, `refactor`, `perf`, `docs`, `chore`, `revert`, `style`, `test`, `build`, or `ci`.
+
+External contribution docs may mention IDEs, editor extensions, GUI Git tools, or local templates. Treat those as optional contributor conveniences, not project requirements. Backend and frontend contribution details live in the corresponding `.ai/skills` file above.
+
 These skills are guidance for contributors and AI agents. They must not be treated as permission to rewrite the project architecture, replace framework choices, or scaffold a different application inside this repository.
*** End Patch
```

- [ ] **Step 3: Verify root contribution note**

Run:

```bash
rg -n "Contribution Notes|feat.*fix.*refactor|optional contributor conveniences|xihan-basicapp-(backend|frontend)/SKILL.md" AGENTS.md
```

Expected output includes:

```text
7:- `.ai/skills/xihan-basicapp-backend/SKILL.md`
11:- `.ai/skills/xihan-basicapp-frontend/SKILL.md`
15:## Contribution Notes
17:Use the existing repository conventions as the source of truth. Commit messages should use the conventional prefixes `feat`, `fix`, `refactor`, `perf`, `docs`, `chore`, `revert`, `style`, `test`, `build`, or `ci`.
19:External contribution docs may mention IDEs, editor extensions, GUI Git tools, or local templates. Treat those as optional contributor conveniences, not project requirements. Backend and frontend contribution details live in the corresponding `.ai/skills` file above.
```

- [ ] **Step 4: Check whitespace for root guidance**

Run:

```bash
git diff --check -- AGENTS.md
```

Expected: no output and exit code `0`.

- [ ] **Step 5: Commit root guidance**

Run:

```bash
git add AGENTS.md
git commit -m "docs: add root contribution notes"
```

Expected: commit succeeds with only `AGENTS.md` staged.

---

### Task 2: Backend Contribution Conventions

**Files:**
- Modify: `.ai/skills/xihan-basicapp-backend/SKILL.md`

**Interfaces:**
- Consumes: The `Contribution Notes` section in `AGENTS.md`.
- Produces: A backend `Contribution Conventions` section that clarifies C# headers, backend formatting sources, and optional backend tooling.

- [ ] **Step 1: Verify backend skill still has the existing C# header guardrail**

Run:

```bash
rg -n "Follow the existing copyright header style|## Verification" .ai/skills/xihan-basicapp-backend/SKILL.md
```

Expected output includes:

```text
62:- Follow the existing copyright header style when adding C# files.
77:## Verification
```

- [ ] **Step 2: Add backend contribution conventions**

Apply this patch:

```patch
*** Begin Patch
*** Update File: .ai/skills/xihan-basicapp-backend/SKILL.md
@@
 - Keep business rules in domain services or domain models, not in controllers, endpoint lambdas, or frontend code.
 - Keep DTOs, contracts, mappers, app services, query services, domain services, repositories, and seeders in focused files.
 - Follow the existing copyright header style when adding C# files.
 
+## Contribution Conventions
+
+When adding or changing backend code:
+
+- Follow the repository's existing C# copyright header style on new C# files, including the MIT license line that points to `LICENSE` in the project root.
+- Use `backend/.editorconfig` and checked-in backend formatting configuration as the formatting source of truth.
+- Visual Studio 2022 and CodeMaid can be useful local tools, but they are not required project gates.
+- When committing backend-only documentation or code changes, use one of the conventional prefixes listed in `AGENTS.md`.
+
 ## Backend Change Workflow
*** End Patch
```

- [ ] **Step 3: Verify backend contribution conventions**

Run:

```bash
rg -n "## Contribution Conventions|MIT license line|backend/.editorconfig|not required project gates|AGENTS.md" .ai/skills/xihan-basicapp-backend/SKILL.md
```

Expected output includes:

```text
64:## Contribution Conventions
68:- Follow the repository's existing C# copyright header style on new C# files, including the MIT license line that points to `LICENSE` in the project root.
69:- Use `backend/.editorconfig` and checked-in backend formatting configuration as the formatting source of truth.
70:- Visual Studio 2022 and CodeMaid can be useful local tools, but they are not required project gates.
71:- When committing backend-only documentation or code changes, use one of the conventional prefixes listed in `AGENTS.md`.
```

- [ ] **Step 4: Check whitespace for backend skill**

Run:

```bash
git diff --check -- .ai/skills/xihan-basicapp-backend/SKILL.md
```

Expected: no output and exit code `0`.

- [ ] **Step 5: Commit backend conventions**

Run:

```bash
git add .ai/skills/xihan-basicapp-backend/SKILL.md
git commit -m "docs: add backend contribution conventions"
```

Expected: commit succeeds with only `.ai/skills/xihan-basicapp-backend/SKILL.md` staged.

---

### Task 3: Frontend Contribution Conventions

**Files:**
- Modify: `.ai/skills/xihan-basicapp-frontend/SKILL.md`

**Interfaces:**
- Consumes: The `Contribution Notes` section in `AGENTS.md`.
- Produces: A frontend `Contribution Conventions` section that clarifies package scripts, frontend formatting sources, and optional frontend tooling.

- [ ] **Step 1: Verify frontend skill has verification guidance**

Run:

```bash
rg -n "Common commands from `frontend/`|pnpm type-check|pnpm build|pnpm lint" .ai/skills/xihan-basicapp-frontend/SKILL.md
```

Expected output includes:

```text
89:Common commands from `frontend/`:
92:pnpm type-check
93:pnpm build
94:pnpm lint
```

- [ ] **Step 2: Add frontend contribution conventions**

Apply this patch:

```patch
*** Begin Patch
*** Update File: .ai/skills/xihan-basicapp-frontend/SKILL.md
@@
 7. Add permission-aware UI actions using existing hooks/stores and the backend permission codes.
 8. Verify type safety and build output before reporting success.
 
+## Contribution Conventions
+
+When adding or changing frontend code:
+
+- Use the scripts in `frontend/package.json` for linting, formatting, type checking, and builds.
+- Use `frontend/.editorconfig` and `frontend/.prettierrc.mjs` as the formatting sources of truth.
+- VS Code and editor plugins can be useful local tools, but they are not required project gates.
+- When committing frontend-only documentation or code changes, use one of the conventional prefixes listed in `AGENTS.md`.
+
 ## Verification
*** End Patch
```

- [ ] **Step 3: Verify frontend contribution conventions**

Run:

```bash
rg -n "## Contribution Conventions|frontend/package.json|frontend/.editorconfig|frontend/.prettierrc.mjs|not required project gates|AGENTS.md" .ai/skills/xihan-basicapp-frontend/SKILL.md
```

Expected output includes:

```text
85:## Contribution Conventions
89:- Use the scripts in `frontend/package.json` for linting, formatting, type checking, and builds.
90:- Use `frontend/.editorconfig` and `frontend/.prettierrc.mjs` as the formatting sources of truth.
91:- VS Code and editor plugins can be useful local tools, but they are not required project gates.
92:- When committing frontend-only documentation or code changes, use one of the conventional prefixes listed in `AGENTS.md`.
```

- [ ] **Step 4: Check whitespace for frontend skill**

Run:

```bash
git diff --check -- .ai/skills/xihan-basicapp-frontend/SKILL.md
```

Expected: no output and exit code `0`.

- [ ] **Step 5: Commit frontend conventions**

Run:

```bash
git add .ai/skills/xihan-basicapp-frontend/SKILL.md
git commit -m "docs: add frontend contribution conventions"
```

Expected: commit succeeds with only `.ai/skills/xihan-basicapp-frontend/SKILL.md` staged.

---

### Task 4: Final Verification

**Files:**
- Verify: `AGENTS.md`
- Verify: `.ai/skills/xihan-basicapp-backend/SKILL.md`
- Verify: `.ai/skills/xihan-basicapp-frontend/SKILL.md`

**Interfaces:**
- Consumes: All documentation changes from Tasks 1 through 3.
- Produces: Evidence that the contribution conventions were added without making optional local tools mandatory.

- [ ] **Step 1: Verify all contribution sections exist**

Run:

```bash
rg -n "Contribution Notes|Contribution Conventions|conventional prefixes|formatting source|formatting sources" AGENTS.md .ai/skills/xihan-basicapp-backend/SKILL.md .ai/skills/xihan-basicapp-frontend/SKILL.md
```

Expected output includes:

```text
AGENTS.md:15:## Contribution Notes
AGENTS.md:17:Use the existing repository conventions as the source of truth. Commit messages should use the conventional prefixes `feat`, `fix`, `refactor`, `perf`, `docs`, `chore`, `revert`, `style`, `test`, `build`, or `ci`.
.ai/skills/xihan-basicapp-backend/SKILL.md:64:## Contribution Conventions
.ai/skills/xihan-basicapp-backend/SKILL.md:69:- Use `backend/.editorconfig` and checked-in backend formatting configuration as the formatting source of truth.
.ai/skills/xihan-basicapp-frontend/SKILL.md:85:## Contribution Conventions
.ai/skills/xihan-basicapp-frontend/SKILL.md:90:- Use `frontend/.editorconfig` and `frontend/.prettierrc.mjs` as the formatting sources of truth.
```

- [ ] **Step 2: Verify optional tools are not mandatory**

Run:

```bash
rg -n "Visual Studio 2022|VS Code|SourceTree|CodeMaid|editor plugins|template" AGENTS.md .ai/skills/xihan-basicapp-backend/SKILL.md .ai/skills/xihan-basicapp-frontend/SKILL.md
```

Expected output:

```text
AGENTS.md:19:External contribution docs may mention IDEs, editor extensions, GUI Git tools, or local templates. Treat those as optional contributor conveniences, not project requirements. Backend and frontend contribution details live in the corresponding `.ai/skills` file above.
.ai/skills/xihan-basicapp-backend/SKILL.md:70:- Visual Studio 2022 and CodeMaid can be useful local tools, but they are not required project gates.
.ai/skills/xihan-basicapp-frontend/SKILL.md:91:- VS Code and editor plugins can be useful local tools, but they are not required project gates.
```

- [ ] **Step 3: Verify Markdown whitespace**

Run:

```bash
git diff --check -- AGENTS.md .ai/skills/xihan-basicapp-backend/SKILL.md .ai/skills/xihan-basicapp-frontend/SKILL.md
```

Expected: no output and exit code `0`.

- [ ] **Step 4: Verify unrelated files remain unstaged**

Run:

```bash
git status --short
```

Expected: only pre-existing untracked `.DS_Store` files remain, and no staged files remain after the task commits.

- [ ] **Step 5: Show recent commits**

Run:

```bash
git log --oneline -5
```

Expected output includes:

```text
docs: add frontend contribution conventions
docs: add backend contribution conventions
docs: add root contribution notes
docs: add contribution conventions design
```
