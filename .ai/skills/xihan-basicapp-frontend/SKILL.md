---
name: xihan-basicapp-frontend
description: Use when modifying, reviewing, documenting, or debugging the XiHan.BasicApp frontend under frontend/, including Vue pages, Naive UI screens, typed API modules, DTO types, Pinia stores, dynamic routes, permission checks, layouts, schema components, request handling, i18n, SignalR UI, or build verification. Preserve Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4, frontend/src, and frontend/packages; do not convert to Ant Design Vue or a generic app/pages/shared scaffold.
---

# XiHan.BasicApp Frontend Development

## Start Here

Before editing frontend code:

1. Read `frontend/package.json`, `frontend/vite.config.ts`, and the nearest existing page/API/store files for the feature area.
2. Inspect both aliases: `@` points to `frontend/src`, and `~` points to `frontend/packages`.
3. Treat `vue-business-frontend` as inspiration for typed API facades, clear state boundaries, dense business UI, and verification habits only. Do not import its Ant Design Vue stack or generic `app/pages/shared` layout into this repository.

## Project Shape

The frontend is a Vue 3 business admin app:

```text
frontend/
  src/
    api/
      modules/
    app/
    router/
    styles/
    types/
    views/
  packages/
    components/
    composables/
    constants/
    hooks/
    layouts/
    locales/
    request/
    router/
    stores/
    types/
    utils/
    views/
```

Use the existing ownership model:

- `frontend/src/views/<domain>`: route-level business pages.
- `frontend/src/api/modules/<domain>`: backend-facing typed API modules.
- `frontend/src/api/modules/<domain>/*.types.ts`: DTO and query types for the adjacent API module.
- `frontend/packages/request`: shared Axios request wrapper, token refresh, request logging, locale/timezone/security headers, and flat response helpers.
- `frontend/packages/router`: core routes, static permission filtering, dynamic backend menu route mapping, and fallback view resolution.
- `frontend/packages/stores`: Pinia stores for user, access, app, notification, chat, layout, tabs, favorites, and cross-page state.
- `frontend/packages/components`: reusable schema, RBAC, chat, and common components.
- `frontend/packages/locales`: i18n messages. Add user-facing text through locale keys when the surrounding feature is localized.

## Non-Negotiable Boundaries

- Keep Vue 3, Vite, TypeScript, Pinia, Vue Router, Naive UI, Tailwind CSS 4, Iconify, Axios, SignalR, and vue-i18n.
- Do not convert UI work to Ant Design Vue.
- Do not restructure the app into a generic `app/pages/shared` scaffold.
- Keep backend calls behind typed API modules. Pages and stores should not call raw `fetch`.
- Use `createDynamicApiClient('<ServiceName>')` for Dynamic API services and `formatDynamicApiRouteValue` for route values when adjacent modules do so.
- Keep command/query API clients aligned with backend Dynamic API service names, such as `AiPrompt` and `AiPromptQuery`.
- Keep DTO types adjacent to API modules in `*.types.ts` files and reuse shared primitives such as `ApiId`, `BasicDto`, `DateTimeString`, `PageRequest`, and `PageResult`.
- Keep route metadata compatible with dynamic menu mapping: backend component paths may be converted by `frontend/packages/router/dynamic.ts`.
- Use Pinia stores for cross-page state; keep page-local filters, drawers, modals, selection, pagination, and loading state in the page or a page-local composable.
- Keep permission checks aligned with `usePermission`, `useUserStore`, `useAccessStore`, and backend permission codes. Hiding a button in the frontend never replaces backend permission enforcement.
- Use Naive UI components and Iconify icons in action buttons, tables, forms, drawers, modals, tabs, tags, empty states, and tooltips.
- Preserve the dense, work-focused admin style. Avoid marketing-style hero sections inside console pages.
- Preserve mobile and narrow viewport usability when editing shared layouts, schema components, chat surfaces, auth pages, or high-traffic management pages.

## Frontend Change Workflow

For a new frontend capability:

1. Locate the owning route page under `frontend/src/views/<domain>` or create it only when no suitable page exists.
2. Add or extend typed DTOs in the adjacent `frontend/src/api/modules/<domain>/*.types.ts` file.
3. Add or extend facade methods in `frontend/src/api/modules/<domain>/<feature>.ts`.
4. Reuse existing schema/list/table/search/import/export components before creating new generic components.
5. Add i18n keys in `frontend/packages/locales/langs/*` when nearby pages use locale keys.
6. Add route metadata only in the appropriate static route file when the page is static; dynamic menu-driven pages should stay compatible with backend menu component paths.
7. Add permission-aware UI actions using existing hooks/stores and the backend permission codes.
8. Verify type safety and build output before reporting success.

## Verification

Use focused verification first, then broaden when shared behavior changes.

Common commands from `frontend/`:

```bash
pnpm type-check
pnpm build
pnpm lint
pnpm dev
```

For UI layout changes, inspect affected routes in a browser or screenshot workflow. Check that text does not overlap, tables fit their containers, icon-only actions have tooltips or labels, loading/empty/error states are visible, and dialogs/drawers are usable on narrow viewports.
