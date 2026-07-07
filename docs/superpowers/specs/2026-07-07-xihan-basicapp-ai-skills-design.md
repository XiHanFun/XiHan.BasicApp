# XiHan.BasicApp AI Skills Design

## Goal

Create two project-local AI skills that help future AI agents work inside XiHan.BasicApp with the repository's real conventions:

- Backend skill: adapt the boundary discipline of `dotnet-business-backend` to XiHan.BasicApp without importing its scaffold, Minimal API, EF Core, or template conventions.
- Frontend skill: adapt the maintainability discipline of `vue-business-frontend` to XiHan.BasicApp without replacing this project's Vue, Naive UI, package, route, and API patterns.

The skills must live in the repository root area and be discoverable by most AI coding agents through a small root entry file.

## Approved Approach

Use a root `AGENTS.md` as the universal AI entry point and place the actual skills under `.ai/skills/`:

```text
AGENTS.md
.ai/
  skills/
    xihan-basicapp-backend/
      SKILL.md
    xihan-basicapp-frontend/
      SKILL.md
```

`AGENTS.md` stays short. It routes backend work to the backend skill and frontend work to the frontend skill. The detailed conventions live in the two `SKILL.md` files so future agents can load only the side they need.

## Backend Skill Design

Path: `.ai/skills/xihan-basicapp-backend/SKILL.md`

Purpose:

- Guide backend feature work, refactors, reviews, and documentation for `backend/`.
- Preserve XiHan.BasicApp's existing architecture: `.NET 10`, `XiHan.Framework`, modular `XiHanModule`, DDD-style module folders, CQRS-style command/query services, Dynamic API, SqlSugar, Redis, SignalR, Scalar, and multi-tenancy.
- Use `dotnet-business-backend` only as a reference for bounded files, explicit dependencies, tests, and verification habits.

Core rules to encode:

- Do not scaffold or migrate this repository to the `dotnet-business-backend` template.
- Do not replace Dynamic API application services with Minimal API endpoint groups.
- Do not replace SqlSugar repositories with EF Core patterns.
- Keep module boundaries aligned with existing projects: `framework`, `modules`, and `main`.
- For business modules, follow the existing `Domain`, `Application`, `Infrastructure`, `Seeders`, `Extensions`, and `Hubs` layout where applicable.
- Expose command APIs through `[DynamicApi]` app services and interfaces under `Application/Contracts`.
- Keep write paths transactional with `[UnitOfWork(true)]` where existing patterns require it.
- Keep permissions explicit with permission-code constants, seeders, and `[PermissionAuthorize]`.
- Keep tenant rules consistent with `BasicAppEntity`: global records use `TenantId = 0`; tenant records use the active tenant context.
- Use `BasicApp*Entity` base classes and SqlSugar attributes for entities.
- Keep repositories behind domain contracts and reuse `SaasRepository<T>` / `ISqlSugarClientResolver` patterns where the module depends on Saas.
- Keep DTOs, contracts, mappers, app services, query services, domain services, repositories, and seeders in focused files.

Verification guidance:

- Prefer focused `dotnet test` or `dotnet build` commands against `backend/XiHan.BasicApp.slnx` or the affected project.
- Use the explicit WebHost project path when running locally: `backend/src/main/XiHan.BasicApp.WebHost/XiHan.BasicApp.WebHost.csproj`.
- For runtime checks, verify `/health` and `/scalar` when backend startup behavior changes.

## Frontend Skill Design

Path: `.ai/skills/xihan-basicapp-frontend/SKILL.md`

Purpose:

- Guide frontend feature work, refactors, reviews, and documentation for `frontend/`.
- Preserve the current stack: Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4, Iconify, Axios request layer, SignalR, and i18n.
- Use `vue-business-frontend` only as a reference for typed facades, clear state boundaries, dense enterprise UI, and verification habits.

Core rules to encode:

- Do not convert the project to Ant Design Vue or a generic `app/pages/shared` scaffold.
- Preserve the existing split between `frontend/src` and `frontend/packages`.
- Put route-level business pages under `frontend/src/views/<domain>`.
- Put reusable framework-level components, stores, hooks, layouts, request code, router helpers, locales, constants, and shared types under `frontend/packages`.
- Keep backend calls behind typed API modules under `frontend/src/api/modules/<domain>` and use `createDynamicApiClient` for Dynamic API surfaces.
- Keep DTO types in adjacent `*.types.ts` files and shared primitive types in `frontend/src/api/types` or existing shared modules.
- Keep route metadata compatible with dynamic menu mapping and backend component paths.
- Use Pinia stores for cross-page state and composables for reusable UI logic; keep page-local state in the page.
- Use Naive UI components, Iconify icons, i18n keys, and existing schema/list components for dense admin workflows.
- Keep permission checks aligned with `usePermission`, `useUserStore`, `useAccessStore`, and backend permission codes.
- Preserve mobile/narrow viewport usability for shared layouts and high-traffic pages.

Verification guidance:

- Run `pnpm type-check` and `pnpm build` for meaningful TypeScript or route/API changes.
- Run `pnpm lint` when formatting, imports, or broad frontend files change.
- For UI layout changes, start the Vite server and inspect affected routes with browser automation or screenshots when feasible.

## Error Handling And Safety

- These files are guidance only; they must not change runtime behavior.
- The skills should warn future agents not to rewrite architecture or introduce unrelated framework migrations while doing ordinary feature work.
- The root entry should be lightweight enough that tools reading only root guidance still get the correct next file to read.
- Existing untracked local files such as `.DS_Store` are out of scope and must not be staged.

## Acceptance Criteria

- `AGENTS.md` exists at the repository root and points to both skill files.
- `.ai/skills/xihan-basicapp-backend/SKILL.md` exists with valid `name` and `description` frontmatter and project-specific backend guidance.
- `.ai/skills/xihan-basicapp-frontend/SKILL.md` exists with valid `name` and `description` frontmatter and project-specific frontend guidance.
- The skill files avoid unfinished markers and avoid claims that conflict with the observed XiHan.BasicApp architecture.
- The implementation is verified by checking the files exist, frontmatter is present, and `git diff --check` passes.
