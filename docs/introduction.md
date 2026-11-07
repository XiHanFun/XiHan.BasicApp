# 应用简介

本页说明 XiHan.BasicApp 的定位、核心约定、技术栈与阅读路径，适用于初次了解本项目的读者。

## 定位

XiHan.BasicApp 是曦寒生态的企业级中后台应用。后端基于 XiHan.Framework，按框架的「能力包 + 操作契约」组织；前端基于 XiHan.UI，接口层由后端导出的 `openapi.json` 生成。

它有两个用途：

- XiHan.Framework 的参考应用：只用框架的一等概念（`IFeature` 能力包、`[Operation]` 操作、管道阶段中间件、Provider、消息键），不另建平行机制。
- 新项目的起点：已包含多租户、身份与会话、RBAC 及其收窄规则、组织机构、参数与字典、文件、通知、审计日志、任务调度与开放平台等中后台的通用部分，另有工作台、工作流、在线聊天、打印模板、代码生成与 AI 几个可选模块。

## 核心约定

| 方面 | 约定 |
| --- | --- |
| 接口 | 一个方法一个 `[Operation("<资源>.<动词>")]`，路由由操作 ID 推导，前缀 `/api`；不写 Controller |
| 响应 | 成功即 DTO；失败是 RFC 9457 `application/problem+json`，按 `code` 分支 |
| 协议 | 同一份操作按 `Expose` 发布到 HTTP、gRPC（内部调用）与 MCP（AI 工具） |
| 主键与租户 | 主键 UUID v7；租户 ID 是字符串；平台库实体不实现 `IMultiTenant`，租户库实体可随租户分库 |
| 权限码 | `<包>.<资源>.<动作>` 点分三段；按钮直接用操作的权限码 |
| 前端接口层 | 由 `openapi.json` 生成类型、操作客户端、权限码与枚举，不手写 |
| 表结构 | `Schema/<版本>/*.sql` 加版本台账，启动时自动升级；新库经 `schema upgrade` + `seed` 初始化（见[表结构升级](./backend/upgrade)） |

取舍与理由见[设计取舍](./why)，逐项能力见[功能清单](./features)，版本变化见[更新日志](./changelog)。

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
| 后端 | .NET 10、XiHan.Framework（`Profiles.Full`）、SqlSugar、PostgreSQL（缺省）、JWT（HS256）、SignalR、Scriban |
| 协议 | HTTP（OpenAPI 3.1 + Scalar）、gRPC（内部服务调用）、MCP |
| 前端 | Vue、Vite、TypeScript、Pinia、Vue Router、vue-i18n（10 种区域）、Tailwind CSS、XiHan.UI |
| 测试 | xUnit（Microsoft.Testing.Platform）、Vitest |

依赖版本见 `backend/Directory.Packages.props` 与 `frontend/package.json`。

## 与 XiHan.Framework 的关系

框架只提供通用能力与 Provider：令牌校验、策略判定、仓储、工作单元、租户目录、授予存储、操作审计、调度、工作流引擎、对象存储、本地化。BasicApp 在其上实现业务：签发令牌与会话、角色与授权规则、租户开通、菜单、日志查询、通知渠道等。框架没有、或属于应用的能力（SignalR Hub、OAuth 服务端、模板引擎、第三方登录），都是 BasicApp 自己的能力包。

后端缺省以 ProjectReference 引用同级检出的 XiHan.Framework 源码，锁定的框架提交写在 `backend/props/framework.props` 的 `XiHanFrameworkCommit`，架构测试 `FrameworkPinTests` 核对检出的 HEAD。`-p:UseXiHanFrameworkSource=false` 改用 NuGet 包引用。细节见[开发环境](./dev-environment#框架检出)。
