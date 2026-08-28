<div align="center">

<img src="./assets/banner.png" alt="XiHan.BasicApp" />

<h1>XiHan.BasicApp</h1>

<p><b>A beautifully crafted admin kernel built on .NET and Vue</b></p>

<p>A .NET 10 backend on <a href="https://github.com/XiHanFun/XiHan.Framework">XiHan.Framework</a>, a Vue 3 frontend on <a href="https://github.com/XiHanFun/XiHan.UI">XiHan.UI</a><br/>Multi-tenancy · RBAC with data scopes and field masking · Code generation · Realtime</p>

<p><b>English</b> | <a href="./README_cn.md">简体中文</a></p>

<p>
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/stargazers"><img alt="GitHub Stars" src="https://img.shields.io/github/stars/XiHanFun/XiHan.BasicApp?style=flat-square&logo=github&label=Stars&color=1f6feb" /></a>
  <a href="https://gitee.com/XiHanFun/XiHan.BasicApp"><img alt="Gitee Stars" src="https://gitee.com/XiHanFun/XiHan.BasicApp/badge/star.svg" /></a>
  <a href="https://gitcode.com/XiHanFun/XiHan.BasicApp"><img alt="GitCode Stars" src="https://gitcode.com/XiHanFun/XiHan.BasicApp/star/badge.svg" /></a>
</p>


<p>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" />
  <a href="https://github.com/XiHanFun/XiHan.Framework"><img alt="XiHan.Framework" src="https://img.shields.io/badge/XiHan.Framework-4.0.0-6f42c1?style=flat-square" /></a>
  <img alt="Vue" src="https://img.shields.io/badge/Vue-3.5-4FC08D?style=flat-square&logo=vuedotjs&logoColor=white" />
  <img alt="TypeScript" src="https://img.shields.io/badge/TypeScript-6.0-3178C6?style=flat-square&logo=typescript&logoColor=white" />
  <img alt="Vite" src="https://img.shields.io/badge/Vite-8-646CFF?style=flat-square&logo=vite&logoColor=white" />
  <img alt="Tailwind CSS" src="https://img.shields.io/badge/Tailwind_CSS-4-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white" />
  <a href="https://www.nuget.org/packages?q=XiHan.BasicApp"><img alt="NuGet" src="https://img.shields.io/nuget/v/XiHan.BasicApp.Core?style=flat-square&logo=nuget&logoColor=white&label=NuGet&color=004880" /></a>
</p>

<p>
  <a href="./LICENSE"><img alt="License" src="https://img.shields.io/github/license/XiHanFun/XiHan.BasicApp?style=flat-square&color=green" /></a>
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/commits"><img alt="Last Commit" src="https://img.shields.io/github/last-commit/XiHanFun/XiHan.BasicApp?style=flat-square&color=blueviolet" /></a>
  <img alt="Commit Activity" src="https://img.shields.io/github/commit-activity/m/XiHanFun/XiHan.BasicApp?style=flat-square" />
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/issues"><img alt="Issues" src="https://img.shields.io/github/issues/XiHanFun/XiHan.BasicApp?style=flat-square" /></a>
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/graphs/contributors"><img alt="Contributors" src="https://img.shields.io/github/contributors/XiHanFun/XiHan.BasicApp?style=flat-square" /></a>
  <img alt="Repo Size" src="https://img.shields.io/github/repo-size/XiHanFun/XiHan.BasicApp?style=flat-square" />
</p>

<p>
  <a href="https://deepwiki.com/XiHanFun/XiHan.BasicApp"><img alt="Ask DeepWiki" src="https://deepwiki.com/badge.svg" /></a>
  <a href="https://basicapp.docs.xihanfun.com"><img alt="Docs" src="https://img.shields.io/badge/Docs-basicapp.docs.xihanfun.com-2496ED?style=flat-square&logo=readthedocs&logoColor=white" /></a>
  <a href="https://qm.qq.com/q/qYp1Urv3z2"><img alt="QQ Group" src="https://img.shields.io/badge/QQ_Group-462371834-EB1923?style=flat-square&logo=tencentqq&logoColor=white" /></a>
</p>
<p>
  <a href="https://trendshift.io/repositories/83127?utm_source=trendshift-badge&amp;utm_medium=badge&amp;utm_campaign=badge-trendshift-83127" target="_blank" rel="noopener noreferrer"><img src="https://trendshift.io/api/badge/trendshift/repositories/83127/daily?language=C%23" alt="XiHanFun%2FXiHan.BasicApp | Trendshift" width="250" height="55"/></a>
</p>


<img src="./assets/login.png" alt="Sign in" />

</div>

## Introduction

XiHan.BasicApp is a decoupled frontend/backend system. The backend follows DDD layering — writes go through application services, reads through query services — and application services are exposed directly as REST endpoints by the dynamic API convention. The frontend is Vue 3 + TypeScript + XiHan.UI. Identity, permissions, tenancy and auditing are built in, so it works both as the starting point for an admin project and as a reference for full-stack .NET + Vue practice. XiHan.BasicApp is the application layer of the XiHanFun open-source ecosystem, which spans foundation, components and applications.

## Documentation

| Destination | Contents |
| --- | --- |
| [Documentation site](https://basicapp.docs.xihanfun.com) | Full guides and topic-by-topic documentation |
| [Backend engineering notes](./backend/README.md) | Structure, project catalog, endpoint exposure, dependencies, local development |
| [Frontend engineering notes](./frontend/README.md) | Stack, monorepo structure, architectural constraints, local development |

## Preview

<table>
  <tr>
    <td align="center"><img src="./assets/tenant-select.png" /><br/>Tenant selection</td>
    <td align="center"><img src="./assets/user-management.png" /><br/>User management</td>
  </tr>
  <tr>
    <td align="center"><img src="./assets/user-management-dark.png" /><br/>User management (dark)</td>
    <td align="center"><img src="./assets/server-monitor.png" /><br/>Server monitoring</td>
  </tr>
  <tr>
    <td align="center"><img src="./assets/preference-settings.png" /><br/>Preferences</td>
    <td align="center"><img src="./assets/operation-log.png" /><br/>Operation log</td>
  </tr>
  <tr>
    <td align="center"><img src="./assets/log-traceability.png" /><br/>Log traceability</td>
    <td align="center"><img src="./assets/about.png" /><br/>About</td>
  </tr>
</table>



Mobile:

<img src="./assets/mobile.png" />

## Features

**Identity and authentication**

- User, role, department and menu management
- JWT with two tokens (access 120 minutes + refresh 7 days), multi-device sign-in and session management
- Multiple sign-in methods: password, email / SMS code, 2FA (TOTP / email / SMS, combinable as flags)
- Eight first-party OAuth providers built in — GitHub, Gitee, Google, QQ, WeChat, WeCom, DingTalk, Lark — enabled by configuration
- Sign-in protection: first-party SVG captcha (single-use, can be turned off), throttling on both account+IP and IP alone, failure lockout, forced password change after signing in with the default password
- PBKDF2 password hashing (OWASP-recommended iterations); one-time codes are destroyed on consumption and compared in constant time

**Permissions**

- Three-segment permission codes `module:resource:action`; the super administrator wildcard is `*`
- Role hierarchy through a closure table, five data scopes (self / own department / department and children / all / custom), six field-masking strategies
- Static separation of duties: mutually exclusive roles and cardinality constraints, evaluated per constraint group
- Permission requests with approval, revocable temporary delegation, and a change audit trail

**Multi-tenancy**

- Column-level isolation by default with `TenantId=0` reserved for global data; per-tenant databases are also supported
- Globally unique email sign-in that routes the user by membership (console / workbench / tenant picker), with tenant switching at any time
- Platform-mode operations for super administrators, who can step into any tenant
- Runtime gating by tenant edition allowlist; onboarding creates the administrator, roles and grants in one go; downgrading reclaims grants that fall outside the allowlist

**Audit logging**

- Six independent log streams: access, API, operation, exception, sign-in and entity change
- Automatic masking before persistence (passwords, tokens, secrets, ID numbers and more, with a counter-example allowlist to avoid over-masking); entity changes distinguish create / update / delete / restore
- Traceability timeline: aggregate across log types by trace ID, username, session, IP or user ID

**Code generation**

- Single-table, tree and master-detail modes: entities, DTOs, APIs and frontend pages in one shot
- Native Scriban templates embedded into the assembly, each with a manual variant; menu permissions, permission seeds, page descriptors and seeders are generated alongside
- Download as a zip or write to disk; writing to disk is off by default and requires an explicit absolute-path allowlist root, with fail-closed path traversal checks

**AI**

- Provider onboarding and API-key custody (encrypted at rest with DataProtection; chat and embedding models are configurable)
- Prompt library stored in the database, able to override the framework defaults
- Knowledge-base RAG: document ingestion and vector search (Qdrant), isolated per tenant, keeping the raw content so indexes can be rebuilt
- Registered skills automatically become chat tools and MCP tools; knowledge retrieval ships built in, and business skills join by implementing `IAiSkill`
- Configurable assistants, bridged into the chat page

**Platform**

- Dynamic APIs: application services are exposed through `[DynamicApi]` with no controller boilerplate, and Scalar documentation is generated automatically
- One source of truth for menus: the backend `PageRegistry` registers menu, route, component path, permission code and i18n key together
- Distributed caching end to end (authorization snapshots, edition gating, menus, settings, dictionaries) with precise invalidation on write paths
- Request tracing; two SignalR hubs (notifications and chat)
- Message center: email / SMS / in-app notification templates, overridable per tenant
- Open platform: a built-in OAuth2 / OIDC provider (third-party app registration, user consent) and per-user OpenAPI credentials with signed calls
- Server monitoring (motherboard / CPU / memory / disk / GPU / network / runtime), cache key lookup and pattern-based clearing
- Multi-backend file storage (local / Aliyun OSS / Tencent COS / MinIO), database-backed scheduled jobs, review workflow, i18n (Chinese / English)
- Export center: async tasks with CSV and XLSX writers on a delayed queue

**Frontend experience**

- Schema-driven list pages: search, table and export generated from configuration, with column settings, density switching, advanced search, saved personal views, row hover previews, tree mode and column resizing
- Permission-, tenant- and preference-aware: pages, fields and actions filtered by permission code with field masking; column and search preferences sync to the backend and stay consistent across devices
- Dynamic-island feedback, tabbed pages, favorites, command-palette global search
- Message center: top banner, sign-in dialog, notification center, with forced reading and role/department targeting
- Preference center: light/dark theme, brand color, layout style and density, synced to the cloud
- Rich text (Tiptap) and markdown editors, visual cron builder, JSON editor/viewer, code editor
- Lock screen, watermark, time zone switching, export center

## Tech Stack

Backend: .NET 10 with XiHan.Framework 4.0.0 (SqlSugar, Redis, SignalR, Serilog and Scalar all arrive through the framework). Frontend: Vue 3 + TypeScript + Vite 8 + XiHan.UI + Pinia + Tailwind CSS 4.

Item-by-item lists live in the [backend](./backend/README.md#dependency-footprint) and [frontend](./frontend/README.md#stack) engineering notes.

## Architecture

The system splits into a framework layer, a module layer and the host application; each module follows DDD layering internally (domain / application / infrastructure).

```text
┌─────────────────────────────────────────────────────────────┐
│                   XiHan.BasicApp.WebHost                    │
│             (startup host, module composition)              │
├──────────┬──────────┬──────────┬──────────┬─────────────────┤
│ CodeGen  │    AI    │ Workflow │ Printing │      Chat       │
│(codegen) │ (AI/RAG) │(workflow)│(printing)│     (chat)      │
├──────────┴──────────┴──────────┴──────────┴─────────────────┤
│                     XiHan.BasicApp.Saas                     │
│    (RBAC / tenancy / org / approval / audit / messaging)    │
├─────────────────────────────────────────────────────────────┤
│                   XiHan.BasicApp.Web.Core                   │
│     (web base / dynamic API / docs / maintenance mode)      │
├─────────────────────────────────────────────────────────────┤
│                     XiHan.BasicApp.Core                     │
│            (application base / DDD / modularity)            │
├─────────────────────────────────────────────────────────────┤
│                      XiHan.Framework.*                      │
│ (auth / authorization / data / caching / events / tenancy)  │
└─────────────────────────────────────────────────────────────┘
```

| Project | Description | Removable |
| --- | --- | --- |
| `XiHan.BasicApp.Core` | Application base composing the non-web framework modules and shared conventions | No |
| `XiHan.BasicApp.Web.Core` | Web base composing six framework web modules, provides the maintenance-mode middleware | No |
| `XiHan.BasicApp.Saas` | Platform governance: users / roles / permissions / menus / departments / tenants / settings / dictionaries / files / notifications / approvals / logs / jobs | No |
| `XiHan.BasicApp.CodeGeneration` | Code generation: data sources / schema import / template configuration / full-stack output | Yes |
| `XiHan.BasicApp.AI` | AI: providers and key custody / prompt library / knowledge-base RAG / skills as MCP tools / chat assistant | Yes (the assistant bridge depends on Chat) |
| `XiHan.BasicApp.Workflow` | Workflow: definitions / instances / todos — persistence and APIs over the framework engine | Yes |
| `XiHan.BasicApp.Printing` | Print templates: visual design / tenant and platform scopes / resolution by code | Yes |
| `XiHan.BasicApp.Chat` | Chat: direct / group / department / assistant conversations, realtime delivery, compliance auditing | Yes (also handle the AI assistant bridge) |
| `XiHan.BasicApp.WebHost` | Startup host composing every module | — |

```text
XiHan.BasicApp/
├── backend/                 # backend (.NET 10)
│   ├── src/
│   │   ├── framework/       #   Core / Web.Core base capabilities
│   │   ├── modules/         #   Saas + five optional modules (CodeGen/AI/Workflow/Printing/Chat)
│   │   └── main/            #   WebHost startup entry
│   ├── props/               #   shared MSBuild properties
│   ├── scripts/             #   version bump and cleanup scripts
│   └── test/                #   test projects
├── frontend/                # frontend (Vue 3 + XiHan.UI)
│   ├── src/                 #   application sources (src/modules/ mirrors the optional backend modules)
│   └── packages/            #   internal packages
└── assets/                  # README assets
```

### Removing Optional Modules

An optional module is one backend project plus one `src/modules/<module>` directory on the frontend. The per-side steps live in the [backend](./backend/README.md#removing-optional-modules) and [frontend](./frontend/README.md#removing-optional-modules) engineering notes.

⚠️ **Removal must be paired with rebuilding the database**: seeded menus, permissions, role grants and scheduled jobs are not reclaimed when a module is deleted.

## Getting Started

### Requirements

| Dependency | Version | Notes |
| --- | --- | --- |
| .NET SDK | 10.0+ | required by the backend |
| Node.js | 24.0+ | required by the frontend |
| pnpm | 11.0+ | required by the frontend |
| PostgreSQL | 14+ | the only hard requirement; MySQL / SQL Server / SQLite / Oracle also work |
| Redis | 6.0+ | optional; falls back to in-process memory caching when disabled |
| Qdrant | v1.15+ | only needed for the AI knowledge base |

### One Command with Containers

The repository root ships a `docker-compose.yml` with five services — PostgreSQL, Redis, Qdrant, backend and frontend:

```bash
cp .env.example .env
docker compose up -d
```

The frontend defaults to `http://localhost:8080` and the backend to `http://localhost:9708`; both ports are configurable in `.env`.

### Backend

```bash
git clone https://github.com/XiHanFun/XiHan.BasicApp.git
cd XiHan.BasicApp/backend

dotnet run --project src/main/XiHan.BasicApp.WebHost --launch-profile Development
```

Then open `http://127.0.0.1:9708/scalar` for the API documentation. Ports per environment: Development `9708`, Production `9709`.

The connection string goes into `XiHan:Data:SqlSugarCore:ConnectionConfigs` in `backend/src/main/XiHan.BasicApp.WebHost/appsettings.Development.json`. The first start creates the schema and seeds it automatically.

The framework is consumed from NuGet by default, so cloning this repository alone is enough to build. See the [backend engineering notes](./backend/README.md#how-the-framework-is-referenced) for working on the framework at the same time.

### Frontend

```bash
cd frontend
pnpm install
pnpm dev
```

> ⚠️ The `overrides` block in `pnpm-workspace.yaml` points `@xihan-ui/*` at a sibling `../../XiHan.UI` checkout, so **`pnpm install` fails without one**. To use the published npm versions instead, delete that overrides block.

### Default Account

The initial super administrator is `superadmin` with the password `SuperAdmin@123`. Override it through `Saas:Seed:SuperAdminPassword` (environment variable `Saas__Seed__SuperAdminPassword`). Always override it in production, and change it right after the first sign-in.

## Ecosystem

- [XiHan.Framework](https://github.com/XiHanFun/XiHan.Framework) - A fast, lightweight, efficient and thoughtfully built modern modular framework for .NET
- [XiHan.UI](https://github.com/XiHanFun/XiHan.UI) - A fast, lightweight, efficient and thoughtfully built framework-agnostic component library
- [XiHan.BasicApp](https://github.com/XiHanFun/XiHan.BasicApp) - A beautifully crafted admin kernel built on .NET and Vue

## Acknowledgements

In no particular order.

| Project                                                        | Thanks for                                              |
| -------------------------------------------------------------- | ------------------------------------------------------- |
| [XiHan.Framework](https://github.com/XiHanFun/XiHan.Framework) | Being the backend foundation of this project            |
| [XiHan.UI](https://github.com/XiHanFun/XiHan.UI)               | Being the frontend component foundation of this project |
| [NaiveUI](https://github.com/tusen-ai/naive-ui)                | The frontend component library before v4.0.0            |
| [Blog.Core](https://github.com/anjoy8/Blog.Core)               | Inspiring parts of the backend architecture             |
| [ Admin.Core.ZR](https://gitee.com/izory/ZrAdminNetCore)       | Inspiring parts of the backend features                 |
| [YuebonCore](https://gitee.com/yuebon/YuebonNetCore)           | Inspiring parts of the backend features                 |
| [VbenAdmin](https://github.com/vbenjs/vue-vben-admin)          | Inspiring parts of the frontend architecture and visuals |
| [SoybeanAdmin](https://github.com/soybeanjs/soybean-admin)     | Inspiring parts of the frontend visuals                 |
| [LitheAdmin](https://github.com/tenianon/lithe-admin)          | Inspiring parts of the frontend visuals                 |
| Other third-party dependencies                                 | Being the foundation this project is built upon         |


## Support & Sponsorship

If this project helps your work, feel free to buy the author a coffee.

Official sponsorship page: https://docs.xihanfun.com/cosmos/sponsor


## License

Copyright (c) 2021-Present XiHanFun and contributors.

Released under the MIT License — see [License](./LICENSE).

The XiHan.BasicApp logo, name, interface visual design and original visual expression belong to the author; third-party dependencies and services are governed by their own licenses and terms.

This project is provided for study and reference; the author assumes no liability for any use of the software.
