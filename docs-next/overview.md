# 系统概述

## 定位

6.0 是一个**单体部署、按能力包模块化**的中后台：一个宿主进程（`XiHan.BasicApp.Host`）装配全部能力包，对外提供 HTTP API、内部 gRPC、MCP 与 SignalR Hub；前端是独立部署的单页应用。多租户、权限与审计是贯穿所有包的维度，不是某个模块的附属功能。

## 架构风格

```text
Ring 6  XiHan.BasicApp.Host             装配：Full Profile + 显式 Provider + 能力包；普通端点（文件、OAuth、Hub、Webhook）
        XiHan.BasicApp.<能力包>          Tier 0–3
Ring 5  Protocols.Http / Grpc / Mcp      一份操作登记，三种协议
Ring 4  Hosting.AspNetCore + Hosting.CommandLine（contracts export / schema upgrade / seed）
Ring 3  Persistence.SqlSugar、Security.Jwt、MultiTenancy.Database、Security.Database、
        Auditing.Database、Scheduling.Database …
Ring 1  Kernel（操作、管道、Result / Error、消息目录）    Ring 0  Primitives
```

能力包之间分四层，只能向下依赖、同层不互相引用，跨包协作经 `Contracts` 里的接口（如 `IWorkbenchWidgetProvider`、`ITenantQuota`、`IRecipientSource`）：

```text
Tier 0 基座       Contracts → Runtime
Tier 1 身份租户    Identity → Tenancy → Organization → Access
Tier 2 平台能力    Settings | Notification | Files | Auditing | Maintenance | OpenPlatform | Realtime
Tier 3 可选模块    Workbench（已落地）| Workflow | Printing | CodeGeneration | Chat | AI（规划中）
Host              XiHan.BasicApp.Host
```

层级由 `XiHan.BasicApp.Architecture.Tests` 的 `TierDependencyTests` 守护。

## 一个请求怎么走

```text
浏览器 ──HTTP──> Kestrel ──> 维护模式 / 认证 / 开放接口原文缓冲 ──> UseXiHan()
                                                                    │
            内核管道：Ingress → Observe → Resilience → Authenticate → Tenant → Authorize
                      → Validate → Idempotency → Transaction → Handle → Egress
                                                                    │
                                        [Operation] HandleAsync → Result<TResponse>
                                                                    │
            成功：200 / 201 + 响应 DTO        失败：application/problem+json（code、detail、traceId、errors）
```

各阶段里 BasicApp 贡献了哪些中间件（会话门禁、访问策略、权限条件、操作人、审计日志），见[请求生命周期](./backend/request-lifecycle)。

## 能力全景

| 包 | Tier | 主要内容 | 状态 |
| --- | --- | --- | --- |
| `Contracts` | 0 | 实体基类与审计协议、分页 / 批量结果、权限描述、页面登记、跨包接口、种子号段 | 已落地 |
| `Runtime` | 0 | SqlSugar 回调、`TenantScopedQuery`、操作人、数据保护、表结构升级器、种子执行器、维护模式 | 已落地 |
| `Identity` | 1 | 登录（密码、验证码、第三方、双因素）、令牌与会话、锁屏、用户管理、在线用户、个人中心、模仿登录、用户设置 | 已落地 |
| `Tenancy` | 1 | 租户开通三步、独立库、套餐与配额、成员与所有权转移、我的订阅、切换租户 | 已落地 |
| `Organization` | 1 | 部门（闭包表）、岗位、部门成员 | 已落地 |
| `Access` | 1 | 角色与授权、Deny、继承、委托、数据范围、字段安全、权限条件、访问策略、约束规则、菜单 | 已落地 |
| `Settings` | 2 | 参数配置、字典、业务编号 | 已落地 |
| `Files` | 2 | 存储配置、文件与副本、秒传、分片上传、签名地址、导出中心、导入历史 | 已落地 |
| `Notification` | 2 | 通知公告、收件箱、消息模板、邮件 / 短信、群机器人、Telegram 机器人 | 已落地 |
| `Auditing` | 2 | 八类日志与链路追踪的查询面、日志写入队列、保留清理 | 已落地 |
| `Maintenance` | 2 | 任务调度、缓存管理、服务监控、版本与升级记录 | 已落地 |
| `OpenPlatform` | 2 | 签名开放接口、OAuth 2.0 / OIDC 服务端、OAuth 应用、我的应用、授权同意 | 已落地 |
| `Realtime` | 2 | 通知 Hub、连接与在线状态、按受众推送 | 已落地 |
| `Workbench` | 3 | 工作台小组件汇总（各包登记的指标、图表与列表） | 已落地 |
| `Workflow` · `Printing` · `CodeGeneration` · `Chat` | 3 | 工作流与审批中心、打印模板、代码生成、在线聊天 | 规划中（N3） |
| `AI` | 3 | AI 提供商、提示词、知识库、助手 | 规划中（N4） |

## 技术栈

### 后端

| 项 | 选型 |
| --- | --- |
| 运行时 | .NET 10（`net10.0`，警告即错误、Nullable、代码风格在构建期执行） |
| 框架 | XiHan.Framework 5（`Profiles.Full`，ProjectReference 锁定提交） |
| 数据 | SqlSugar（框架 `Persistence.SqlSugar` 方式一），PostgreSQL 为缺省与 CI 口径 |
| 认证 | 框架 `Security.Jwt` 校验 HS256；签发、刷新、会话由 Identity 负责 |
| 实时 | ASP.NET Core SignalR（`/hubs/notification`） |
| 邮件 / 模板 | MailKit、Scriban |
| 接口文档 | 框架 OpenAPI 生成器 + Scalar.AspNetCore |
| 测试 | xUnit v3（Microsoft.Testing.Platform）、TestServer + SQLite / PostgreSQL |

### 前端

| 项 | 选型 |
| --- | --- |
| 基础 | Vue 3.5、Vite、TypeScript、Pinia、vue-router（hash 缺省）、vue-i18n |
| UI | XiHan.UI（`@xihan-ui/vue`、`headless`、`core`、`styles`、`tokens`、`icons` 等）、Tailwind CSS 4 |
| 契约 | `scripts/gen-contracts.mjs` 生成 `packages/contracts/*` |
| 实时 | `@microsoft/signalr` |
| 时间 | dayjs（utc + timezone，按用户时区显示） |
| 工具链 | oxlint + eslint（antfu）、Vitest + jsdom；Node 24+、pnpm 11+，无锁文件 |

## 文档怎么读

- 先读[快速开始](./getting-started)把系统跑起来，再读[目录结构](./project-structure)建立代码地图。
- 后端从[架构总览](./backend/introduction)与[开发流程](./backend/development)开始；前端从[框架简介](./frontend/introduction)开始。
- 侧栏里标注「规划中」的页面描述的是方案设计，不是已有功能。

## 下一步

- [开发环境](./dev-environment) · [快速开始](./getting-started) · [功能清单](./features)
