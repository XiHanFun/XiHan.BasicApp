![logo](../assets/logo.png)

[中文](README_cn.md)

# XiHan.BasicApp Frontend

This file is for frontend developers and contributors. It documents **how this layer is organized**: the stack, the monorepo structure, the architectural constraints and local development.

If you just want to get the whole system running, start from the [repository README](../README.md); topic-by-topic documentation lives on the [documentation site](https://basicapp.docs.xihanfun.com).

## Stack

Vue 3 + TypeScript + Vite. The application root only carries vue, vue-router, pinia, vue-i18n, `@vueuse/core` and six `@xihan-ui/*` packages as runtime dependencies; every heavier library is pushed down into the workspace package that owns it.

Tooling: Vite, `@vitejs/plugin-vue(-jsx)`, vue-tsc, Turbo, Tailwind CSS v4 (`@tailwindcss/vite`, CSS-first with no JS config), unplugin-auto-import, unplugin-vue-components, oxlint + ESLint.

## Requirements

`packageManager` is `pnpm@11.7.0`; `engines` requires Node ≥ 24 and pnpm ≥ 11.

`@xihan-ui/*` comes from the published npm releases (pinned to `^1.1.0` in the `pnpm-workspace.yaml` catalog), so cloning this repository on its own and running `pnpm install` just works — no sibling `XiHan.UI` checkout required.

> To debug against the component library sources, add an `overrides` block to `pnpm-workspace.yaml` pointing `@xihan-ui/*` at `link:../../XiHan.UI/ui/packages/*`, then remove it when you are done. That is a local-only change; do not commit it.

## Monorepo Structure

`pnpm-workspace.yaml` declares `packages/*` — 19 private workspace packages (all named `@xihan/<name>`, version `0.0.0`, aliased as `~/`): chat, components, composables, constants, design, diagram, hooks, iconify, layouts, locales, printing, request, router, stores, types, utils, views and more.

Heavier third-party libraries live in the package that owns them, never at the application root: axios → request, `@antv/x6` → diagram, tiptap / monaco / md-editor-v3 / jsoneditor → components, `@microsoft/signalr` / pinyin-pro → composables, vue-plugin-hiprint → printing, emoji-mart → chat, eight `@iconify-json` sets → iconify, dayjs / papaparse → utils.

Every third-party version is declared once in the `catalog:` block of `pnpm-workspace.yaml` (~75 entries); packages just write `"catalog:"`.

Two path aliases: `@` → `./src` (application layer) and `~` → `./packages` (infrastructure layer).

### Architectural Constraint

The named ESLint rule `xihan/packages-no-reverse-dep` forbids `packages/**` from importing `@/*` or `../../src/**` — contract types belong in `packages/types`, and runtime dependencies are injected through the app context. Four files currently hold an exemption pending migration.

## Directory Layout

```text
frontend/
├── src/
│   ├── api/          # base·request·factory·helpers·types + 21 domain modules
│   ├── app/          # application composition
│   ├── components/   # application-level components
│   ├── constants/
│   ├── locales/      # zh-CN / en-US
│   ├── modules/      # pluggable business modules, see below
│   ├── router/
│   ├── styles/
│   ├── types/
│   └── views/        # 55 pages across 10 groups
├── packages/         # 19 workspace packages (infrastructure layer)
└── public/
```

### Pluggable Modules

`src/modules/<module>/` may only contain `views/`, `api/`, `locales/`, `setup.ts` and `README.md`. `main.ts` discovers startup hooks with `import.meta.glob('/src/modules/*/setup.ts', { eager: true })` — **deleting the directory uninstalls the module; no registration code has to change**.

The five modules ai / chat / codegen / printing / workflow map one-to-one onto the optional backend modules and contribute 35 pages in total.

### Routing Model

Routes are driven by the backend menu by default: PascalCase component paths map to kebab-case files, `coreComponentMap` covers the `_core` pages that live in packages, and a not-found route catches the rest. Set `VITE_AUTH_ROUTE_MODE=static` to switch to frontend static permission filtering. History mode defaults to hash; set `VITE_ROUTER_HISTORY=history` for HTML5.

## Local Development

```bash
pnpm install
pnpm dev          # dev server, port 9800 by default
pnpm build        # production build (runs type-check first)
pnpm type-check
pnpm lint         # oxlint + eslint
pnpm test         # Vitest (jsdom)
pnpm validate     # the i18n and module-convention gates
```

All commands are delegated through Turbo to `app:*`.

### Quality Gates

- `app:build` depends on `app:type-check` — a type error stops the build outright, container image builds included
- `validate` runs two dependency-free Node gates: `validate-i18n.mjs` checks that zh-CN and en-US keys are symmetric and scans `t('...')` calls for orphan keys (vue-i18n silently returns the key itself at runtime, so neither type-check nor ESLint catches this — orphans must be zero); `validate-modules.mjs` checks the module directory convention, that view keys do not collide, and that locales come in pairs
- `postinstall` installs the git hooks; pre-commit runs lint-staged: ts/vue/js/mjs through oxlint + ESLint fixes, json/css/html/md/yaml through Prettier

## Styling and Theming

`src/styles/index.css` declares the complete `@layer` order in one place:

```text
xihan.reset, xihan.tokens, xihan.motion, xihan.components,
theme, base, components, utilities, xihan.overrides
```

Tailwind utilities deliberately sit after `xihan.components`; a component's `<style scoped>` belongs to no layer and therefore always wins. Tailwind's preflight is intentionally not imported — the base reset comes from XiHan.UI's `xihan.reset` layer. `packages/design/xihan-ui.css` is the token bridge that rewrites `--xh-*` onto the application palette, and must come after the XiHan.UI skins.

## Component and API Auto-Import

A custom `XiHanUiResolver` imports any `Xh`-prefixed tag from `@xihan-ui/vue` as a named import, so anatomy-style multi-part components need no per-part imports and stay tree-shakeable. `unplugin-vue-components` is configured with an empty `dirs` array — the application's own components are **not** auto-registered. Auto-imported APIs come from vue, vue-router, pinia and `@vueuse/core`.

## Environment Variables

Three `.env` files are committed (`.env`, `.env.development`, `.env.production`):

| Group                 | Variables                                                                                                                                                                                                                                  |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Application identity  | `VITE_APP_TITLE` / `SUBTITLE` / `DESCRIPTION` / `LOGO` / `NAMESPACE` / `VERSION`                                                                                                                                                           |
| API and routing       | `VITE_API_BASE_URL` (left empty in development to use the same-origin proxy and avoid CORS), `VITE_API_PREFIX=/api`, `VITE_DEV_PROXY_TARGET=http://localhost:9708`, `VITE_PORT=9800`, `VITE_ROUTER_HISTORY`, `VITE_BASE`, `VITE_HOME_PATH` |
| API security pipeline | eight `VITE_API_SECURITY_*` variables; `ENABLED` is false in all three .env files (off by default)                                                                                                                                         |
| Printing              | `VITE_HIPRINT_HOST` / `VITE_HIPRINT_TOKEN`                                                                                                                                                                                                 |

## Build and Deployment

The build targets es2022 and uses a hand-written `manualChunks` split — vendor-ui, vendor-printing (the hiprint family is loaded dynamically and stays out of the admin first paint), vendor-monaco, vendor-icon-`<set>` and a dozen more — emitting `assets/js/[name]-[hash].js`.

`Dockerfile` is a two-stage build (`node:24-alpine` → `nginx:1.29-alpine`); `ARG VITE_API_BASE_URL` is written into `.env.production.local`, which outranks `.env.production`, and an empty value means same-origin. `nginx.conf` defines `upstream basicapp_backend = backend:9708`, serves the SPA through `try_files`, caches `/assets/` for a year as immutable while keeping `index.html` no-cache, and proxies `/api/` (300s), `/hubs/` (SignalR: Upgrade headers, `proxy_buffering off`, 3600s) and `/uploads/`, with `client_max_body_size 100m`.

## Removing Optional Modules

Delete `src/modules/<module>/` along with the matching `packages/{chat,printing,diagram}` and their catalog entries.

⚠️ **Removal must be paired with rebuilding the database** — seeded menus and permission codes are not reclaimed automatically.
