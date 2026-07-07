# XiHan.BasicApp AI Skills Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a root AI guidance entrypoint and two project-local skills that encode XiHan.BasicApp backend and frontend development conventions.

**Architecture:** `AGENTS.md` is the short root router that most AI agents can discover. The detailed backend and frontend guidance lives in separate `.ai/skills/*/SKILL.md` files so future agents can load only the relevant side.

**Tech Stack:** Markdown, Codex-style skill frontmatter, XiHan.BasicApp backend (`.NET 10`, `XiHan.Framework`, Dynamic API, SqlSugar), XiHan.BasicApp frontend (Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4).

## Global Constraints

- Create guidance documents only; do not modify runtime backend or frontend code.
- Create the root entrypoint at `AGENTS.md`.
- Create backend guidance at `.ai/skills/xihan-basicapp-backend/SKILL.md`.
- Create frontend guidance at `.ai/skills/xihan-basicapp-frontend/SKILL.md`.
- The backend skill must preserve `XiHan.Framework`, Dynamic API application services, SqlSugar repositories, and the existing module layout; it must reject `dotnet-business-backend` scaffolding, Minimal API rewrites, and EF Core replacement for ordinary backend work.
- The frontend skill must preserve Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4, `frontend/src`, and `frontend/packages`; it must reject Ant Design Vue conversion and generic `app/pages/shared` scaffolding.
- Do not stage existing untracked local files such as `.DS_Store`.

---

## File Structure

- Create `AGENTS.md`: universal root guidance for AI agents, with routing to the two project-local skills and a short safety note.
- Create `.ai/skills/xihan-basicapp-backend/SKILL.md`: backend-specific workflow, architecture map, non-negotiable boundaries, implementation checklist, and verification commands.
- Create `.ai/skills/xihan-basicapp-frontend/SKILL.md`: frontend-specific workflow, directory ownership, API/route/state/UI rules, and verification commands.

---

### Task 1: Root AI Entrypoint

**Files:**
- Create: `AGENTS.md`

**Interfaces:**
- Consumes: Approved design in `docs/superpowers/specs/2026-07-07-xihan-basicapp-ai-skills-design.md`.
- Produces: Root instructions that direct future AI agents to `.ai/skills/xihan-basicapp-backend/SKILL.md` and `.ai/skills/xihan-basicapp-frontend/SKILL.md`.

- [ ] **Step 1: Verify the root entrypoint is not already present**

Run:

```bash
test ! -f AGENTS.md && echo "AGENTS.md missing as expected"
```

Expected:

```text
AGENTS.md missing as expected
```

- [ ] **Step 2: Create the root entrypoint**

Apply this patch:

```patch
*** Begin Patch
*** Add File: AGENTS.md
+# XiHan.BasicApp AI Agent Guide
+
+This repository keeps project-local AI guidance under `.ai/skills/`.
+
+Before making backend changes, read:
+
+- `.ai/skills/xihan-basicapp-backend/SKILL.md`
+
+Before making frontend changes, read:
+
+- `.ai/skills/xihan-basicapp-frontend/SKILL.md`
+
+For work that touches both sides, read both skills and preserve the backend API contracts, permission codes, route/component paths, and DTO shapes used by the current frontend and backend.
+
+These skills are guidance for contributors and AI agents. They must not be treated as permission to rewrite the project architecture, replace framework choices, or scaffold a different application inside this repository.
*** End Patch
```

- [ ] **Step 3: Verify the root entrypoint routes to both skills**

Run:

```bash
rg -n "xihan-basicapp-(backend|frontend)/SKILL.md|rewrite the project architecture" AGENTS.md
```

Expected:

```text
7:- `.ai/skills/xihan-basicapp-backend/SKILL.md`
11:- `.ai/skills/xihan-basicapp-frontend/SKILL.md`
15:These skills are guidance for contributors and AI agents. They must not be treated as permission to rewrite the project architecture, replace framework choices, or scaffold a different application inside this repository.
```

- [ ] **Step 4: Check whitespace**

Run:

```bash
git diff --check -- AGENTS.md
```

Expected: no output and exit code `0`.

- [ ] **Step 5: Commit the root entrypoint**

Run:

```bash
git add AGENTS.md
git commit -m "docs: add ai agent guide"
```

Expected: commit succeeds with only `AGENTS.md` staged.

---

### Task 2: Backend Skill

**Files:**
- Create: `.ai/skills/xihan-basicapp-backend/SKILL.md`

**Interfaces:**
- Consumes: `AGENTS.md` link from Task 1.
- Produces: A valid skill named `xihan-basicapp-backend` that future agents can read before backend work.

- [ ] **Step 1: Verify the backend skill is not already present**

Run:

```bash
test ! -f .ai/skills/xihan-basicapp-backend/SKILL.md && echo "backend skill missing as expected"
```

Expected:

```text
backend skill missing as expected
```

- [ ] **Step 2: Create the backend skill**

Run:

```bash
mkdir -p .ai/skills/xihan-basicapp-backend
```

Expected: no output and exit code `0`.

Apply this patch:

```patch
*** Begin Patch
*** Add File: .ai/skills/xihan-basicapp-backend/SKILL.md
+---
+name: xihan-basicapp-backend
+description: Use when modifying, reviewing, documenting, or debugging the XiHan.BasicApp backend under backend/, including modules, Dynamic API application services, domain services, repositories, permissions, multi-tenancy, SqlSugar entities, seeders, health checks, SignalR, or Scalar contracts. Preserve XiHan.Framework, .NET 10, DynamicApi app-service boundaries, SqlSugar repositories, and the existing DDD/CQRS module layout; do not apply dotnet-business-backend scaffolding, Minimal API endpoint rewrites, or EF Core replacement unless the user explicitly asks for a separate migration.
+---
+
+# XiHan.BasicApp Backend Development
+
+## Start Here
+
+Before editing backend code:
+
+1. Read `README.md` and the nearest module README, such as `backend/src/modules/XiHan.BasicApp.Saas/README.md` or `backend/src/modules/XiHan.BasicApp.CodeGeneration/README.md`.
+2. Inspect neighboring files for the same feature type: contracts, DTOs, mappers, app services, query services, domain services, repositories, permissions, seeders, and module registration.
+3. Treat `dotnet-business-backend` as inspiration for small files, explicit boundaries, and verification habits only. Do not import its solution layout, template, Minimal API pattern, EF Core persistence model, Result/UseCase pipeline, or scaffold commands into this repository.
+
+## Project Shape
+
+The backend is a `.NET 10` XiHan.Framework application:
+
+```text
+backend/
+  src/
+    framework/
+      XiHan.BasicApp.Core/
+      XiHan.BasicApp.Web.Core/
+    modules/
+      XiHan.BasicApp.Saas/
+      XiHan.BasicApp.CodeGeneration/
+      XiHan.BasicApp.AI/
+    main/
+      XiHan.BasicApp.WebHost/
+  test/
+```
+
+Use the existing module boundary before creating new files:
+
+- `Domain/Entities`: SqlSugar entities, enums, value objects, specifications, permission constants, repository contracts, and domain services.
+- `Application/Contracts`: app-service and query-service interfaces exposed through Dynamic API plus request/response contracts when the module uses contract files.
+- `Application/Dtos`: DTO shapes consumed by Dynamic API and frontend API modules.
+- `Application/Mappers`: explicit application mapping helpers.
+- `Application/AppServices`: command-oriented app services with `[DynamicApi]`, permission attributes, and unit-of-work attributes.
+- `Application/QueryServices`: read-oriented query services.
+- `Infrastructure/Repositories`: SqlSugar repository implementations.
+- `Infrastructure/Seeders`: idempotent seed data for permissions, menus, resources, roles, operations, and defaults.
+- `Extensions/ServiceCollectionExtensions.cs`: module-owned service registration.
+- `XiHanBasicApp*Module.cs`: XiHan module dependency and initialization wiring.
+
+## Non-Negotiable Boundaries
+
+- Keep `Program.cs` small and module-driven through `builder.AddApplicationAsync<XiHanBasicAppWebHostModule>()`.
+- Expose ordinary HTTP APIs through `[DynamicApi]` application services and `IApplicationService` contracts. Do not replace them with Minimal API endpoint groups for normal feature work.
+- Use `[PermissionAuthorize(...)]` on protected operations and keep permission codes in module-owned permission constant files.
+- Use `[UnitOfWork(true)]` on write operations when neighboring command services do so.
+- Keep cancellation explicit: accept `CancellationToken cancellationToken = default`, call `cancellationToken.ThrowIfCancellationRequested()` in app/domain/repository methods that follow the local pattern, and pass the token to async calls.
+- Use `BasicAppEntity`, `BasicAppFullAuditedEntity`, `BasicAppAggregateRoot`, and related base classes from `XiHan.BasicApp.Core.Entities`.
+- Use SqlSugar attributes such as `[SugarTable]`, `[SugarColumn]`, and `[SugarIndex]` for persisted entities. Do not introduce EF Core configuration files for this repository's normal backend.
+- Preserve tenant semantics from `BasicAppEntity`: platform/global records use `TenantId = 0`; tenant records use the active tenant context. Do not use nullable tenant ids to represent global data.
+- For soft-deleted entities, include `IsDeleted` in unique indexes the same way nearby `BasicAppFullAuditedEntity` models do.
+- Keep repositories behind domain contracts and reuse `SaasRepository<T>` with `ISqlSugarClientResolver` when the module depends on Saas repository infrastructure.
+- Keep business rules in domain services or domain models, not in controllers, endpoint lambdas, or frontend code.
+- Keep DTOs, contracts, mappers, app services, query services, domain services, repositories, and seeders in focused files.
+- Follow the existing copyright header style when adding C# files.
+
+## Backend Change Workflow
+
+For a new backend capability:
+
+1. Locate the owning module. Use `Saas` for platform identity, RBAC, ABAC, tenancy, configuration, logging, workflow, files, messaging, and monitoring; use `CodeGeneration` for generator metadata/runtime; use `AI` for prompt/provider/knowledge capabilities.
+2. Add or extend domain entities, enums, permission constants, repository contracts, and domain services first.
+3. Add DTOs and mapper methods that keep frontend-facing shapes stable.
+4. Add command methods in `Application/AppServices` and read methods in `Application/QueryServices`.
+5. Add repository methods under `Infrastructure/Repositories` when data access cannot be expressed through existing repository methods.
+6. Add or update seeders for permissions, menus, resources, role grants, operations, or default configuration when the feature must appear in the UI or authorization catalog.
+7. Register new services in the module's `Extensions/ServiceCollectionExtensions.cs` when constructor injection requires it.
+8. Check the frontend API module and route/component path if the backend contract changes.
+
+## Verification
+
+Use focused verification first, then broaden when shared behavior changes.
+
+Common commands from the repository root:
+
+```bash
+dotnet build backend/XiHan.BasicApp.slnx
+dotnet test backend/XiHan.BasicApp.slnx
+dotnet run --project backend/src/main/XiHan.BasicApp.WebHost/XiHan.BasicApp.WebHost.csproj --launch-profile Development
+```
+
+When running locally, point `dotnet run --project` at `XiHan.BasicApp.WebHost.csproj`; the WebHost directory contains multiple project files, so the directory path is ambiguous.
+
+For runtime HTTP checks, do not hardcode hostnames or ports in guidance or scripts. Discover the current backend base URL from `Properties/launchSettings.json`, `Hosting:Urls`, `appsettings*.json`, environment variables, or the startup log. Then set a temporary shell variable and use it for checks:
+
+```bash
+# Set BACKEND_BASE_URL from the current launch/config output before running these.
+curl "$BACKEND_BASE_URL/health"
+curl -L "$BACKEND_BASE_URL/scalar"
+```
*** End Patch
```

- [ ] **Step 3: Verify backend skill frontmatter and key guardrails**

Run:

```bash
sed -n '1,4p' .ai/skills/xihan-basicapp-backend/SKILL.md
rg -n "DynamicApi|SqlSugar|TenantId = 0|XiHan.BasicApp.WebHost.csproj|Do not replace" .ai/skills/xihan-basicapp-backend/SKILL.md
```

Expected:

```text
---
name: xihan-basicapp-backend
description: Use when modifying, reviewing, documenting, or debugging the XiHan.BasicApp backend under backend/, including modules, Dynamic API application services, domain services, repositories, permissions, multi-tenancy, SqlSugar entities, seeders, health checks, SignalR, or Scalar contracts. Preserve XiHan.Framework, .NET 10, DynamicApi app-service boundaries, SqlSugar repositories, and the existing DDD/CQRS module layout; do not apply dotnet-business-backend scaffolding, Minimal API endpoint rewrites, or EF Core replacement unless the user explicitly asks for a separate migration.
---
```

The `rg` command should print lines containing each searched guardrail.

- [ ] **Step 4: Check whitespace**

Run:

```bash
git diff --check -- .ai/skills/xihan-basicapp-backend/SKILL.md
```

Expected: no output and exit code `0`.

- [ ] **Step 5: Commit the backend skill**

Run:

```bash
git add .ai/skills/xihan-basicapp-backend/SKILL.md
git commit -m "docs: add xihan backend ai skill"
```

Expected: commit succeeds with only the backend skill staged.

---

### Task 3: Frontend Skill And Final Validation

**Files:**
- Create: `.ai/skills/xihan-basicapp-frontend/SKILL.md`

**Interfaces:**
- Consumes: `AGENTS.md` link from Task 1 and backend skill path from Task 2.
- Produces: A valid skill named `xihan-basicapp-frontend` plus final validation for all AI guidance files.

- [ ] **Step 1: Verify the frontend skill is not already present**

Run:

```bash
test ! -f .ai/skills/xihan-basicapp-frontend/SKILL.md && echo "frontend skill missing as expected"
```

Expected:

```text
frontend skill missing as expected
```

- [ ] **Step 2: Create the frontend skill**

Run:

```bash
mkdir -p .ai/skills/xihan-basicapp-frontend
```

Expected: no output and exit code `0`.

Apply this patch:

```patch
*** Begin Patch
*** Add File: .ai/skills/xihan-basicapp-frontend/SKILL.md
+---
+name: xihan-basicapp-frontend
+description: Use when modifying, reviewing, documenting, or debugging the XiHan.BasicApp frontend under frontend/, including Vue pages, Naive UI screens, typed API modules, DTO types, Pinia stores, dynamic routes, permission checks, layouts, schema components, request handling, i18n, SignalR UI, or build verification. Preserve Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4, frontend/src, and frontend/packages; do not convert to Ant Design Vue or a generic app/pages/shared scaffold.
+---
+
+# XiHan.BasicApp Frontend Development
+
+## Start Here
+
+Before editing frontend code:
+
+1. Read `frontend/package.json`, `frontend/vite.config.ts`, and the nearest existing page/API/store files for the feature area.
+2. Inspect both aliases: `@` points to `frontend/src`, and `~` points to `frontend/packages`.
+3. Treat `vue-business-frontend` as inspiration for typed API facades, clear state boundaries, dense business UI, and verification habits only. Do not import its Ant Design Vue stack or generic `app/pages/shared` layout into this repository.
+
+## Project Shape
+
+The frontend is a Vue 3 business admin app:
+
+```text
+frontend/
+  src/
+    api/
+      modules/
+    app/
+    router/
+    styles/
+    types/
+    views/
+  packages/
+    components/
+    composables/
+    constants/
+    hooks/
+    layouts/
+    locales/
+    request/
+    router/
+    stores/
+    types/
+    utils/
+    views/
+```
+
+Use the existing ownership model:
+
+- `frontend/src/views/<domain>`: route-level business pages.
+- `frontend/src/api/modules/<domain>`: backend-facing typed API modules.
+- `frontend/src/api/modules/<domain>/*.types.ts`: DTO and query types for the adjacent API module.
+- `frontend/packages/request`: shared Axios request wrapper, token refresh, request logging, locale/timezone/security headers, and flat response helpers.
+- `frontend/packages/router`: core routes, static permission filtering, dynamic backend menu route mapping, and fallback view resolution.
+- `frontend/packages/stores`: Pinia stores for user, access, app, notification, chat, layout, tabs, favorites, and cross-page state.
+- `frontend/packages/components`: reusable schema, RBAC, chat, and common components.
+- `frontend/packages/locales`: i18n messages. Add user-facing text through locale keys when the surrounding feature is localized.
+
+## Non-Negotiable Boundaries
+
+- Keep Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4, Iconify, Axios, SignalR, and vue-i18n.
+- Do not convert UI work to Ant Design Vue.
+- Do not restructure the app into a generic `app/pages/shared` scaffold.
+- Keep backend calls behind typed API modules. Pages and stores should not call raw `fetch`.
+- Use `createDynamicApiClient('<ServiceName>')` for Dynamic API services and `formatDynamicApiRouteValue` for route values when adjacent modules do so.
+- Keep command/query API clients aligned with backend Dynamic API service names, such as `AiPrompt` and `AiPromptQuery`.
+- Keep DTO types adjacent to API modules in `*.types.ts` files and reuse shared primitives such as `ApiId`, `BasicDto`, `DateTimeString`, `PageRequest`, and `PageResult`.
+- Keep route metadata compatible with dynamic menu mapping: backend component paths may be converted by `frontend/packages/router/dynamic.ts`.
+- Use Pinia stores for cross-page state; keep page-local filters, drawers, modals, selection, pagination, and loading state in the page or a page-local composable.
+- Keep permission checks aligned with `usePermission`, `useUserStore`, `useAccessStore`, and backend permission codes. Hiding a button in the frontend never replaces backend permission enforcement.
+- Use Naive UI components and Iconify icons in action buttons, tables, forms, drawers, modals, tabs, tags, empty states, and tooltips.
+- Preserve the dense, work-focused admin style. Avoid marketing-style hero sections inside console pages.
+- Preserve mobile and narrow viewport usability when editing shared layouts, schema components, chat surfaces, auth pages, or high-traffic management pages.
+
+## Frontend Change Workflow
+
+For a new frontend capability:
+
+1. Locate the owning route page under `frontend/src/views/<domain>` or create it only when no suitable page exists.
+2. Add or extend typed DTOs in the adjacent `frontend/src/api/modules/<domain>/*.types.ts` file.
+3. Add or extend facade methods in `frontend/src/api/modules/<domain>/<feature>.ts`.
+4. Reuse existing schema/list/table/search/import/export components before creating new generic components.
+5. Add i18n keys in `frontend/packages/locales/langs/*` when nearby pages use locale keys.
+6. Add route metadata only in the appropriate static route file when the page is static; dynamic menu-driven pages should stay compatible with backend menu component paths.
+7. Add permission-aware UI actions using existing hooks/stores and the backend permission codes.
+8. Verify type safety and build output before reporting success.
+
+## Verification
+
+Use focused verification first, then broaden when shared behavior changes.
+
+Common commands from `frontend/`:
+
+```bash
+pnpm type-check
+pnpm build
+pnpm lint
+pnpm dev
+```
+
+For UI layout changes, inspect affected routes in a browser or screenshot workflow. Check that text does not overlap, tables fit their containers, icon-only actions have tooltips or labels, loading/empty/error states are visible, and dialogs/drawers are usable on narrow viewports.
*** End Patch
```

- [ ] **Step 3: Verify frontend skill frontmatter and key guardrails**

Run:

```bash
sed -n '1,4p' .ai/skills/xihan-basicapp-frontend/SKILL.md
rg -n "Naive UI|createDynamicApiClient|frontend/src/views|frontend/packages|pnpm type-check|Do not convert" .ai/skills/xihan-basicapp-frontend/SKILL.md
```

Expected:

```text
---
name: xihan-basicapp-frontend
description: Use when modifying, reviewing, documenting, or debugging the XiHan.BasicApp frontend under frontend/, including Vue pages, Naive UI screens, typed API modules, DTO types, Pinia stores, dynamic routes, permission checks, layouts, schema components, request handling, i18n, SignalR UI, or build verification. Preserve Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4, frontend/src, and frontend/packages; do not convert to Ant Design Vue or a generic app/pages/shared scaffold.
---
```

The `rg` command should print lines containing each searched guardrail.

- [ ] **Step 4: Validate all guidance files**

Run:

```bash
test -f AGENTS.md
test -f .ai/skills/xihan-basicapp-backend/SKILL.md
test -f .ai/skills/xihan-basicapp-frontend/SKILL.md
rg -n "^---$|^name: xihan-basicapp-|^description:" .ai/skills/xihan-basicapp-backend/SKILL.md .ai/skills/xihan-basicapp-frontend/SKILL.md
git diff --check -- AGENTS.md .ai/skills/xihan-basicapp-backend/SKILL.md .ai/skills/xihan-basicapp-frontend/SKILL.md
git status --short
```

Expected: the first three `test` commands produce no output and exit `0`. The `rg` command prints frontmatter delimiter, name, and description lines for both skill files. `git diff --check` produces no output and exits `0`. `git status --short` shows `.ai/skills/xihan-basicapp-frontend/SKILL.md` as untracked or staged plus any pre-existing `.DS_Store` files; it must not show `.DS_Store` staged.

- [ ] **Step 5: Commit the frontend skill**

Run:

```bash
git add .ai/skills/xihan-basicapp-frontend/SKILL.md
git commit -m "docs: add xihan frontend ai skill"
```

Expected: commit succeeds with only the frontend skill staged.

- [ ] **Step 6: Confirm final repository state**

Run:

```bash
git status --short
git log --oneline -4
```

Expected: only pre-existing untracked `.DS_Store` files remain in `git status --short`, and the log includes the spec commit plus the three implementation commits.
