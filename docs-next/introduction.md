# XiHan.BasicApp 6.0

本页介绍 XiHan.BasicApp 6.0 的定位与它和 5.x 的差异，适用于初次了解 6.0 的读者。

::: tip 正在使用 5.x
5.x 部署留在 `release/v5` / `hotfix/v5` 维护线，文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/v5/)。6.0 不提供数据迁移，见[表结构升级](./backend/upgrade)。
:::

## 定位

XiHan.BasicApp 是曦寒生态的企业级中后台应用。6.0 是一次从零重写：后端基于 XiHan.Framework 5，按框架原生的「能力包 + 操作契约」写法组织；前端基于 XiHan.UI，API 层由后端导出的 `openapi.json` 生成。

6.0 有两个用途。一是作为 XiHan.Framework 5 的官方参考实现，只使用框架的一等公民概念（`IFeature` 能力包、`[Operation]` 操作、管道阶段中间件、Provider、消息键），不另建平行机制。二是作为新项目的起点模板，已包含多租户、身份与会话、RBAC 及其收窄规则、组织机构、参数与字典、文件、通知、审计日志、任务调度与开放平台等中后台的通用部分，另有工作流、在线聊天、打印模板、代码生成与 AI 几个可选模块。

## 与 5.x 的差异

| 方面 | 5.x | 6.0 |
| --- | --- | --- |
| 接口层 | `[DynamicApi]` 应用服务，`/Controller/Action` 风格路由 | 一方法一个 `[Operation("<资源>.<动词>")]`，路由由操作 ID 推导，前缀 `/api` |
| 响应 | 统一 `ApiResponse` 信封 + 业务码 | 成功即 DTO；失败为 RFC 9457 `application/problem+json`，按 `code` 分支 |
| 协议 | HTTP | 同一份操作同时服务 HTTP、gRPC（内部调用）、MCP（AI 工具） |
| 主键 / 租户 ID | 雪花 `long` / `long`，0 表示平台 | UUID v7 / 字符串（即租户编码），平台数据不实现 `IMultiTenant` |
| 权限码 | 冒号形式 + 226 个按钮码 | `<包>.<资源>.<动作>` 点分三段，按钮即操作 |
| 前端 API 层 | 78 组手写 facade 与 `*.types.ts` | 由 `openapi.json` 生成类型、操作客户端、权限码与枚举 |
| 数据 | 可从 4.x / 5.x 升级 | 不提供 5.x → 6.0 数据迁移，从种子全新初始化（见[表结构升级](./backend/upgrade)） |

动机与取舍见[为什么是 6.0](./why)，逐项能力见[功能清单](./features)，版本变化见[更新日志](./changelog)。

## 阅读路径

| 目标 | 文档 |
| --- | --- |
| 在本机运行 | [开发环境](./dev-environment) → [快速开始](./getting-started) |
| 了解整体结构 | [系统概述](./overview) → [目录结构](./project-structure) → [后端架构总览](./backend/introduction) |
| 新增操作或页面 | [后端开发流程](./backend/development) · [前端开发流程](./frontend/development) |
| 对接接口 | [接口对接指南](./api-guide) |
| 查阅配置项 | [配置参考](./configuration) |
| 部署与升级 | [部署](./deployment) · [表结构升级](./backend/upgrade) |

## 技术栈

| 区域 | 技术 |
| --- | --- |
| 后端 | .NET 10、XiHan.Framework 5（`Profiles.Full`）、SqlSugar、PostgreSQL（缺省）、JWT（HS256）、SignalR、Scriban |
| 协议 | HTTP（OpenAPI 3.1 + Scalar）、gRPC（内部服务调用）、MCP |
| 前端 | Vue 3.5、Vite、TypeScript、Pinia、vue-router、vue-i18n（10 种区域）、Tailwind CSS 4、XiHan.UI |
| 测试 | xUnit（Microsoft.Testing.Platform）、Vitest |

## 与 XiHan.Framework 的关系

框架只提供通用能力与 Provider：令牌校验、策略判定、仓储、工作单元、租户目录、授予存储、操作审计、调度、对象存储、本地化。BasicApp 在其上实现「业务」：签发令牌与会话、角色与授权规则、租户开通、菜单、日志查询、通知渠道等。框架没有的、或属于应用的（SignalR Hub、OAuth 服务端、模板引擎、第三方登录），都是 BasicApp 自己的能力包。

`backend-next` 以 ProjectReference 引用同级检出的 `XiHan.Framework/next/src`，锁定提交写在 `backend-next/props/framework.props` 的 `XiHanFrameworkCommit`，架构测试核对检出的 HEAD；框架 5.0 发布到 NuGet 前不走包引用。
