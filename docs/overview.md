# 系统概述

XiHan.BasicApp 是一个**企业级中后台内核**：后端基于 .NET 10 与 [XiHan.Framework](https://framework.docs.xihanfun.com/)，前端基于 Vue 3。它把中后台系统里"每个项目都要重做一遍"的部分——身份、权限、租户、审计、代码生成、实时通信——一次性做好，让你专注在真正的业务上。

## 定位

- **面向生产的中后台起点**：提供 RBAC + ABAC、多租户与审计等基础能力，上线前仍需完成安全配置、容量验证与业务适配
- **全栈实践参考**：一套规范的 .NET + Vue 前后端分离工程，从契约到 UI 数据流都可借鉴
- **框架用法样板**：XiHan.Framework 各能力在真实业务里怎么用，这里有活的例子

## 架构风格

系统采用**前后端分离**：

- **后端**遵循 DDD 分层与 CQRS，应用服务经**动态 API** 直接暴露为 REST 接口——不写 Controller，接口即服务方法
- **前端**使用 Vue 3 + TypeScript + Naive UI，列表页由 **Schema 驱动**，搜索/表格/导出按配置生成，并对权限、租户、个人偏好三重感知

详见 [系统架构](./backend/introduction)。

## 能力全景

| 领域 | 关键能力 |
| --- | --- |
| [身份与认证](./backend/authentication) | 用户/角色/菜单；JWT 双令牌；账号密码、邮箱/短信验证码、OAuth2（GitHub/Gitee/Google/QQ）、2FA |
| [权限](./backend/permission) | RBAC + ABAC，权限码 `module:resource:action`（如 `saas:user:read`），数据范围，字段级脱敏，会话角色激活 |
| [组织架构](./backend/organization) | 部门树 + 闭包表、岗位字典、用户多部门归属（数据范围的底座） |
| [多租户](./backend/multi-tenancy) | 字段级隔离，邮箱全局唯一登录，租户版本（Edition）权限白名单门控 |
| [消息中心](./backend/messaging) | 五类消息 + 优先级 + 强制阅读；模板；四渠道扇出；SignalR 通知与在线聊天 |
| [工作流](./backend/workflow) | 图执行引擎、17 个内置活动、人工任务（或签/会签/依次）、落库可恢复 |
| [审批与约束](./backend/approval) | 通用审批单 `SysReview`；RBAC 约束规则引擎（SSD/DSD/互斥/基数…八类） |
| [文件与存储](./backend/file) | 元数据与存储位置分离、多后端（本地/S3/OSS/COS/MinIO）、异步导出中心 |
| [任务调度](./backend/scheduling) | Cron/间隔/一次性触发、并发控制、多租户上下文、按月分表日志 |
| [系统设置](./backend/settings) | 菜单、字典、参数、业务编号、缓存、服务监控、版本、通道配置 |
| [健康与可观测性](./backend/health-observability) | 数据库/Redis/Qdrant 健康检查，TraceId，OpenTelemetry 链路与指标导出 |
| [升级与迁移](./backend/upgrade) | 前向 SQL、版本/迁移台账、租约锁、多租户升级与维护模式；执行入口状态说明 |
| [开放能力](./backend/open-api) | OAuth2/OIDC 服务端、开放接口签名调用、个人 API 凭证 |
| [审计日志](./backend/logging) | 访问/API/操作/异常/登录/实体变更等多类日志，落库前自动脱敏 |
| [代码生成](./backend/code-generation) | 单表/树形/主从三模式，实体→DTO→API→前端页一键生成，Scriban 模板 |
| [AI 能力](./backend/ai) | Provider 库化管理（热切换）、RAG 知识库（Qdrant）、Agent / MCP |
| [前端体验](./frontend/introduction) | Schema 驱动列表、命令面板、多标签、消息中心、偏好中心、富文本、时区切换 |

完整清单见 [功能清单](./features)。

## 技术栈

### 后端

| 技术 | 说明 |
| --- | --- |
| .NET 10 / C# | 运行时与语言 |
| XiHan.Framework 3.10.1 | 自研模块化应用框架 |
| SqlSugar | ORM，支持 PostgreSQL / MySQL / MariaDB |
| Redis | 分布式缓存与分布式锁 |
| SignalR | 实时通信 |
| Serilog | 结构化日志 |
| Scalar | API 文档 |

### 前端

| 技术 | 说明 |
| --- | --- |
| Vue 3.5+ | UI 框架 |
| TypeScript 6.0+ | 类型系统 |
| Vite 8 | 构建工具 |
| Naive UI | 组件库 |
| Pinia | 状态管理 |
| Tailwind CSS 4 | 原子化 CSS |
| Tiptap | 富文本编辑器 |
| vue-i18n | 国际化 |

## 文档怎么读

按角色挑一条线走：

| 你是 | 推荐路线 |
| --- | --- |
| **第一次接触** | [开发环境](./dev-environment) → [快速开始](./getting-started) → [目录结构与代码地图](./project-structure) |
| **后端开发** | [架构总览](./backend/introduction) → [后端架构](./backend/introduction) → [请求生命周期](./backend/request-lifecycle) → [数据模型](./backend/data-model) → [二次开发](./backend/development) |
| **前端开发** | [前端架构](./frontend/introduction) → [前端开发指南](./frontend/introduction) → [Schema 驱动页面](./frontend/schema-page) |
| **对接接口** | [接口对接指南](./api-guide)（含**怎么获取 token**） |
| **运维部署** | [配置参考](./configuration) → [部署](./deployment) |
| **排查问题** | [常见问题](./faq) → [请求生命周期](./backend/request-lifecycle#按现象定位) |

## 下一步

- [开发环境](./dev-environment)：用 Docker 命令行准备数据库与 Redis 等本地依赖
- [快速开始](./getting-started)：在本地把系统跑起来
- [架构总览](./backend/introduction)：全景与五个架构分册的索引
- [权限模型](./backend/permission)：RBAC + ABAC 与多租户隔离
