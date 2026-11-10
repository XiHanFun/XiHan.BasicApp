# 系统概述

本页说明部署形态、分层、请求路径与技术栈，适用于需要建立全局认识的开发者与架构评审者。

## 部署形态

单体部署、按能力包模块化：一个宿主进程（`XiHan.BasicApp.Host`）装配全部能力包，对外提供 HTTP API、内部 gRPC、MCP 与 SignalR Hub；前端是独立部署的单页应用。多租户、权限与审计贯穿所有包，不是某个模块的附属功能。

## 分层

宿主与框架按环层组织，外层依赖内层：

```text
Ring 6  XiHan.BasicApp.Host             装配：Full Profile + 显式 Provider + 能力包；普通端点（文件、OAuth、Hub、Webhook）
        XiHan.BasicApp.<能力包>          Tier 0–3
Ring 5  Protocols.Http / Grpc / Mcp      一份操作登记，三种协议
Ring 4  Hosting.AspNetCore + Hosting.CommandLine（seed / schema upgrade / operations 等命令）
Ring 3  Persistence.SqlSugar、Security.Jwt、MultiTenancy.Database、Security.Database、
        Auditing.Database、Scheduling.Database、Workflow.Database、Localization.Database …
Ring 1  Kernel（操作、管道、Result / Error、消息目录）    Ring 0  Primitives
```

能力包分四层，只能向下依赖、同层不互相引用；跨包协作经 `Contracts` 里的接口（如 `IWorkbenchWidgetProvider`、`ITenantQuota`、`IRealtimePush`）与消息主题：

```text
Tier 0 基座       Contracts → Runtime
Tier 1 身份租户    Identity → Tenancy → Organization → Access
Tier 2 平台能力    Settings | Notification | Files | Auditing | Maintenance | OpenPlatform | Realtime
Tier 3 可选模块    Workbench | Workflow | Printing | CodeGeneration | Chat | AI
Host              XiHan.BasicApp.Host
```

层级由 `XiHan.BasicApp.Architecture.Tests` 的 `TierDependencyTests` 守护。Tier 3 由 `BasicApp:Modules:<名>:Enabled` 开关，见[配置参考](./configuration#basicapp-modules)。

## 请求路径

```text
浏览器 ──HTTP──> Kestrel ──> 转发头 / 安全响应头 / 跨域 / 维护模式 / 认证 / 开放接口原文缓冲 ──> UseXiHan()
                                                                    │
            内核管道：Ingress → Observe → Resilience → Authenticate → Tenant → Authorize
                      → Validate → Idempotency → Transaction → Handle → Egress
                                                                    │
                                        [Operation] HandleAsync → Result<TResponse>
                                                                    │
            成功：200 / 201 + 响应 DTO        失败：application/problem+json（code、detail、traceId、errors）
```

各阶段里 BasicApp 贡献了哪些中间件（会话门禁、访问策略、权限条件、操作人、审计日志），见[请求生命周期](./backend/request-lifecycle)。

## 能力包一览

| 包 | Tier | 主要内容 |
| --- | --- | --- |
| `Contracts` | 0 | 实体基类与审计协议、分页 / 批量结果、权限描述、页面登记、跨包接口、消息主题、种子号段 |
| `Runtime` | 0 | SqlSugar 回调、`TenantScopedQuery`、操作人、数据保护、表结构升级器、种子执行器、维护模式 |
| `Identity` | 1 | 登录（密码、验证码、第三方、双因素）、令牌与会话、锁屏、用户管理、在线用户、个人中心、模仿登录、用户设置 |
| `Tenancy` | 1 | 租户开通三步、独立库、套餐与配额、成员与所有权转移、我的订阅、切换租户 |
| `Organization` | 1 | 部门（闭包表）、岗位、部门成员 |
| `Access` | 1 | 角色与授权、Deny、继承、委托、数据范围、字段安全、权限条件、访问策略、约束规则、菜单 |
| `Settings` | 2 | 参数配置、字典、业务编号 |
| `Files` | 2 | 存储配置、文件与副本、秒传、分片上传、签名地址、导出中心、导入历史 |
| `Notification` | 2 | 通知公告、收件箱、消息模板、邮件 / 短信、群机器人、Telegram 机器人 |
| `Auditing` | 2 | 八类日志与链路追踪的查询面、日志写入队列、保留清理 |
| `Maintenance` | 2 | 任务调度、缓存管理、服务监控、版本与升级记录 |
| `OpenPlatform` | 2 | 签名开放接口、OAuth 2.0 / OIDC 服务端、OAuth 应用、我的应用、授权同意 |
| `Realtime` | 2 | 通知 Hub、连接与在线状态、按受众推送 |
| `Workbench` | 3 | 工作台小组件汇总（各包登记的指标、图表与列表） |
| `Workflow` | 3 | 流程定义与实例、审批中心、审批业务接入 |
| `Printing` | 3 | 打印模板（平台全局与租户）、按编码解析、打印数据源目录 |
| `CodeGeneration` | 3 | 数据源、表结构导入、模板、代码生成、零代码只读运行时 |
| `Chat` | 3 | 单聊、群聊、部门群与助手会话、聊天 Hub、聊天审计、保留清理 |
| `AI` | 3 | AI 提供商、提示词、知识库、助手 |

## 技术栈

### 后端

| 项 | 选型 |
| --- | --- |
| 运行时 | .NET 10（`net10.0`，警告即错误、Nullable、代码风格在构建期执行） |
| 框架 | XiHan.Framework（`Profiles.Full`，NuGet 包；联调时以源码引用锁定提交） |
| 数据 | SqlSugar（框架 `Persistence.SqlSugar`），PostgreSQL 为缺省与 CI 口径 |
| 认证 | 框架 `Security.Jwt` 校验 HS256；签发、刷新、会话由 Identity 负责 |
| 实时 | ASP.NET Core SignalR（`/hubs/notification`、`/hubs/chat`） |
| 邮件 / 模板 | MailKit、Scriban |
| 接口文档 | 框架 OpenAPI 生成器 + Scalar |
| 测试 | xUnit（Microsoft.Testing.Platform）、TestServer + SQLite / PostgreSQL |

### 前端

| 项 | 选型 |
| --- | --- |
| 基础 | Vue、Vite、TypeScript、Pinia、Vue Router（缺省 hash 模式）、vue-i18n |
| UI | XiHan.UI（`@xihan-ui/vue`、`headless`、`core`、`styles`、`tokens`、`icons` 等）、Tailwind CSS |
| 契约 | `scripts/gen-contracts.mjs` 生成 `packages/contracts/*` |
| 实时 | `@microsoft/signalr` |
| 时间 | dayjs（utc + timezone，按用户时区显示） |
| 工具链 | oxlint + eslint、Vitest + jsdom；Node.js 24+、pnpm 11+，没有锁文件 |

## 下一步

- 先按[快速开始](./getting-started)在本机运行，再读[目录结构](./project-structure)。
- 后端从[架构总览](./backend/introduction)与[开发流程](./backend/development)开始，前端从[框架简介](./frontend/introduction)开始。
