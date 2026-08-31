<div align="center">

<img src="./assets/banner.png" alt="XiHan.BasicApp" />

<h1>XiHan.BasicApp</h1>

<p><b>基于 .Net + Vue 的超高颜值中后台内核</b></p>

<p>后端基于 .NET 10 与 <a href="https://github.com/XiHanFun/XiHan.Framework">XiHan.Framework</a>，前端基于 Vue 3 与 <a href="https://github.com/XiHanFun/XiHan.UI">XiHan.UI</a><br/>多租户 · RBAC + 数据范围 + 字段脱敏 · 代码生成 · 实时通信</p>

<p><a href="./README.md">English</a> | <b>简体中文</b></p>

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


<img src="./assets/login.png" alt="登录" />

</div>

## 简介

XiHan.BasicApp 采用前后端分离架构。后端遵循 DDD 分层，写路径走应用服务、读路径走查询服务，应用服务经动态 API 直接暴露为 REST 接口；前端使用 Vue 3 + TypeScript + XiHan.UI。系统内置完整的身份、权限、租户与审计能力，既可作为中后台项目的起点，也可作为 .NET + Vue 全栈实践的参考。属于曦寒懿（XiHanFun）开源生态的基础应用，拥有底座、组件、应用的完整生态。

## 文档

| 去处 | 内容 |
| --- | --- |
| [文档站](https://basicapp.docs.xihanfun.com) | 完整指南与逐主题专题文档 |
| [后端工程说明](./backend/README_cn.md) | 分层结构、工程清单、接口暴露、依赖构成、本地开发 |
| [前端工程说明](./frontend/README_cn.md) | 技术栈、monorepo 结构、架构约束、本地开发 |

## 预览

<table>
  <tr>
    <td align="center"><img src="./assets/tenant-select.png" /><br/>租户选择</td>
    <td align="center"><img src="./assets/user-management.png" /><br/>用户管理</td>
  </tr>
  <tr>
    <td align="center"><img src="./assets/user-management-dark.png" /><br/>用户管理（暗色）</td>
    <td align="center"><img src="./assets/server-monitor.png" /><br/>服务监控</td>
  </tr>
  <tr>
    <td align="center"><img src="./assets/preference-settings.png" /><br/>偏好设置</td>
    <td align="center"><img src="./assets/operation-log.png" /><br/>操作日志</td>
  </tr>
  <tr>
    <td align="center"><img src="./assets/log-traceability.png" /><br/>日志链路</td>
    <td align="center"><img src="./assets/about.png" /><br/>关于</td>
  </tr>
</table>



移动端：

<img src="./assets/mobile.png" />

## 功能

**身份与认证**

- 用户、角色、部门、菜单管理
- JWT 双令牌（Access 120 分钟 + Refresh 7 天），多端登录与会话管理
- 多种登录方式：账号密码、邮箱 / 短信验证码、2FA（TOTP / 邮箱 / 短信，可位组合）
- OAuth 内置八家自研提供商：GitHub、Gitee、Google、QQ、微信、企业微信、钉钉、飞书，按配置启用
- 登录防护：自绘 SVG 图形验证码（一次性消费、可配置关闭）、账号+IP 与纯 IP 双维度防爆破节流、失败计数锁定、默认密码登录强制改密
- 密码 PBKDF2 哈希（OWASP 推荐迭代次数）；一次性验证码消费即销毁、恒定时间比较

**权限**

- 权限码三段式 `module:resource:action`，超级管理员通配符为 `*`
- 角色层级继承（闭包表）、数据范围五档（本人 / 本部门 / 本部门及子级 / 全部 / 自定义）、字段级脱敏六策略
- 角色互斥与基数约束（静态职责分离），按约束组判定
- 权限申请审批、权限委托（临时授权、可撤销）与变更留痕

**多租户**

- 默认字段级隔离，全局数据使用 `TenantId=0` 约定；另支持按租户独立数据库
- 邮箱全局唯一登录，登录后按归属自动落点（控制台 / 工作台 / 租户选择），可随时切换租户
- 超级管理员平台态运维，可切入任意租户代为管理
- 租户版本（Edition）权限白名单运行时门控；开通一站式建管理员、角色与授权；降级自动回收越权授权

**审计日志**

- 访问 / 接口 / 操作 / 异常 / 登录 / 实体变更 六类日志，各自独立写入
- 落库前自动脱敏（密码、令牌、密钥、证件号等，带反例白名单避免误掩）；实体变更区分新增 / 修改 / 删除 / 恢复
- 链路追踪时间线：支持按 TraceId、用户名、会话标识、IP、用户主键五个维度跨类型聚合

**代码生成**

- 单表 / 树形 / 主从三种模式，从实体、DTO、API 到前端页面一键生成
- 原生 Scriban 模板以嵌入资源打进程序集，各带手工变体；同时生成菜单权限、权限种子、页面描述符与种子器
- Zip 下载或落盘；落盘默认关闭，需显式配置绝对路径白名单根目录，带路径穿越 fail-closed

**AI 能力**

- AI 提供商接入与密钥托管（DataProtection 可逆加密落库，Chat / Embedding 模型可配置）
- 提示词库：数据库存储、可覆盖框架默认提示词
- 知识库 RAG：文档摄取、向量检索（Qdrant），租户级隔离，保留原文供重建索引
- 技能注册即自动暴露为对话工具与 MCP 工具；内置知识检索技能，业务技能实现 `IAiSkill` 即自动接入
- 可配置多助手，聊天页 AI 助手桥接

**平台能力**

- 动态 API：应用服务经 `[DynamicApi]` 暴露，无 Controller 样板，Scalar 文档自动生成
- 菜单单一事实源：后端 `PageRegistry` 统一注册菜单、路由、组件路径、权限码与国际化键
- 全链路分布式缓存（授权快照、版本门控、菜单、配置、字典），写路径精准失效
- 请求追踪；SignalR 双 Hub（实时通知 + 在线聊天）
- 消息中心：邮件 / 短信 / 站内通知模板，租户可覆盖默认
- 开放平台：内置 OAuth2 / OIDC 身份提供方（第三方应用注册、用户同意授权），个人级 OpenAPI 凭证（签名调用）
- 服务器信息监控（主板 / CPU / 内存 / 磁盘 / GPU / 网络 / 运行时）、缓存键查询与按 pattern 清理
- 文件多存储（本地 / 阿里云 OSS / 腾讯云 COS / MinIO）、定时任务（数据库持久化）、审核工作流、国际化（中 / 英）
- 导出中心：异步任务 + CSV / XLSX 两种写出器 + 延迟队列

**前端体验**

- Schema 驱动列表页：搜索 / 表格 / 导出由配置生成，内置列设置、密度切换、高级搜索、个人视图保存、行悬停速览、树形模式与列宽拖拽
- 权限 / 租户 / 偏好感知：页面、字段、操作三级按权限码过滤，字段级脱敏；列设置与搜索偏好同步到后端，多端一致
- 灵动岛全局反馈、多标签页、收藏夹、命令面板式全局搜索
- 消息中心：顶部横幅、登录弹窗、通知中心，支持强制阅读与按角色 / 部门定向
- 偏好中心：亮 / 暗主题、主题色、布局风格与紧凑度，偏好云端同步
- 富文本（Tiptap）与 Markdown 编辑器、Cron 可视化、JSON 编辑 / 查看、代码编辑器
- 锁屏、水印、时区切换、导出中心

## 技术栈

后端 .NET 10 + XiHan.Framework 4.0.0（SqlSugar / Redis / SignalR / Serilog / Scalar 均由框架带入）；前端 Vue 3 + TypeScript + Vite 8 + XiHan.UI + Pinia + Tailwind CSS 4。

逐项清单见[后端工程说明](./backend/README_cn.md#依赖构成)与[前端工程说明](./frontend/README_cn.md#技术栈)。

## 架构

系统分为框架层、模块层与主应用层，每个模块内部遵循 DDD 分层（Domain / Application / Infrastructure）。

```text
┌─────────────────────────────────────────────────────────────┐
│                   XiHan.BasicApp.WebHost                    │
│                    (启动入口与模块聚合)                     │
├──────────┬──────────┬──────────┬──────────┬─────────────────┤
│ CodeGen  │    AI    │ Workflow │ Printing │      Chat       │
│(代码生成)│ (AI/RAG) │ (工作流) │(打印模板)│   (在线聊天)    │
├──────────┴──────────┴──────────┴──────────┴─────────────────┤
│                     XiHan.BasicApp.Saas                     │
│       (RBAC / 多租户 / 组织 / 审批 / 审计 / 消息中心)       │
├─────────────────────────────────────────────────────────────┤
│                   XiHan.BasicApp.Web.Core                   │
│          (Web 侧基座 / 动态 API / 文档 / 维护模式)          │
├─────────────────────────────────────────────────────────────┤
│                     XiHan.BasicApp.Core                     │
│                  (应用基座 / DDD / 模块化)                  │
├─────────────────────────────────────────────────────────────┤
│                      XiHan.Framework.*                      │
│   底层框架(认证 / 授权 / 数据 / 缓存 / 事件总线 / 多租户)   │
└─────────────────────────────────────────────────────────────┘
```

| 项目 | 说明 | 可卸载 |
| --- | --- | --- |
| `XiHan.BasicApp.Core` | 应用基座，聚合框架的非 Web 模块与全应用共享约定 | 否 |
| `XiHan.BasicApp.Web.Core` | Web 侧基座，聚合六个框架 Web 模块，提供维护模式中间件 | 否 |
| `XiHan.BasicApp.Saas` | 平台治理模块：用户 / 角色 / 权限 / 菜单 / 部门 / 租户 / 配置 / 字典 / 文件 / 通知 / 审批 / 日志 / 任务 | 否 |
| `XiHan.BasicApp.CodeGeneration` | 代码生成：数据源管理 / 表结构导入 / 模板配置 / 全栈生成 | 是 |
| `XiHan.BasicApp.AI` | AI 能力：提供商与密钥管理 / 提示词库 / 知识库 RAG / AI 技能（MCP 工具）/ 聊天 AI 助手 | 是（助手桥接依赖 Chat） |
| `XiHan.BasicApp.Workflow` | 工作流引擎落地：流程定义 / 实例 / 待办（框架引擎的持久化与 API） | 是 |
| `XiHan.BasicApp.Printing` | 打印模板：可视化设计 / 租户与平台双作用域 / 按编码解析 | 是 |
| `XiHan.BasicApp.Chat` | 在线聊天：单聊 / 群聊 / 部门群 / AI 助手会话 / 实时推送 / 合规审计 | 是（删除须连带处理 AI 的助手桥接） |
| `XiHan.BasicApp.WebHost` | 启动入口，聚合所有模块 | — |

```text
XiHan.BasicApp/
├── backend/                 # 后端（.NET 10）
│   ├── src/
│   │   ├── framework/       #   Core / Web.Core 基础能力
│   │   ├── modules/         #   Saas + 五个可选模块（CodeGen/AI/Workflow/Printing/Chat）
│   │   └── main/            #   WebHost 启动入口
│   ├── props/               #   共享 MSBuild 属性
│   ├── scripts/             #   版本号与清理脚本
│   └── test/                #   测试项目
├── frontend/                # 前端（Vue 3 + XiHan.UI）
│   ├── src/                 #   应用源码（src/modules/ 与后端可选模块一一对应）
│   └── packages/            #   内部包
└── assets/                  # README 资源
```

### 卸载可选模块

一个可选模块 = 后端一个工程 + 前端一个 `src/modules/<模块>` 目录。后端与前端各自的卸载步骤见[后端工程说明](./backend/README_cn.md#卸载可选模块)与[前端工程说明](./frontend/README_cn.md#卸载可选模块)。

⚠️ **卸载必须伴随重建数据库**：菜单、权限、角色授权、定时任务的种子行不会随模块删除自动回收。

## 快速开始

### 环境要求

| 依赖 | 版本 | 说明 |
| --- | --- | --- |
| .NET SDK | 10.0+ | 后端必需 |
| Node.js | 24.0+ | 前端必需 |
| pnpm | 11.0+ | 前端必需 |
| PostgreSQL | 14+ | 唯一硬依赖，也可用 MySQL / SQL Server / SQLite / Oracle |
| Redis | 6.0+ | 可选，关闭后退化为进程内内存缓存 |
| Qdrant | v1.15+ | 仅启用 AI 知识库时需要 |

### 容器一键起

仓库根自带 `docker-compose.yml`，包含 PostgreSQL、Redis、Qdrant、后端与前端五个服务：

```bash
cp .env.example .env
docker compose up -d
```

默认前端 `http://localhost:8080`、后端 `http://localhost:9708`，端口可在 `.env` 里改。

### 后端

```bash
git clone https://github.com/XiHanFun/XiHan.BasicApp.git
cd XiHan.BasicApp/backend

dotnet run --project src/main/XiHan.BasicApp.WebHost --launch-profile Development
```

启动后访问 `http://127.0.0.1:9708/scalar` 查看 API 文档。各环境端口：Development `9708`、Production `9709`。

连接串配在 `backend/src/main/XiHan.BasicApp.WebHost/appsettings.Development.json` 的 `XiHan:Data:SqlSugarCore:ConnectionConfigs`。首次启动会自动建库建表并播种。

框架默认走 NuGet 包，克隆本仓即可编译；连框架一起改的方式见[后端工程说明](./backend/README_cn.md#框架引用方式)。

### 前端

```bash
cd frontend
pnpm install
pnpm dev
```

> `@xihan-ui/*` 取 npm 上的正式版，单独 clone 本仓即可 `pnpm install`，不需要并列检出 `XiHan.UI`。要连组件库源码一起调试，临时在 `pnpm-workspace.yaml` 加 `overrides` 指到 `link:../../XiHan.UI/ui/packages/*`，调完删掉、不要提交。

### 默认账号

初始超级管理员账号为 `superadmin`，密码 `SuperAdmin@123`。可通过 `Saas:Seed:SuperAdminPassword`（环境变量 `Saas__Seed__SuperAdminPassword`）覆盖。生产环境请务必覆盖，并在首次登录后立即修改。

## 项目生态

- [XiHan.Framework](https://github.com/XiHanFun/XiHan.Framework) - 快速、轻量、高效、用心的 .NET 现代模块化开发框架
- [XiHan.UI](https://github.com/XiHanFun/XiHan.UI) - 快速、轻量、高效、用心的框架无关跨端组件库
- [XiHan.BasicApp](https://github.com/XiHanFun/XiHan.BasicApp) - 基于 .Net + Vue 的超高颜值中后台内核

## 诚挚致谢

排名不分先后。

| 项目                                                         | 致谢                                           |
| ------------------------------------------------------------ | ---------------------------------------------- |
| [XiHan.Framework](https://github.com/XiHanFun/XiHan.Framework) | 作为本项目的后端底层框架支持                   |
| [XiHan.UI](https://github.com/XiHanFun/XiHan.UI)             | 作为本项目的前端视图组件支持                   |
| [NaiveUI](https://github.com/tusen-ai/naive-ui)              | 作为本项目的前端视图组件支持（v4.0.0前）       |
| [Blog.Core](https://github.com/anjoy8/Blog.Core)             | 作为部分后端架构、逻辑功能灵感来源（启蒙项目） |
| [ Admin.Core.ZR](https://gitee.com/izory/ZrAdminNetCore)     | 作为部分后端功能灵感来源                       |
| [YuebonCore](https://gitee.com/yuebon/YuebonNetCore)         | 作为部分后端功能灵感来源                       |
| [VbenAdmin](https://github.com/vbenjs/vue-vben-admin)        | 作为部分前端架构、视觉功能灵感来源（启蒙项目） |
| [SoybeanAdmin](https://github.com/soybeanjs/soybean-admin)   | 作为部分前端视觉功能灵感来源                   |
| [LitheAdmin](https://github.com/tenianon/lithe-admin)        | 作为部分前端视觉功能灵感来源                   |
| 其他第三方依赖                                               | 作为项目功能丰富与拓展的基石                   |


## 支持&赞助

如果此项目对你的开发有助益，也欢迎请作者一杯咖啡。

官方赞助页 https://docs.xihanfun.com/cosmos/sponsor


## 版权&授权

Copyright (c) 2021-Present XiHanFun and contributors.

本项目采用 MIT 授权，详见 [License](./LICENSE)

XiHan.BasicApp Logo、XiHan.BasicApp名称、界面视觉设计与原创视觉表达归作者所有，第三方依赖和第三方服务分别遵循其各自授权与服务条款。

项目仅供学习参考，作者不承担任何软件的使用风险。
