# XiHan.BasicApp 基础应用

**企业级中后台内核。** 后端基于 .NET 10 与 [XiHan.Framework](https://framework.docs.xihanfun.com/)，前端基于 Vue 3，开箱即带多租户、RBAC + ABAC 权限、代码生成与实时通信等能力。它既是一套可直接投产的中后台起点，也是学习 .NET + Vue 全栈实践、以及 XiHan.Framework 用法的最佳参考。

## 它是什么

XiHan.BasicApp 采用**前后端分离**架构：

- **后端** 遵循 DDD 分层与 CQRS，应用服务经**动态 API** 直接暴露为 REST 接口，无需写 Controller
- **前端** 使用 Vue 3 + TypeScript + Naive UI，Schema 驱动的列表页、权限/租户/偏好三重感知

系统内置完整的身份、权限、租户与审计能力。你可以拿它当项目脚手架直接改，也可以只读它的代码学习框架怎么用。

## 从这里开始

<div class="tip custom-block" style="padding-top: 8px">

还在做技术选型？先读 [**为什么选择曦寒**](./why) —— 它适合什么项目、和其它中后台方案的区别、以及它的短板。

已经决定动手？按这个顺序走：

1. [**快速开始**](./getting-started) —— 在本地把前后端跑起来（约 10 分钟）
2. [**系统架构**](./backend/introduction) —— 看懂后端模块划分与前后端协作
3. [**权限模型**](./backend/permission) —— 理解 RBAC + ABAC、数据范围、多租户隔离

</div>

## 文档地图

**入门**

- [为什么选择曦寒](./why) —— 选型指南：适合什么项目、四条方案路线的取舍、它的短板
- [系统概述](./overview) —— 定位、技术栈、能力全景、**按角色的阅读路线**
- [开发环境](./dev-environment) —— Docker 起数据库与 Redis
- [快速开始](./getting-started) —— 跑起来并调通第一个接口
- [目录结构与代码地图](./project-structure) —— **「我要改 X 该去哪个文件」**

**架构**

- [架构总览](./backend/introduction) —— 全景、四条贯穿全局的设计、数据流
- [后端架构](./backend/introduction) —— 分层、模块装配、DDD、DI 约定、种子
- [前端架构](./frontend/introduction) —— 五层结构、启动引导、请求时序
- [请求生命周期](./backend/request-lifecycle) —— 中间件管道逐段、401/403/423、UoW 收尾
- [数据模型](./backend/data-model) —— 实体基类、审计与软删、多租户列、全表清单
- [缓存与异步](./backend/caching) —— 缓存条目、精准失效、队列消费
- [健康与可观测性](./backend/health-observability) —— `/health`、数据库/Redis/Qdrant 探测、OpenTelemetry

**身份与访问**

- [身份与认证](./backend/authentication) —— 多种登录、JWT 双令牌、会话、2FA
- [权限模型](./backend/permission) —— RBAC + ABAC、权限码、数据范围、字段脱敏
- [组织架构](./backend/organization) —— 部门树与闭包表、岗位、用户归属
- [多租户与版本](./backend/multi-tenancy) —— 字段级隔离、租户切换、版本门控

**业务功能**

- [消息中心](./backend/messaging) · [工作流](./backend/workflow) · [审批与约束规则](./backend/approval)
- [文件与存储](./backend/file) · [任务调度](./backend/scheduling) · [系统设置](./backend/settings)
- [开放能力](./backend/open-api) · [审计日志](./backend/logging) · [升级与迁移](./backend/upgrade)
- [代码生成](./backend/code-generation) · [AI 能力](./backend/ai)

**前端开发**

- [前端开发指南](./frontend/introduction) —— 技术栈、api 层、SignalR、常用组件
- [Schema 驱动页面](./frontend/schema-page) · [路由与菜单](./frontend/routing) · [权限与脱敏](./frontend/permission) · [主题与国际化](./frontend/theme)

**二次开发与参考**

- [二次开发](./backend/development) —— 加功能纵切片 / 加独立模块 / 加前端页
- [接口对接指南](./api-guide) —— 响应信封、业务码、**怎么获取 token**、分页协议
- [配置参考](./configuration) —— `appsettings` 全量配置节
- [功能清单](./features) · [常见问题](./faq) · [部署](./deployment) · [更新日志](./changelog)

## 技术栈速览

| 端 | 关键技术 |
| --- | --- |
| 后端 | .NET 10 · XiHan.Framework 3.10.1 · SqlSugar（PostgreSQL/MySQL/MariaDB）· Redis · SignalR · Serilog · Scalar |
| 前端 | Vue 3.5+ · TypeScript 6.0+ · Vite 8 · Naive UI · Pinia · Tailwind CSS 4 · Tiptap · vue-i18n |

## 在线体验

- **在线用例**：<https://basicapp.xihanfun.com>
- **源码**：[GitHub](https://github.com/XiHanFun/XiHan.BasicApp) · [Gitee](https://gitee.com/XiHanFun/XiHan.BasicApp)

## 与 XiHan.Framework 的关系

XiHan.BasicApp 构建在 [XiHan.Framework](https://framework.docs.xihanfun.com/) 之上——框架提供认证、授权、数据、缓存、事件总线、多租户、动态 API 等底层能力，BasicApp 在其上实现具体的中后台业务。想先理解底层机制，建议先读[框架快速上手](https://framework.docs.xihanfun.com/quickstart)。
