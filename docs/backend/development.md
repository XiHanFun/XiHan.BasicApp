# 二次开发

本页是 BasicApp 最重实操的一页：**在既有代码库上扩展新功能时，究竟要新建/改哪些文件、在哪些点接线**。所有清单与接线点均对照真实源码复核（以「岗位 / Position」这一现成的扁平 CRUD 纵切片、以及 `XiHan.BasicApp.AI` 独立模块为样板）。

> 先读懂 [系统架构](./introduction)（分层与模块装配）与 [权限模型](./permission)（权限码 / 数据范围 / FLS）再动手，事半功倍。

## 两种扩展粒度

| 粒度 | 何时用 | 落点 | 样板 |
| --- | --- | --- | --- |
| **配方 A：Saas 功能纵切片** | 在核心业务域里加一个扁平 CRUD 实体（岗位、标签、分类…），复用 Saas 现成基础设施 | `modules/XiHan.BasicApp.Saas` 内新增文件 | 岗位 `Position` |
| **配方 B：独立一等模块** | 加一个完整功能域（代码生成、AI…），自成项目、独立种子/权限命名空间 | `modules/XiHan.BasicApp.<Name>` 新建工程 + 双边接线 | `XiHan.BasicApp.AI` / `CodeGeneration` |
| **配方 C：仅前端页面** | 后端已有接口，只补一个视图 | `frontend/src/views/**` + `PageRegistry` | 见下 |

**判断准则**：功能是否共享 Saas 的 RBAC 表、`SaasRepository`、Data Protection 密文前缀？是且体量小 → 配方 A；是独立大域、想要独立的权限/种子 `Order` 段与项目边界 → 配方 B。`CodeGeneration` / `AI` / `Workflow` 三个模块彼此不直接依赖，均以 Saas 为共享基座。

## DDD 分层与命名约定

每个业务模块内部统一分 **Domain / Application / Infrastructure** 三层。以岗位纵切片为例，各层文件与命名如下（均为真实文件）：

| 层 | 目录 | 类型 | 命名约定 |
| --- | --- | --- | --- |
| Domain | `Domain/Entities` | `SysPosition`（`partial`，继承 `BasicAppFullAuditedEntity`） | 实体 `Sys{名}`，表名 `Sys_{名}`（`SugarTable`） |
| Domain | `Domain/Repositories/Organization` | `IPositionRepository : ISaasRepository<SysPosition>` | 仓储接口 `I{名}Repository` |
| Domain | `Domain/DomainServices/Organization` | `IPositionDomainService` + `Implementations/PositionDomainService` + `PositionCommandModels`（命令 record） | 领域服务 `I{名}DomainService`；命令 `{名}{动作}Command` |
| Application | `Application/Contracts/Organization` | `IPositionAppService : IApplicationService`、`IPositionQueryService : IApplicationService` | 写侧接口 `I{名}AppService`、读侧 `I{名}QueryService` |
| Application | `Application/Dtos/Organization` | `PositionDtos`（`Create`/`Update`/`StatusUpdate`/`ListItem`/`Detail`/`PageQuery`） | DTO `{名}{用途}Dto` |
| Application | `Application/Mappers/Organization` | `PositionApplicationMapper`（静态） | `{名}ApplicationMapper` |
| Application | `Application/AppServices/Organization` | `PositionAppService`（写侧命令，`[DynamicApi]`） | 继承 `SaasApplicationService` |
| Application | `Application/QueryServices/Organization` | `PositionQueryService`（读侧投影，`[DynamicApi]`） | 继承 `SaasApplicationService` |
| Infrastructure | `Infrastructure/Repositories/Organization` | `PositionRepository : SaasRepository<SysPosition>` | 实现 `{名}Repository` |

> **CQRS 落地**：写侧 `*AppService`（`[UnitOfWork]` + `[PermissionAuthorize]`）调领域服务改状态；读侧 `*QueryService` 直接投影、多带缓存。两者都用 `[DynamicApi]` 暴露，无 Controller。

---

## 配方 A：加一个 Saas 功能（扁平 CRUD 纵切片）

以「岗位」为完整样板。分**新增文件**与**接线点**两部分。

### 新增文件

| # | 文件（`modules/XiHan.BasicApp.Saas/` 下） | 内容 |
| --- | --- | --- |
| 1 | `Domain/Entities/SysPosition.cs` | 实体：`[SugarTable]` + `[SugarIndex]`，继承 `BasicAppFullAuditedEntity`（自带审计/软删/`TenantId`） |
| 2 | `Domain/Repositories/Organization/IPositionRepository.cs` | 仓储接口，继承 `ISaasRepository<SysPosition>`，加业务查询（如 `ExistsCodeAsync`） |
| 3 | `Infrastructure/Repositories/Organization/PositionRepository.cs` | 仓储实现，继承 `SaasRepository<SysPosition>`（→ `IScopedDependency`，自动注册） |
| 4 | `Domain/DomainServices/Organization/PositionCommandModels.cs` | 命令 record：`PositionCreateCommand` / `UpdateCommand` / `StatusChangeCommand` / `CommandResult` |
| 5 | `Domain/DomainServices/Organization/IPositionDomainService.cs` | 领域服务接口（写侧业务规则） |
| 6 | `Domain/DomainServices/Organization/Implementations/PositionDomainService.cs` | 领域服务实现（唯一编码校验、状态流转等） |
| 7 | `Application/Dtos/Organization/PositionDtos.cs` | 全部 DTO（`Create`/`Update`/`StatusUpdate`/`ListItem`/`Detail`/`PageQuery`；分页 DTO 继承 `BasicAppPRDto`） |
| 8 | `Application/Mappers/Organization/PositionApplicationMapper.cs` | 静态映射：DTO ↔ 命令 ↔ 实体 |
| 9 | `Application/Contracts/Organization/IPositionAppService.cs` | 写侧应用服务接口，继承 `IApplicationService` |
| 10 | `Application/Contracts/Organization/IPositionQueryService.cs` | 读侧查询服务接口，继承 `IApplicationService` |
| 11 | `Application/AppServices/Organization/PositionAppService.cs` | 写侧实现（`Create`/`Update`/`UpdateStatus`/`Delete`） |
| 12 | `Application/QueryServices/Organization/PositionQueryService.cs` | 读侧实现（`GetPositionPageAsync` 标 `[HttpPost]`、`GetPositionDetailAsync`） |

前端另加 3 个文件，见[配方 C](#配方-c-加一个前端页面)。

> **应用服务 / 查询服务无需手写 DI**：它们实现 `IApplicationService`，由框架约定自动注册（瞬时）。仓储实现 `IScopedDependency`（经 `SaasRepository` 基类），也自动注册。**只有领域服务要手写登记**（见接线点 2）。

### 接线点检查清单

按顺序逐项接线，缺一项就静默失效：

#### 1. 权限码 → `SaasPermissionCodes`（唯一事实源，别内联字符串）

在 `Domain/Permissions/SaasPermissionCodes.cs` 新增一个嵌套静态类，`Group` 是资源段，每个 `const` 是 `saas:{resource}:{action}` 三段码：

```csharp
public static class Position
{
    public const string Group = "position";
    public const string Read = "saas:position:read";
    public const string Create = "saas:position:create";
    public const string Update = "saas:position:update";
    public const string Status = "saas:position:status";
    public const string Delete = "saas:position:delete";
    public const string Export = "saas:position:export";
}
```

再把这些码追加进同文件的 `All` 集合。代码里**一律引用 `SaasPermissionCodes.Position.Read`，绝不内联字符串** `"saas:position:read"`。

#### 2. 权限**定义**（落库种子）→ `SaasPermissionDefinitions`

在 `Domain/Permissions/SaasPermissionDefinitions.cs` 的 `Groups`（手写单一事实源）里加一个分组节点，写上中文组名、每条权限的显示名/描述/是否审计/排序：

```csharp
new(SaasPermissionCodes.Position.Group, "岗位",
[
    new(SaasPermissionCodes.Position.Read, "岗位查看", "查看岗位列表和详情", false, 505),
    new(SaasPermissionCodes.Position.Create, "岗位创建", "创建当前租户岗位", true, 506),
    // …Update / Status / Delete / Export
]),
```

落库扁平表 `All`、组码→组名 `GroupNames`、`ModuleCode`/`Tags`/`Priority` 全部**自动派生**，无需手写。`SaasPermissionSeeder`（`Order=20`）据此播种 `SysPermission`。

#### 3. 菜单 + 按钮 → `PageRegistry`（建菜单即绑权限）

在 `Application/Pages/PageRegistry.cs` 里：

- `All` 加一条 `PageDescriptor`（页面），其 `PermissionCode` **直接引用 `SaasPermissionCodes.Position.Read`**——这就是「权限种子前移、建菜单即绑权限」，**不要**事后回填：

```csharp
new("identity.position", "岗位管理", "menu.identity_position", MenuType.Menu,
    "/identity/position", "IdentityPosition", "identity/position/index",
    "identity", SaasPermissionCodes.Position.Read, "lucide:briefcase", 135),
```

- `Buttons` 加页面内操作按钮，`ParentCode` 对应上面页面码，各按钮绑对应权限码：

```csharp
new("identity.position.create", "新增", "identity.position", SaasPermissionCodes.Position.Create, 1),
new("identity.position.update", "编辑", "identity.position", SaasPermissionCodes.Position.Update, 2),
// …delete / status / export
```

`SaasMenuSeeder`（`Order=25`）从 `PageRegistry.All + Buttons` 生成菜单；它先按权限码查 `SysPermission`，**查不到就跳过并告警（fail-closed）**——所以权限种子（`Order=20`）必须排在菜单种子（`Order=25`）之前，天然满足。父目录必须排在子项之前（种子依顺序解析 `ParentId`）。

`Component`（`identity/position/index`）= `Path` 去前导斜杠 + `/index`，与前端 `src/views` 目录一一对应。`_core` 页面例外（见配方 C）。

#### 4. 领域服务 → 手写 DI（`ServiceCollectionExtensions`）

领域服务接口**不带 DI 标记接口**，框架不会自动注册。必须在 `Extensions/ServiceCollectionExtensions.cs` 的 `AddSaasDomainServices` 里显式登记（依赖仓储 → `Scoped`）：

```csharp
services.AddScoped<IPositionDomainService, PositionDomainService>();
```

> 遗漏此步 → 应用服务构造函数解析 `IPositionDomainService` 时抛 DI 异常。这是纵切片最常见的漏接线点。

#### 5. 动态 API 暴露 + 分页 `[HttpPost]`

应用服务/查询服务基类 `SaasApplicationService` 已打 `[Authorize]` + `[DynamicApi(Group="BasicApp.Saas", GroupName="系统SaaS服务")]`；具体服务类（如 `PositionAppService`/`PositionQueryService`）**再重复声明一次** `[Authorize]` + `[DynamicApi(...)]` 并补上 `Tag="岗位"`（按功能命名，用于 Swagger 分组）——方法即自动成为 REST 端点。约定：

- **写方法**标 `[UnitOfWork(true)]` + `[PermissionAuthorize(SaasPermissionCodes.Position.Xxx)]`。
- **路由剥离动词前缀**：框架按方法名前缀映射 HTTP 谓词——`Get/List/Query/Search/Find/Fetch/Retrieve→GET`、`Create/Add/Insert→POST`、`Update/Edit/Modify→PUT`、`Delete/Remove/Destroy→DELETE`、`Patch/PartialUpdate→PATCH`，并剥掉前缀生成资源路由。
- **分页方法必须显式标 `[HttpPost]`**：否则会被识别为 GET（方法名以 `Get` 开头）。前端把整个查询对象（含 `conditions`/`filters`/`sorts`）作 body 发送。

```csharp
[PermissionAuthorize(SaasPermissionCodes.Position.Read)]
[HttpPost]  // ← 分页必须显式补，别漏
public async Task<PageResultDtoBase<PositionListItemDto>> GetPositionPageAsync(
    PositionPageQueryDto input, CancellationToken cancellationToken = default) { … }
```

读侧还应经 `IFieldSecurityService.GuardFiltersAsync` / `GuardSortsAsync` 做 FLS 门控（剔除不可读/已脱敏字段），无有效排序时回退默认排序。

#### 6. 前端页面

见[配方 C](#配方-c-加一个前端页面)。

### 接线检查清单（速查）

| 步骤 | 文件 | 漏了会怎样 |
| --- | --- | --- |
| 权限码常量 | `SaasPermissionCodes.cs`（新嵌套类 + `All`） | 代码引用不到、内联字符串失去单一事实源 |
| 权限定义种子 | `SaasPermissionDefinitions.cs`（`Groups` 加节点） | `SysPermission` 无此码 → 菜单种子跳过该菜单 |
| 菜单 + 按钮 | `PageRegistry.cs`（`All` + `Buttons`） | 前端无菜单/无操作按钮 |
| **领域服务 DI** | `ServiceCollectionExtensions.AddSaasDomainServices` | **DI 解析异常** |
| 动态 API + 分页 `[HttpPost]` | `*AppService` / `*QueryService` | 分页方法变 GET、body 收不到查询对象 |
| 前端页 | `views/**` + `api/**` + `menu.ts` | 路由 404 / 菜单文案回退 |

---

## 配方 B：加一个独立一等模块（像 AI / CodeGeneration）

以 `XiHan.BasicApp.AI` 为样板。独立模块 = 自成项目 + 独立权限/种子命名空间 + 一个 csproj + slnx 登记 + WebHost 接线。

### 新增/接线一览

| # | 项 | 做法 |
| --- | --- | --- |
| 1 | **模块类** | `modules/XiHan.BasicApp.<Name>/XiHanBasicApp<Name>Module.cs`，继承 `XiHanModule`，`[DependsOn(typeof(XiHanBasicAppSaasModule))]`；`ConfigureServices` 调用自己的扩展方法 |
| 2 | **csproj** | `XiHan.BasicApp.<Name>.csproj`，只引 `..\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj`（框架经 Saas → Core 传递，别重复加框架引用） |
| 3 | **`ServiceCollectionExtensions`** | `Extensions/ServiceCollectionExtensions.cs`：`AddXxxDataSeeders` / `AddXxxDomainServices` / 必要的 `Replace` 覆盖 |
| 4 | **WebHost 接线** | 把 `typeof(XiHanBasicApp<Name>Module)` 加进 `XiHanBasicAppWebHostModule` 的 `[DependsOn(...)]` |
| 5 | **slnx 登记** | 在 `backend/XiHan.BasicApp.slnx` 加一个模块文件夹指向该 csproj；若你在 XiHanFun 工作区开发，仓库根的 `XiHanFun.Local.slnx` 也加一条 |
| 6 | 权限/菜单/种子 | 模块自带独立 `Order` 段（见下）与独立权限命名空间 |
| 7 | 前端页 | 同配方 C |

### 模块类（`ConfigureServices` 只调自己的扩展）

WebHost 只用 `[DependsOn]` 挂模块，**不**在 WebHost 里重复 `AddXxx`。模块自己的 `ConfigureServices`（因在依赖图内被框架调用）完成全部注册：

```csharp
[DependsOn(typeof(XiHanBasicAppSaasModule))]  // 复用 Saas 的 RBAC 表 / SaasRepository / DataProtection
public class XiHanBasicAppAIModule : XiHanModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        services.AddAIDataSeeders();     // 种子：操作→资源→权限→角色授权
        services.AddAIDomainServices();  // 领域服务：显式 AddScoped（无 DI 标记）
        services.AddAIConfigStore();     // Replace 覆盖框架默认配置源

        services.AddRAGDataSeeders();
        services.AddRAGDomainServices();
        services.AddRAG(configuration);

        services.AddAISkills();

        services.AddPromptDataSeeders();     // 提示词库：种子 209–212
        services.AddPromptDomainServices();
        services.AddPromptStore();           // Replace 覆盖框架默认提示词库

        services.AddAssistantDataSeeders();  // AI 助手：种子 213–216，并由 AiMenuSeeder(217) 统一播种本模块菜单
        services.AddAssistantDomainServices();
    }
}
```

> `AI` 模块包含四段能力：Provider 库化管理、知识库 RAG、提示词库、AI 助手。前三段各自「种子 + 领域服务 + `Replace` 覆盖框架默认存储」一套齐全，AI 助手段为「种子 + 领域服务」，`AddAISkills` 单独登记对话技能。新增能力时依样追加一段，`Order` 段落不与既有四段交叠。

### 模块 csproj（一份就够）

模块 csproj 只引 Saas，框架经 `Saas → Core` 传递依赖，**不要重复加框架引用**：

```xml
<ProjectReference Include="..\XiHan.BasicApp.Saas\XiHan.BasicApp.Saas.csproj" />
```

框架「源码 vs NuGet」的切换点只在基座 `XiHan.BasicApp.Core` 与 `XiHan.BasicApp.Web.Core`，由 `backend/props/framework.props` 的 `UseXiHanFrameworkSource` 决定，两组 `ItemGroup` 二选一：

- **未设置**（默认）：构建仓库根 `XiHanFun*.slnx` 且框架源码在位 → `ProjectReference` 走源码；其余（含 `XiHan.BasicApp.slnx`、单工程构建）→ `PackageReference` 走 NuGet。
- **显式指定**：`dotnet build -p:UseXiHanFrameworkSource=true|false`。

也就是说单独 clone `XiHan.BasicApp` 就能编译发布，工作区里用根解决方案打开即进入源码调试，模块作者无需为此改任何东西。详见[本地调试](#本地调试)。

### 用 `Replace` 而非 `TryAdd` 覆盖框架默认

模块要覆盖框架默认实现（配置源、存储等）时，**必须用 `services.Replace(...)`**：框架模块用 `TryAdd` 先注册了默认实现，你再 `TryAdd` 会被静默忽略、DB 实现永不生效。

```csharp
public static IServiceCollection AddAIConfigStore(this IServiceCollection services)
{
    // 框架 AddXiHanAI 已 TryAddSingleton 默认配置源，故此处必须 Replace
    services.Replace(ServiceDescriptor.Singleton<IAiProviderConfigStore, SaasAiProviderConfigStore>());
    return services;
}
```

### 种子 `Order` 段：模块间互不交叠

每个模块占一段互不重叠的 `Order`，链内遵循「**操作 → 资源 → 权限 → 菜单 → 角色授权**」顺序（建即绑权限码）：

| 模块 | `Order` 段 | 说明 |
| --- | --- | --- |
| Saas | 10–37 | 系统基线 10–29、演示 30–37 |
| CodeGeneration | 100–105 | — |
| AI | 200–217 | Provider 200–204、知识库 RAG 205–208、提示词库 209–212、AI 助手 213–216、菜单 217 |
| Workflow | 300–304 | 操作 300 → 资源 301 → 权限 302 → 菜单 303 → 角色授权 304 |

AI 的 `AddAIDataSeeders` 实链（`AddDataSeeder<T>()` 逐个登记）：

```csharp
services.AddDataSeeder<SysOperationSeeder>();       // 200 操作字典（权限派生前置）
services.AddDataSeeder<SysResourceSeeder>();        // 201 资源（权限派生前置）
services.AddDataSeeder<SysPermissionSeeder>();      // 202 资源 × 操作 → ai:* 权限
services.AddDataSeeder<SysRolePermissionSeeder>();  // 204 仅授超管
```

`AddRAGDataSeeders`（205–208）、`AddPromptDataSeeders`（209–212）与 `AddAssistantDataSeeders`（213–216）各自复用 AI 段的 `SysOperationSeeder`（200），链内只补「资源 → 权限 → 角色授权」三步，不重复种操作字典。本模块全部菜单由末尾的 `AiMenuSeeder`（217，`PageRegistry` 驱动）一次播种。

> 新模块选一段未用的 `Order`（如 400–）；**操作/资源种子必须排在权限种子之前**（权限由「资源 × 操作」派生）。

`XiHan.BasicApp.Workflow` 是最干净的一个独立模块样板：`ConfigureServices` 只有三行（`AddWorkflowStores` 用 `Replace` 把框架工作流的内存存储换成 SqlSugar 持久化、`AddWorkflowDataSeeders` 走完整的五步种子链、`AddWorkflowEventHandlers` 登记三个本地事件处理器），仓储与应用服务全部交给约定注册。要照着做一个新模块，读它比读 AI 模块更省力。

### 动态 API 动词/路由映射

与配方 A 相同：`[DynamicApi(Group="BasicApp.AI", GroupName="AI 服务", Tag="…")]`，动词前缀按框架 `DynamicApiConventionOptions` 映射 HTTP 谓词并剥离，分页方法显式补 `[HttpPost]`。

---

## 配方 C：加一个前端页面

前端页面由**后端 `PageRegistry` 驱动**（菜单/路由/组件路径/权限码/i18n 键都在后端登记），前端只需补落盘文件。以岗位页为例：

| # | 文件 | 内容 |
| --- | --- | --- |
| 1 | `frontend/src/views/identity/position/index.vue` | 视图（Schema 驱动列表页，`SchemaPage` + 字段单一事实源） |
| 2 | `frontend/src/api/modules/organization/position.ts` | API 客户端（动态 API，动词前缀剥离） |
| 3 | `frontend/src/api/modules/organization/position.types.ts` | 前端 DTO 类型 |
| 4 | `frontend/packages/locales/langs/zh-CN/menu.ts` + `en-US/menu.ts` | i18n 菜单文案（键 `identity_position`） |

### 路由与视图组件路径

后端 `PageDescriptor.Component`（如 `identity/position/index`）决定前端视图落点：前端约定 `Component` = `Path` 去前导斜杠 + `/index`，对应 `src/views/identity/position/index.vue`。动态路由由后端菜单数据生成，前端无需手写路由表。

### `_core` 页面后端化用 `coreComponentMap`

若页面不落在 `src/views`（个人中心、关于页等 `packages/views/_core` 下的页），`PageDescriptor.Component` 写 `_core/xxx/index`，由前端 `packages/router/dynamic.ts` 的 `coreComponentMap` 解析：

```ts
// packages/router/dynamic.ts
const coreComponentMap: Record<string, () => Promise<unknown>> = {
  '_core/about/index': () => import('~/views/_core/about/index.vue'),
  '_core/profile/index': () => import('~/views/_core/profile/index.vue'),
}
```

新增 `_core` 页要同时在 `coreComponentMap`（和 `componentAliasMap`）登记，否则动态路由匹配不到组件、回退 not-found。

### API 客户端：动态 API 动词前缀剥离

前端用 `createDynamicApiClient(控制器名)` 建客户端，URL 拼成 `/api/{控制器名}/{动作名}`；动作名是后端方法名**剥离动词前缀**后的结果（`CreatePositionAsync` → `Position`、`GetPositionPageAsync` → `PositionPage`）：

```ts
const positionQueryApi = createDynamicApiClient('PositionQuery')
const positionCommandApi = createDynamicApiClient('Position')

export const positionApi = {
  create: (input) => positionCommandApi.post('Position', input),
  update: (input) => positionCommandApi.put('Position', input),
  updateStatus: (input) => positionCommandApi.put('PositionStatus', input),
  delete: (id) => positionCommandApi.delete('Position', { id }),   // id 走查询串，不是路径段
  page: (input) => positionQueryApi.post('PositionPage', input),   // 分页 POST
}
```

::: warning id 不要拼成路径段
动态 API 的**路由段只由显式 `[FromRoute]` 参数产生**，普通参数一律落到查询串或请求体。写成 `` delete(`Position/${id}`) `` 会 404。标准 CRUD 直接用 `defineResource({ query: 'PositionQuery', command: 'Position' })` 工厂，一次生成 `page` / `detail` / `create` / `update` / `remove`，不用手拼。
:::

### i18n 键

`PageDescriptor.I18nKey` 命名为 `menu.{Code 中 . 与 - 替换为 _}`（岗位 → `menu.identity_position`）；双语文案在 `frontend/packages/locales/langs/{zh-CN,en-US}/menu.ts` 维护，键为去掉 `menu.` 前缀的 `identity_position`。

> 前端更细的约定（Schema 驱动、三级权限过滤、字段级脱敏、时区/语言头、枚举选项响应式等）见 [前端结构](../frontend/introduction)。

---

## 本地调试

后端**只有一套 csproj**，框架引用方式由 `backend/props/framework.props` 的 `UseXiHanFrameworkSource` 决定，**看你打开/构建的是哪个解决方案**：

| 解决方案 | 框架引用 | 场景 |
| --- | --- | --- |
| `backend/XiHan.BasicApp.slnx` | `PackageReference` → NuGet（版本写在 Core / Web.Core 两个基座里） | 常规开发、发布、外部克隆 |
| 仓库根 `XiHanFun.slnx` / `XiHanFun.Local.slnx` | `ProjectReference` → 同级框架源码 | 连框架源码调试 |

判定条件是 `$(SolutionName)` 以 `XiHanFun` 开头**且**框架源码在位；直接 `dotnet build` 单个 csproj（无解决方案上下文）走 NuGet。强制指定：`dotnet build -p:UseXiHanFrameworkSource=true|false`。

> **为什么以解决方案为准而不是探测目录**：源码模式下 VS 要求被 `ProjectReference` 的工程也是解决方案成员，否则设计时报 `NU1105`。`XiHan.BasicApp.slnx` 里没有、也不该有框架工程（它要能被单独克隆的人打开），所以它必须始终走 NuGet。

新增独立模块（配方 B）时只加一个 csproj，并在 `XiHan.BasicApp.slnx` 与仓库根的 `XiHanFun*.slnx` 登记。

> 后端由用户在 Linux 服务器 build / 部署；本地运行中的应用会锁 DLL，`dotnet build` 改动需部署后生效，诊断以加日志为主。

## 注意事项

以下是几个「静默失败」陷阱，改代码时务必对照：

- **本地事件处理器须显式加入 `XiHanLocalEventBusOptions.Handlers`**。事件总线只自动发现「以接口为服务类型」的注册；裸 `AddTransient<具体处理器>()` 不会被订阅、**静默失败**。用 `AddSaasLocalEventHandler<T>()` 封装（内部 `AddTransient` + `Configure<XiHanLocalEventBusOptions>(o => o.Handlers.AddIfNotContains(typeof(T)))`），并在 `AddSaasEventHandlers` 里登记。

- **覆盖框架默认实现一律用 `services.Replace(...)` 而非 `TryAdd`**。框架用 `TryAdd` 先注册默认实现（`IPermissionChecker`、`IUserStore`、`IJobStore`、`IAiProviderConfigStore`、各 Bot `*ConfigStore` 等），`TryAdd` 你的实现会被静默忽略。

- **领域服务必须手写 DI**。领域服务接口不带 `IScopedDependency`/`IDomainService` 标记，框架不自动注册；漏了 `AddScoped<I..DomainService, ..DomainService>()` → 运行期 DI 解析异常。仓储与应用/查询服务由约定自动注册，无需手写。

- **种子链缺 `SysOperation` 会致 CodeGen 静默失效**。权限由「资源 × 操作」派生，操作字典种子（`SysOperationSeeder`）必须先于权限种子；干净库若缺此段，代码生成等依赖该链的功能会静默失效。独立模块的种子链务必保持「操作 → 资源 → 权限 → 菜单 → 角色授权」完整顺序。

- **分页方法必须显式补 `[HttpPost]`**。方法名以 `Get` 开头会被默认识别为 GET；新增分页方法漏标 `[HttpPost]` → 前端 body 收不到查询对象。

- **实体变更必须补前向升级脚本**。CodeFirst 只为全新数据库建表；已有库的列、索引和数据变化写入 `WebHost/UpdateScripts/{version}.sql`，并在 PostgreSQL 副本上验证失败回滚与重复启动行为。

## 下一步

- [系统架构](./introduction)：分层、模块装配、DI 约定、缓存与失效
- [权限模型](./permission)：权限码 / RBAC / ABAC / 数据范围 / 字段级脱敏
- [前端结构](../frontend/introduction)：Schema 驱动页、三级权限过滤、i18n / 时区
- [代码生成](./code-generation)：用生成器批量产出 CRUD 纵切片
- [部署](../deployment)：升级脚本、Qdrant 前置、环境配置
- [动态 API 概念](https://framework.docs.xihanfun.com/guide/dynamic-api)：框架层动词/路由映射机制
