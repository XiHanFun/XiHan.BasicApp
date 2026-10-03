<div align="center">

<img src="./assets/banner.png" alt="XiHan.BasicApp" />

<h1>XiHan.BasicApp</h1>

<p><b>A beautifully crafted general-purpose admin kernel built on XiHan.Framework and XiHan.UI</b></p>

<p>A .NET backend on <a href="https://github.com/XiHanFun/XiHan.Framework">XiHan.Framework</a>, a Vue frontend on <a href="https://github.com/XiHanFun/XiHan.UI">XiHan.UI</a><br/>Multi-tenancy · RBAC with data scopes and field masking · Code generation · Realtime</p>

<p><b>English</b> | <a href="./README_cn.md">简体中文</a></p>

<p>
  <a href="https://github.com/XiHanFun/XiHan.BasicApp/stargazers"><img alt="GitHub Stars" src="https://img.shields.io/github/stars/XiHanFun/XiHan.BasicApp?style=flat-square&logo=github&label=Stars&color=1f6feb" /></a>
  <a href="https://gitee.com/XiHanFun/XiHan.BasicApp"><img alt="Gitee Stars" src="https://gitee.com/XiHanFun/XiHan.BasicApp/badge/star.svg" /></a>
  <a href="https://gitcode.com/XiHanFun/XiHan.BasicApp"><img alt="GitCode Stars" src="https://gitcode.com/XiHanFun/XiHan.BasicApp/star/badge.svg" /></a>
</p>


<p>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-512BD4?style=flat-square&logo=dotnet&logoColor=white" />
  <a href="https://github.com/XiHanFun/XiHan.Framework"><img alt="XiHan.Framework" src="https://img.shields.io/badge/XiHan.Framework-6f42c1?style=flat-square" /></a>
  <img alt="Vue" src="https://img.shields.io/badge/Vue-4FC08D?style=flat-square&logo=vuedotjs&logoColor=white" />
  <img alt="TypeScript" src="https://img.shields.io/badge/TypeScript-3178C6?style=flat-square&logo=typescript&logoColor=white" />
  <img alt="Vite" src="https://img.shields.io/badge/Vite-646CFF?style=flat-square&logo=vite&logoColor=white" />
  <img alt="Tailwind CSS" src="https://img.shields.io/badge/Tailwind_CSS-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white" />
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


<img src="./assets/preview/login.png" alt="Sign in" />

</div>

## Introduction

XiHan.BasicApp is a decoupled frontend/backend system. The backend follows DDD layering — writes go through application services, reads through query services — and application services are exposed directly as REST endpoints by the dynamic API convention. The frontend is Vue + TypeScript + XiHan.UI, and the backend .NET + XiHan.Framework. Identity, permissions, tenancy and auditing are built in, so it works both as the starting point for an admin project and as a reference for full-stack .NET + Vue practice. XiHan.BasicApp is the basic application of the XiHanFun open-source ecosystem.

## Documentation

| Destination | Contents |
| --- | --- |
| [Documentation site](https://basicapp.docs.xihanfun.com) | Full guides and topic-by-topic documentation |
| [Backend engineering notes](./backend/README.md) | Structure, project catalog, endpoint exposure, dependencies, local development |
| [Frontend engineering notes](./frontend/README.md) | Stack, monorepo structure, architectural constraints, local development |

## Preview

Captured locally on October 4, 2026, as lossless 3086 × 1866 PNG images. Click an image to open the original. Dashboard business charts use built-in sample data; empty states and unsaved forms are identified beside each image.

#### Dashboard

[![Dashboard](./assets/preview/dashboard.png)](./assets/preview/dashboard.png)

#### User management

[![User management](./assets/preview/user.png)](./assets/preview/user.png)

#### Log tracing

[![Log tracing](./assets/preview/log-trace.png)](./assets/preview/log-trace.png)

#### Print templates

Unsaved configuration or design preview.

[![Print templates](./assets/preview/printing.png)](./assets/preview/printing.png)

<details>
<summary>Workspace and identity（14）</summary>

#### Control center

[![Control center](./assets/preview/control-center.png)](./assets/preview/control-center.png)

#### Authentication

[![Authentication](./assets/preview/login.png)](./assets/preview/login.png)

#### Personal center

[![Personal center](./assets/preview/profile.png)](./assets/preview/profile.png)

#### Personal security settings

[![Personal security settings](./assets/preview/profile-security.png)](./assets/preview/profile-security.png)

#### Online users

[![Online users](./assets/preview/online-user.png)](./assets/preview/online-user.png)

#### Role management

[![Role management](./assets/preview/role.png)](./assets/preview/role.png)

#### Organizations

[![Organizations](./assets/preview/org.png)](./assets/preview/org.png)

#### Positions

[![Positions](./assets/preview/position.png)](./assets/preview/position.png)

#### Permissions

[![Permissions](./assets/preview/permission.png)](./assets/preview/permission.png)

#### Menus

[![Menus](./assets/preview/menu.png)](./assets/preview/menu.png)

#### Field security

Unsaved configuration or design preview.

[![Field security](./assets/preview/field-security.png)](./assets/preview/field-security.png)

#### Permission requests and delegation

Unsaved configuration or design preview.

[![Permission requests and delegation](./assets/preview/authorization.png)](./assets/preview/authorization.png)

#### Approval center

No records in the current demo environment.

[![Approval center](./assets/preview/review.png)](./assets/preview/review.png)

#### Approval constraints

Unsaved configuration or design preview.

[![Approval constraints](./assets/preview/constraint.png)](./assets/preview/constraint.png)

</details>

<details>
<summary>Tenants and messaging（10）</summary>

#### Tenant management

[![Tenant management](./assets/preview/tenant.png)](./assets/preview/tenant.png)

#### Tenant members and support access

[![Tenant members and support access](./assets/preview/tenant-members.png)](./assets/preview/tenant-members.png)

#### Tenant editions

[![Tenant editions](./assets/preview/edition.png)](./assets/preview/edition.png)

#### My subscription

[![My subscription](./assets/preview/subscription.png)](./assets/preview/subscription.png)

#### Announcements

[![Announcements](./assets/preview/notification.png)](./assets/preview/notification.png)

#### My inbox

[![My inbox](./assets/preview/inbox.png)](./assets/preview/inbox.png)

#### Message templates

[![Message templates](./assets/preview/message-template.png)](./assets/preview/message-template.png)

#### Email and SMS records

No records in the current demo environment.

[![Email and SMS records](./assets/preview/message-record.png)](./assets/preview/message-record.png)

#### Online chat

Unsaved configuration or design preview.

[![Online chat](./assets/preview/chat.png)](./assets/preview/chat.png)

#### Chat audit

No records in the current demo environment.

[![Chat audit](./assets/preview/chat-audit.png)](./assets/preview/chat-audit.png)

</details>

<details>
<summary>Files and system management（16）</summary>

#### Files

Unsaved configuration or design preview.

[![Files](./assets/preview/file-library.png)](./assets/preview/file-library.png)

#### Storage configuration

Unsaved configuration or design preview.

[![Storage configuration](./assets/preview/file-storage.png)](./assets/preview/file-storage.png)

#### Export center

No records in the current demo environment.

[![Export center](./assets/preview/export-center.png)](./assets/preview/export-center.png)

#### Dictionaries

[![Dictionaries](./assets/preview/dict.png)](./assets/preview/dict.png)

#### Parameters

[![Parameters](./assets/preview/config.png)](./assets/preview/config.png)

#### Business numbering

Unsaved configuration or design preview.

[![Business numbering](./assets/preview/numbering.png)](./assets/preview/numbering.png)

#### Scheduled jobs

[![Scheduled jobs](./assets/preview/job.png)](./assets/preview/job.png)

#### Cache management

[![Cache management](./assets/preview/cache.png)](./assets/preview/cache.png)

#### Server monitoring

[![Server monitoring](./assets/preview/server.png)](./assets/preview/server.png)

#### Version management

[![Version management](./assets/preview/version.png)](./assets/preview/version.png)

#### Email configuration

Unsaved configuration or design preview.

[![Email configuration](./assets/preview/email-config.png)](./assets/preview/email-config.png)

#### SMS configuration

Unsaved configuration or design preview.

[![SMS configuration](./assets/preview/sms-config.png)](./assets/preview/sms-config.png)

#### Webhook bots

Unsaved configuration or design preview.

[![Webhook bots](./assets/preview/bot-config.png)](./assets/preview/bot-config.png)

#### Telegram bots

Unsaved configuration or design preview.

[![Telegram bots](./assets/preview/telegram-bot.png)](./assets/preview/telegram-bot.png)

#### Application management

[![Application management](./assets/preview/openapi-app.png)](./assets/preview/openapi-app.png)

#### OpenAPI credentials

No records in the current demo environment.

[![OpenAPI credentials](./assets/preview/openapi-credentials.png)](./assets/preview/openapi-credentials.png)

</details>

<details>
<summary>Logs and audit（9）</summary>

#### Access logs

[![Access logs](./assets/preview/log-access.png)](./assets/preview/log-access.png)

#### OpenAPI logs

No records in the current demo environment.

[![OpenAPI logs](./assets/preview/log-api.png)](./assets/preview/log-api.png)

#### Operation logs

[![Operation logs](./assets/preview/log-operation.png)](./assets/preview/log-operation.png)

#### Login logs

[![Login logs](./assets/preview/log-login.png)](./assets/preview/log-login.png)

#### Exception logs

[![Exception logs](./assets/preview/log-exception.png)](./assets/preview/log-exception.png)

#### Data change logs

[![Data change logs](./assets/preview/log-diff.png)](./assets/preview/log-diff.png)

#### Permission change logs

[![Permission change logs](./assets/preview/log-permission.png)](./assets/preview/log-permission.png)

#### Correlated log timeline

[![Correlated log timeline](./assets/preview/log-trace-timeline.png)](./assets/preview/log-trace-timeline.png)

#### Migration history

[![Migration history](./assets/preview/log-migration.png)](./assets/preview/log-migration.png)

</details>

<details>
<summary>Development and optional modules（9）</summary>

#### Code generation

[![Code generation](./assets/preview/codegen.png)](./assets/preview/codegen.png)

#### AI providers

Unsaved configuration or design preview.

[![AI providers](./assets/preview/ai-provider.png)](./assets/preview/ai-provider.png)

#### AI prompts

Unsaved configuration or design preview.

[![AI prompts](./assets/preview/ai-prompt.png)](./assets/preview/ai-prompt.png)

#### Knowledge base

Unsaved configuration or design preview.

[![Knowledge base](./assets/preview/knowledge.png)](./assets/preview/knowledge.png)

#### AI assistants

Unsaved configuration or design preview.

[![AI assistants](./assets/preview/ai-assistant.png)](./assets/preview/ai-assistant.png)

#### Workflow definitions

Unsaved configuration or design preview.

[![Workflow definitions](./assets/preview/workflow-definition.png)](./assets/preview/workflow-definition.png)

#### Workflow JSON editor

Unsaved configuration or design preview.

[![Workflow JSON editor](./assets/preview/workflow-json.png)](./assets/preview/workflow-json.png)

#### Workflow instances

No records in the current demo environment.

[![Workflow instances](./assets/preview/workflow-instance.png)](./assets/preview/workflow-instance.png)

#### My workflow tasks

No records in the current demo environment.

[![My workflow tasks](./assets/preview/workflow-todo.png)](./assets/preview/workflow-todo.png)

</details>

<details>
<summary>Shared user experience（6）</summary>

#### Preferences

[![Preferences](./assets/preview/preferences.png)](./assets/preview/preferences.png)

#### Advanced lists

[![Advanced lists](./assets/preview/schema-page.png)](./assets/preview/schema-page.png)

#### Content editors

[![Content editors](./assets/preview/editors.png)](./assets/preview/editors.png)

#### JSON editor

[![JSON editor](./assets/preview/editor-json.png)](./assets/preview/editor-json.png)

#### Rich text editor

[![Rich text editor](./assets/preview/editor-rich-text.png)](./assets/preview/editor-rich-text.png)

#### Global navigation

[![Global navigation](./assets/preview/navigation.png)](./assets/preview/navigation.png)

</details>

## Features

### Workspace and identity

- **Dashboard**: Customize widgets, charts, announcements and pending work, and save a personal dashboard layout.
- **Control center**: Choose a tenant or enter platform administration from one workspace selector.
- **Authentication**: Sign in with a password, phone or email codes, external providers and two-factor authentication, with CAPTCHA, throttling and account lockout.
- **Personal center**: Maintain profile and security settings, linked accounts, devices, notification preferences, tenant memberships and usage statistics.
- **User management**: Search and maintain users, status, roles, departments, direct grants and data scopes; lock accounts, reset passwords, impersonate users and revoke sessions.
- **Online users**: Inspect active sessions, clients and live connections, and search users or revoke individual sessions.
- **Role management**: Manage roles, inheritance and members, batch membership changes, menu and operation grants, and data scopes.
- **Organizations**: Manage department trees, leaders, child departments and members, including positions, employee numbers, job levels and join dates.
- **Positions**: Maintain position names, codes, order and enabled state for department membership assignments.
- **Permissions**: Manage permission codes, modules, resources, operations, API paths, HTTP methods, platform or tenant scope and audit requirements.
- **Menus**: Maintain directory, menu and button trees with component paths, icons, order, visibility, caching, external links and permission bindings.
- **Field security**: Configure field read, write and masking policies for entities and authorization targets.
- **Permission requests and delegation**: Approve, reject or withdraw access requests, and delegate roles or permissions within a time window with revocation.
- **Approval center**: Inspect approval items, status and processing history, and approve, reject or withdraw requests.
- **Approval constraints**: Configure separation-of-duty, exclusion, cardinality and conditional rules, their targets and violation handling.

### Tenants and messaging

- **Tenant management**: Manage status, editions, expiration and quotas, initialize databases and administrators, and manage memberships, support access and ownership transfer.
- **Tenant editions**: Configure prices, billing periods, user and storage quotas, and permission allowlists for tenant features.
- **My subscription**: View the current tenant edition, subscription period, user and storage usage, quotas and entitlements.
- **Announcements**: Publish targeted notices to users, roles or departments, configure presentation and mandatory reading, and inspect read statistics.
- **My inbox**: Read notification details, filter pending messages, and mark individual or all messages as read or confirmed.
- **Message templates**: Maintain variable-based email, SMS, in-app and bot templates, including tenant overrides.
- **Email and SMS records**: Inspect recipients, content and delivery results, and resend failed email or SMS messages.
- **Online chat**: Use direct, group, department and AI assistant conversations with real-time messages and unread state.
- **Chat audit**: Search conversations and messages, inspect members and message details, and review conversation content.

### Files and system management

- **Files**: Upload, search, download and preview files, maintain metadata and archive state, and manage storage copies and the primary location.
- **Storage configuration**: Configure local, S3, OSS, COS and MinIO storage channels, status and a default provider.
- **Export center**: Track asynchronous exports, download CSV or XLSX results, cancel pending tasks and delete records.
- **Dictionaries**: Maintain linked dictionary categories and items, codes, values, defaults, order and status for shared search and form options.
- **Parameters**: Maintain platform and tenant parameters, search groups and status, and inspect or edit configuration values.
- **Business numbering**: Configure prefixes, dates, sequence width, reset periods and time zones, preview formats, perform guarded resets and inspect allocation history.
- **Scheduled jobs**: Configure Cron and interval jobs, status, immediate runs and retry policies, and inspect execution logs.
- **Cache management**: Search cache keys or patterns, inspect grouped keys and values, edit values and clear individual or multiple entries.
- **Server monitoring**: Inspect CPU, memory, disks, network, GPU, motherboard and runtime information.
- **Version management**: Inspect the current release, version notes, database migration information and upgrade state.
- **Email configuration**: Maintain email channels and server settings, enabled state and the default configuration.
- **SMS configuration**: Maintain SMS providers, delivery settings, enabled state and a default channel.
- **Webhook bots**: Configure DingTalk, Feishu and WeCom Webhook channels, status and a default notification bot.
- **Telegram bots**: Maintain connection settings and status for multiple Telegram Bot instances.
- **Application management**: Register OAuth2 / OIDC clients with client types, grant types, redirect URIs, status and secrets.
- **OpenAPI credentials**: Manage personal OpenAPI credentials and rotate keys for signed API calls.

### Logs and audit

- **Access logs**: Inspect request paths, methods, status, duration and client details, and open related traces.
- **OpenAPI logs**: Inspect OpenAPI calls, signature authorization results, request and response details, and related traces.
- **Operation logs**: Inspect business operations, outcomes, duration, descriptions, client details and related traces.
- **Login logs**: Inspect successful and failed logins, logout and impersonation events, client and IP details.
- **Exception logs**: Search exception types, messages and locations, and inspect stack traces and related requests.
- **Data change logs**: Inspect entity creation, updates, deletion and restoration with before-and-after field differences and traces.
- **Permission change logs**: Inspect role, user and permission grant changes, affected targets and related traces.
- **Log tracing**: Correlate log types by TraceId, user, session or IP, then analyze timelines, flow diagrams and time distributions.
- **Migration history**: Inspect database upgrade script history, versions, execution status, duration and errors.

### Development and optional modules

- **Code generation**: Manage data sources, tables, fields and templates; preview and generate single-table, tree and master-detail stacks, download output and inspect history.
- **AI providers**: Configure endpoints and models, securely store keys, test connections and choose a default provider.
- **AI prompts**: Maintain prompt codes, content and status for reusable conversation and business templates.
- **Knowledge base**: Ingest documents, build or rebuild vector indexes, and query tenant-isolated knowledge with source references.
- **AI assistants**: Configure assistant identities, models and prompts, choose a default and connect assistants to chat.
- **Workflow definitions**: Design nodes and edges, maintain draft, published, disabled and archived definitions, manage versions and start workflows.
- **Workflow instances**: Inspect execution and nodes, suspend, resume, cancel, terminate or retry instances, and send workflow signals.
- **My workflow tasks**: Process human tasks with approval, rejection, transfer and additional signers, and track completion.
- **Print templates**: Design text, tables, images and barcodes with paper and data-source settings, sample data, zoom, JSON inspection, preview and printing.

### Shared user experience

- **Preferences**: Configure themes, colors, layouts, density, watermarks and interaction preferences with cloud synchronization.
- **Advanced lists**: Use combined search, saved views, column settings, multi-column sorting, density, trees, quick previews and exports.
- **Content editors**: Edit rich text, Markdown, code and JSON, and configure Cron expressions visually.
- **Global navigation**: Use tabs, favorites, global search, notification and task feedback, languages, time zones and screen locking.

## Tech Stack

Backend: .NET with XiHan.Framework (SqlSugar, Redis, SignalR, Serilog and Scalar all arrive through the framework). Frontend: Vue + TypeScript + Vite + XiHan.UI + Pinia + Tailwind CSS.

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
| `XiHan.BasicApp.Web.Core` | Web base composing the framework web modules, provides the maintenance-mode middleware | No |
| `XiHan.BasicApp.Saas` | Platform governance: users / roles / permissions / menus / departments / tenants / settings / dictionaries / files / notifications / approvals / logs / jobs | No |
| `XiHan.BasicApp.CodeGeneration` | Code generation: data sources / schema import / template configuration / full-stack output | Yes |
| `XiHan.BasicApp.AI` | AI: providers and key custody / prompt library / knowledge-base RAG / skills as MCP tools / chat assistant | Yes (the assistant bridge depends on Chat) |
| `XiHan.BasicApp.Workflow` | Workflow: definitions / instances / todos — persistence and APIs over the framework engine | Yes |
| `XiHan.BasicApp.Printing` | Print templates: visual design / tenant and platform scopes / resolution by code | Yes |
| `XiHan.BasicApp.Chat` | Chat: direct / group / department / assistant conversations, realtime delivery, compliance auditing | Yes (also handle the AI assistant bridge) |
| `XiHan.BasicApp.WebHost` | Startup host composing every module | — |

```text
XiHan.BasicApp/
├── backend/                 # backend (.NET)
│   ├── src/
│   │   ├── framework/       #   Core / Web.Core base capabilities
│   │   ├── modules/         #   Saas + optional modules (CodeGen/AI/Workflow/Printing/Chat)
│   │   └── main/            #   WebHost startup entry
│   ├── props/               #   shared MSBuild properties
│   ├── scripts/             #   version bump and cleanup scripts
│   └── test/                #   test projects
├── frontend/                # frontend (Vue + XiHan.UI)
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

> `@xihan-ui/*` comes from the published npm releases, so cloning this repository on its own and running `pnpm install` just works — no sibling `XiHan.UI` checkout required. To debug against the component library sources, temporarily add an `overrides` block in `pnpm-workspace.yaml` pointing at `link:../../XiHan.UI/ui/packages/*`, then remove it; do not commit it.

### Default Account

The initial super administrator is `superadmin` with the password `SuperAdmin@123` (written by the seed; the account is flagged as needing its own password change). In production, change it right after the first sign-in and consider turning on forced password change in the "Password settings" parameter.

The development configuration turns on demo data (`Saas:Seed:EnableDemoData`): demo tenants and accounts covering every edition, tenant status, member type, data scope and account state, all with the password `Demo@123`. See [Introduction: seed data](docs/backend/introduction.md#演示数据) for the list.

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
