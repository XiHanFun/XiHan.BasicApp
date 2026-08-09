# 目录结构与代码地图

这页解决一个具体问题：**「我要改 X，该去哪个文件？」** 上半部分是目录树，下半部分是按任务索引的代码地图。

## 仓库全貌

```text
XiHan.BasicApp/
├── backend/                          # 后端（.NET 10）
│   ├── src/
│   │   ├── framework/                #   基座层
│   │   │   ├── XiHan.BasicApp.Core/          实体/DTO 基类、查询服务标记、聚合框架模块
│   │   │   └── XiHan.BasicApp.Web.Core/      聚合 Web 能力并接入升级维护模式
│   │   ├── modules/                  #   业务模块层（四个一等模块）
│   │   │   ├── XiHan.BasicApp.Saas/
│   │   │   ├── XiHan.BasicApp.CodeGeneration/
│   │   │   ├── XiHan.BasicApp.AI/
│   │   │   └── XiHan.BasicApp.Workflow/
│   │   ├── business/                 #   OA/CMS/CRM/ERP 等领域脚手架（当前未接入 WebHost）
│   │   └── main/
│   │       └── XiHan.BasicApp.WebHost/       启动入口 + 根模块 + appsettings
│   ├── props/                        #   共享 MSBuild 属性（framework.props 源码/NuGet 切换）
│   ├── test/                         #   测试项目
│   └── XiHan.BasicApp.slnx           #   解决方案（始终走 NuGet 引用框架）
├── frontend/                         # 前端（Vue 3 + Vite）
│   ├── src/                          #   应用装配与业务视图
│   ├── packages/                     #   可复用内核
│   ├── .env / .env.development / .env.production
│   └── nginx.conf                    #   生产静态托管与 /api 反代示例
├── docker-compose.yml
└── assets/
```

`backend/src/business` 下的 OA、CMS、CRM、HRM、ERP 等 15 个项目目前是解决方案内脚手架，模块注册基本为空，且 `XiHanBasicAppWebHostModule` 未依赖它们。它们代表预留边界，不应当作已交付业务功能。

## 后端：一个业务模块内部

以 `XiHan.BasicApp.Saas` 为例，四个模块结构一致：

```text
XiHan.BasicApp.Saas/
├── XiHanBasicAppSaasModule.cs        # 模块类：[DependsOn] + ConfigureServices
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # ★ 所有接线集中在这里
├── Domain/                           # 领域层
│   ├── Entities/                     #   实体（+ Expands 派生属性 / Aggregates 聚合行为 / Enums）
│   ├── DomainServices/               #   领域服务（接口 + Implementations + 命令 record）
│   ├── Repositories/                 #   仓储接口
│   ├── Permissions/                  #   ★ 权限码与权限定义（单一事实源）
│   ├── Events/                       #   领域事件
│   ├── Specifications/ ValueObjects/ Enums/
├── Application/                      # 应用层
│   ├── AppServices/                  #   ★ 写侧应用服务（按域分目录）
│   ├── QueryServices/                #   ★ 读侧查询服务
│   ├── Contracts/                    #   应用服务接口
│   ├── Dtos/  Mappers/               #   DTO 与映射
│   ├── Caching/                      #   ★ 缓存条目 + 键常量 + 失效器
│   ├── EventHandlers/                #   事件处理器
│   ├── Pages/PageRegistry.cs         #   ★ 菜单/路由/权限码/i18n 键单一事实源
│   └── Exporting/                    #   导出投影
├── Infrastructure/                   # 基础设施层
│   ├── Repositories/                 #   仓储实现
│   ├── Seeders/                      #   ★ 种子数据（System 基线 / Demo 演示）
│   ├── Auth/  Security/  MultiTenancy/
│   ├── Messaging/  Logging/  Tasks/  OAuth/
├── Hubs/                             # SignalR Hub
└── OpenApi/                          # 开放接口自测端点
```

带 ★ 的是**改动频率最高**的位置。

## 前端目录

```text
frontend/
├── src/                              # 应用侧（别名 @）
│   ├── main.ts                       #   ★ 引导顺序（图标→i18n→请求绑定→守卫→mount）
│   ├── App.vue                       #   Provider 装配 + 全局挂件
│   ├── app/context.ts                #   ★ 注册视图 glob / api 实例 / 静态路由
│   ├── api/
│   │   ├── base.ts                   #   ★ 动态 API 客户端
│   │   ├── factory.ts                #   ★ defineResource 资源工厂
│   │   ├── helpers.ts                #   分页/过滤/排序构造
│   │   └── modules/**                #   ★ 按域的 API 与 DTO 类型
│   ├── router/                       #   路由入口（守卫、静态路由）
│   ├── styles/index.css              #   Tailwind v4 入口（CSS-first @theme）
│   ├── locales/                      #   应用级语言包
│   └── views/**                      #   ★ 业务视图
└── packages/                         # 内核侧（别名 ~）
    ├── request/                      #   ★ RequestClient（拦截器/解包/401/423）
    ├── components/
    │   ├── schema/                   #   ★ Schema 驱动表格引擎
    │   ├── common/  chat/
    ├── router/                       #   ★ dynamic.ts（后端菜单→路由）+ guard.ts
    ├── stores/  hooks/  composables/
    ├── layouts/                      #   布局与顶部横幅/通知门
    ├── locales/langs/{zh-CN,en-US}/  #   ★ 语言包（含 menu.ts）
    ├── design/  iconify/  diagram/  plugins/  views/
    └── types/  utils/  constants/
```

## 代码地图：我要改 X，去哪儿

### 后端

| 我要… | 改这里 |
| --- | --- |
| 加一个业务实体 | `Domain/Entities/SysXxx.cs`（+ `Expands/`），选对[实体基类](./backend/entity#先选对基类) |
| 加一个权限码 | `Domain/Permissions/SaasPermissionCodes.cs`（常量）**和** `SaasPermissionDefinitions.cs`（落库定义），两处都要 |
| 加一个菜单/页面/按钮 | `Application/Pages/PageRegistry.cs` 的 `All` / `Buttons` |
| 加一个接口 | `Application/AppServices/**`（写）或 `QueryServices/**`（读）；**分页方法记得标 `[HttpPost]`** |
| 注册领域服务 | `Extensions/ServiceCollectionExtensions.cs` 的 `AddSaasDomainServices`（**必须手写**） |
| 覆盖框架默认实现 | `Extensions/ServiceCollectionExtensions.cs`，用 **`services.Replace(...)`** |
| 加事件处理器 | `Application/EventHandlers/**` + `AddSaasLocalEventHandler<T>()` 登记 |
| 加缓存 | `Application/Caching/`：条目类 + `SaasCacheNames` 常量 + 失效器方法 |
| 加种子数据 | `Infrastructure/Seeders/System/**`，注意 `Order` 段与「操作→资源→权限→菜单→授权」顺序 |
| 改中间件管道 | 在模块的 `OnPreApplicationInitialization` / `OnApplicationInitialization` 里加 |
| 改启动/健康检查/Webhook | `main/XiHan.BasicApp.WebHost/XiHanBasicAppWebHostModule.cs` |
| 改数据库连接/Redis/JWT | `main/XiHan.BasicApp.WebHost/appsettings.*.json`，见 [配置参考](./configuration) |

### 前端

| 我要… | 改这里 |
| --- | --- |
| 加一个页面 | `src/views/{域}/{页}/index.vue`（路径由后端 `PageDescriptor.Component` 决定） |
| 加一个 `_core` 页面 | 视图放 `packages/views/_core/**`，**并在 `packages/router/dynamic.ts` 的 `coreComponentMap` 登记** |
| 加 API 客户端 | `src/api/modules/{域}/xxx.ts` + `xxx.types.ts`；标准 CRUD 用 `defineResource` |
| 加菜单文案 | `packages/locales/langs/{zh-CN,en-US}/menu.ts`（键 = 后端 `I18nKey` 去掉 `menu.` 前缀） |
| 加业务文案 | `packages/locales/langs/{lang}/{模块}.ts`；**注意裸 `@` 会白屏** |
| 改列表页字段/搜索/导出 | 页面里的 `ListFieldSchema[]`（**字段单一事实源**） |
| 改表格引擎行为 | `packages/components/schema/**` |
| 改请求头/401 处理/解包 | `packages/request/index.ts` |
| 改动态路由生成 | `packages/router/dynamic.ts` |
| 改路由守卫 | `packages/router/guard.ts` |
| 改主题/暗色令牌 | `packages/design/**` 与 `src/styles/index.css` |
| 改后端地址/端口 | `frontend/.env.development` 的 `VITE_DEV_PROXY_TARGET` |

### 常见「两处都要改」

新人最容易只改一处的地方：

| 事情 | 必须同时改 |
| --- | --- |
| 加权限码 | `SaasPermissionCodes`（代码引用） **+** `SaasPermissionDefinitions`（落库） |
| 加菜单 | `PageRegistry`（后端） **+** `src/views/**` 视图文件（前端） **+** `menu.ts` 文案 |
| 加 `_core` 页面 | 视图文件 **+** `coreComponentMap` 登记 |
| 加领域服务 | 接口与实现 **+** `AddSaasDomainServices` 手写注册 |
| 加事件处理器 | 处理器类 **+** `XiHanLocalEventBusOptions.Handlers` 登记 |
| 加缓存 | 条目类 **+** 失效器方法 **+** 所有会改到该数据的写侧调用 |

漏掉右边那一半，表现基本都是**静默失效**：不报错、但功能不生效。完整清单见 [二次开发](./backend/development)。

## 相关页面

- [后端架构](./backend/introduction)：分层与装配机制
- [前端架构](./frontend/introduction)：五层结构与依赖方向
- [二次开发](./backend/development)：新增功能的端到端清单
- [配置参考](./configuration)：`appsettings` 全量
