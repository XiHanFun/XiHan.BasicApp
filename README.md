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

<table>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/dashboard.png"><img src="./assets/preview/dashboard.png" alt="Dashboard" /></a><br/>Dashboard</td>
    <td align="center" width="50%"><a href="./assets/preview/user.png"><img src="./assets/preview/user.png" alt="User management" /></a><br/>User management</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-trace.png"><img src="./assets/preview/log-trace.png" alt="Log tracing" /></a><br/>Log tracing</td>
    <td align="center" width="50%"><a href="./assets/preview/printing.png"><img src="./assets/preview/printing.png" alt="Print templates" /></a><br/>Print templates</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/control-center.png"><img src="./assets/preview/control-center.png" alt="Control center" /></a><br/>Control center</td>
    <td align="center" width="50%"><a href="./assets/preview/login.png"><img src="./assets/preview/login.png" alt="Authentication" /></a><br/>Authentication</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/profile.png"><img src="./assets/preview/profile.png" alt="Personal center" /></a><br/>Personal center</td>
    <td align="center" width="50%"><a href="./assets/preview/profile-security.png"><img src="./assets/preview/profile-security.png" alt="Personal security settings" /></a><br/>Personal security settings</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/online-user.png"><img src="./assets/preview/online-user.png" alt="Online users" /></a><br/>Online users</td>
    <td align="center" width="50%"><a href="./assets/preview/role.png"><img src="./assets/preview/role.png" alt="Role management" /></a><br/>Role management</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/org.png"><img src="./assets/preview/org.png" alt="Organizations" /></a><br/>Organizations</td>
    <td align="center" width="50%"><a href="./assets/preview/position.png"><img src="./assets/preview/position.png" alt="Positions" /></a><br/>Positions</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/permission.png"><img src="./assets/preview/permission.png" alt="Permissions" /></a><br/>Permissions</td>
    <td align="center" width="50%"><a href="./assets/preview/menu.png"><img src="./assets/preview/menu.png" alt="Menus" /></a><br/>Menus</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/field-security.png"><img src="./assets/preview/field-security.png" alt="Field security" /></a><br/>Field security</td>
    <td align="center" width="50%"><a href="./assets/preview/authorization.png"><img src="./assets/preview/authorization.png" alt="Permission requests and delegation" /></a><br/>Permission requests and delegation</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/review.png"><img src="./assets/preview/review.png" alt="Approval center" /></a><br/>Approval center</td>
    <td align="center" width="50%"><a href="./assets/preview/constraint.png"><img src="./assets/preview/constraint.png" alt="Approval constraints" /></a><br/>Approval constraints</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/tenant.png"><img src="./assets/preview/tenant.png" alt="Tenant management" /></a><br/>Tenant management</td>
    <td align="center" width="50%"><a href="./assets/preview/tenant-members.png"><img src="./assets/preview/tenant-members.png" alt="Tenant members and support access" /></a><br/>Tenant members and support access</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/edition.png"><img src="./assets/preview/edition.png" alt="Tenant editions" /></a><br/>Tenant editions</td>
    <td align="center" width="50%"><a href="./assets/preview/subscription.png"><img src="./assets/preview/subscription.png" alt="My subscription" /></a><br/>My subscription</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/notification.png"><img src="./assets/preview/notification.png" alt="Announcements" /></a><br/>Announcements</td>
    <td align="center" width="50%"><a href="./assets/preview/inbox.png"><img src="./assets/preview/inbox.png" alt="My inbox" /></a><br/>My inbox</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/message-template.png"><img src="./assets/preview/message-template.png" alt="Message templates" /></a><br/>Message templates</td>
    <td align="center" width="50%"><a href="./assets/preview/message-record.png"><img src="./assets/preview/message-record.png" alt="Email and SMS records" /></a><br/>Email and SMS records</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/chat.png"><img src="./assets/preview/chat.png" alt="Online chat" /></a><br/>Online chat</td>
    <td align="center" width="50%"><a href="./assets/preview/chat-audit.png"><img src="./assets/preview/chat-audit.png" alt="Chat audit" /></a><br/>Chat audit</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/file-library.png"><img src="./assets/preview/file-library.png" alt="Files" /></a><br/>Files</td>
    <td align="center" width="50%"><a href="./assets/preview/file-storage.png"><img src="./assets/preview/file-storage.png" alt="Storage configuration" /></a><br/>Storage configuration</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/export-center.png"><img src="./assets/preview/export-center.png" alt="Export center" /></a><br/>Export center</td>
    <td align="center" width="50%"><a href="./assets/preview/dict.png"><img src="./assets/preview/dict.png" alt="Dictionaries" /></a><br/>Dictionaries</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/config.png"><img src="./assets/preview/config.png" alt="Parameters" /></a><br/>Parameters</td>
    <td align="center" width="50%"><a href="./assets/preview/numbering.png"><img src="./assets/preview/numbering.png" alt="Business numbering" /></a><br/>Business numbering</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/job.png"><img src="./assets/preview/job.png" alt="Scheduled jobs" /></a><br/>Scheduled jobs</td>
    <td align="center" width="50%"><a href="./assets/preview/cache.png"><img src="./assets/preview/cache.png" alt="Cache management" /></a><br/>Cache management</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/server.png"><img src="./assets/preview/server.png" alt="Server monitoring" /></a><br/>Server monitoring</td>
    <td align="center" width="50%"><a href="./assets/preview/version.png"><img src="./assets/preview/version.png" alt="Version management" /></a><br/>Version management</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/email-config.png"><img src="./assets/preview/email-config.png" alt="Email configuration" /></a><br/>Email configuration</td>
    <td align="center" width="50%"><a href="./assets/preview/sms-config.png"><img src="./assets/preview/sms-config.png" alt="SMS configuration" /></a><br/>SMS configuration</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/bot-config.png"><img src="./assets/preview/bot-config.png" alt="Webhook bots" /></a><br/>Webhook bots</td>
    <td align="center" width="50%"><a href="./assets/preview/telegram-bot.png"><img src="./assets/preview/telegram-bot.png" alt="Telegram bots" /></a><br/>Telegram bots</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/openapi-app.png"><img src="./assets/preview/openapi-app.png" alt="Application management" /></a><br/>Application management</td>
    <td align="center" width="50%"><a href="./assets/preview/openapi-credentials.png"><img src="./assets/preview/openapi-credentials.png" alt="OpenAPI credentials" /></a><br/>OpenAPI credentials</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-access.png"><img src="./assets/preview/log-access.png" alt="Access logs" /></a><br/>Access logs</td>
    <td align="center" width="50%"><a href="./assets/preview/log-api.png"><img src="./assets/preview/log-api.png" alt="OpenAPI logs" /></a><br/>OpenAPI logs</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-operation.png"><img src="./assets/preview/log-operation.png" alt="Operation logs" /></a><br/>Operation logs</td>
    <td align="center" width="50%"><a href="./assets/preview/log-login.png"><img src="./assets/preview/log-login.png" alt="Login logs" /></a><br/>Login logs</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-exception.png"><img src="./assets/preview/log-exception.png" alt="Exception logs" /></a><br/>Exception logs</td>
    <td align="center" width="50%"><a href="./assets/preview/log-diff.png"><img src="./assets/preview/log-diff.png" alt="Data change logs" /></a><br/>Data change logs</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-permission.png"><img src="./assets/preview/log-permission.png" alt="Permission change logs" /></a><br/>Permission change logs</td>
    <td align="center" width="50%"><a href="./assets/preview/log-trace-timeline.png"><img src="./assets/preview/log-trace-timeline.png" alt="Correlated log timeline" /></a><br/>Correlated log timeline</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/log-migration.png"><img src="./assets/preview/log-migration.png" alt="Migration history" /></a><br/>Migration history</td>
    <td align="center" width="50%"><a href="./assets/preview/codegen.png"><img src="./assets/preview/codegen.png" alt="Code generation" /></a><br/>Code generation</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/ai-provider.png"><img src="./assets/preview/ai-provider.png" alt="AI providers" /></a><br/>AI providers</td>
    <td align="center" width="50%"><a href="./assets/preview/ai-prompt.png"><img src="./assets/preview/ai-prompt.png" alt="AI prompts" /></a><br/>AI prompts</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/knowledge.png"><img src="./assets/preview/knowledge.png" alt="Knowledge base" /></a><br/>Knowledge base</td>
    <td align="center" width="50%"><a href="./assets/preview/ai-assistant.png"><img src="./assets/preview/ai-assistant.png" alt="AI assistants" /></a><br/>AI assistants</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/workflow-definition.png"><img src="./assets/preview/workflow-definition.png" alt="Workflow definitions" /></a><br/>Workflow definitions</td>
    <td align="center" width="50%"><a href="./assets/preview/workflow-json.png"><img src="./assets/preview/workflow-json.png" alt="Workflow JSON editor" /></a><br/>Workflow JSON editor</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/workflow-instance.png"><img src="./assets/preview/workflow-instance.png" alt="Workflow instances" /></a><br/>Workflow instances</td>
    <td align="center" width="50%"><a href="./assets/preview/workflow-todo.png"><img src="./assets/preview/workflow-todo.png" alt="My workflow tasks" /></a><br/>My workflow tasks</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/preferences.png"><img src="./assets/preview/preferences.png" alt="Preferences" /></a><br/>Preferences</td>
    <td align="center" width="50%"><a href="./assets/preview/schema-page.png"><img src="./assets/preview/schema-page.png" alt="Advanced lists" /></a><br/>Advanced lists</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/editors.png"><img src="./assets/preview/editors.png" alt="Content editors" /></a><br/>Content editors</td>
    <td align="center" width="50%"><a href="./assets/preview/editor-json.png"><img src="./assets/preview/editor-json.png" alt="JSON editor" /></a><br/>JSON editor</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a href="./assets/preview/editor-rich-text.png"><img src="./assets/preview/editor-rich-text.png" alt="Rich text editor" /></a><br/>Rich text editor</td>
    <td align="center" width="50%"><a href="./assets/preview/navigation.png"><img src="./assets/preview/navigation.png" alt="Global navigation" /></a><br/>Global navigation</td>
  </tr>
</table>

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
└── assets/                  # Branding and README assets
    └── preview/             # Feature screenshots and manifest
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
