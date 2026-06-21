# XiHan.BasicApp.CodeGeneration 架构重构设计文档

> 版本：v1（骨架阶段）· 日期：2026-06-20 · 作者：zhaifanhua
> 本文是 CodeGeneration 模块对齐 `XiHan.BasicApp.Saas` 分层架构的落地设计，并给出由"低代码（代码生成器）"演进到"零代码（运行时元数据驱动）"的路线。
> 本轮交付定位：**架构骨架 + 设计文档**。接口/DTO/服务空壳/引擎抽象已落地，**引擎内部实现与内置模板留 TODO**，供逐步填充或评审后实现。

---

## 1. 现状评估与缺口

### 1.1 现状（完成度 ≈ 20%）

| 层 | 现状 |
| --- | --- |
| Domain/Entities | ✅ 5 个实体：`SysCodeGenDataSource`、`SysCodeGenTable`、`SysCodeGenTableColumn`、`SysCodeGenTemplate`、`SysCodeGenHistory`（均继承 `BasicAppFullAuditedEntity`，多租户/软删/审计齐备） |
| Domain/Enums | ✅ 7 个枚举：`TemplateType`/`GenStatus`/`QueryType`/`HtmlType`/`GenType`/`TemplateEngine`/`DatabaseType` |
| Seeders | ✅ 资源/菜单/权限/角色权限 4 个种子（权限码 `code_gen:{read,create,update,delete,export,import,execute}`） |
| Application | ❌ **空目录** —— 无 Contracts / Dtos / AppServices / QueryServices / Mappers |
| Infrastructure | ❌ **空目录** —— 无 Repositories、无任何代码生成引擎 |
| 引擎 | ❌ **完全缺失** —— 无元数据扫描、无模板渲染、无文件产出、无打包下载 |
| 前端 | ❌ 无（菜单已埋点 `/develop/codeGen`，种子标注"前端与应用服务重建前暂不显示"） |

### 1.2 与 Saas 架构的差距

Saas 是本仓库的"黄金参照"，其分层与约定（详见 §2）已经成熟。CodeGeneration 仅有领域实体层，缺整条 Application/Infrastructure 链路与引擎。**本设计的目标即把 CodeGeneration 补齐为与 Saas 同构的完整模块，并新增一套代码生成引擎抽象。**

### 1.3 一个必须先定的现实：模板引擎

实体 `SysCodeGenTemplate.TemplateEngine` 默认值是 `Razor`，但 **框架当前只集成了 Scriban**（`XiHan.Framework.Templating` 的 `ScribanTemplateEngine` + `ITemplateService`）。Razor/T4 没有运行时实现。

**决策（已确认）**：保留 `TemplateEngine` 多引擎抽象（`ITemplateRenderer` 可扩展），**本轮只实现 Scriban 渲染通道**，Razor/T4 渲染器作为 `NotSupported` 占位 + TODO。内置模板与实体默认值后续统一调整为 `Scriban`（见 §11 决策记录 D1）。

---

## 2. 参照：Saas 分层架构约定（提炼）

> 详见 `XiHan.BasicApp.Saas`。以下是本模块直接复用的约定。

- **分层**：`Application`（编排/DTO/缓存失效/权限）→ `Domain`（实体/仓储接口/领域服务/领域规则）→ `Infrastructure`（仓储实现/引擎实现/种子）。
- **命名空间按层扁平**：文件夹仅作组织，命名空间如 `...Application.Contracts`、`...Application.Dtos`、`...Domain.Repositories`、`...Infrastructure.Repositories`，不随子目录加深。
- **CQRS 读写分离**：命令走 `IXxxAppService`（`Application/AppServices`），查询走 `IXxxQueryService`（`Application/QueryServices`）。
- **应用服务基类**：`SaasApplicationService : ApplicationServiceBase`，类级标注 `[Authorize]` + `[DynamicApi(Group, GroupName)]`。方法级标注 `[UnitOfWork(true)]`（写）+ `[PermissionAuthorize(权限码)]`。
- **DTO 约定**（基类来自 `XiHan.BasicApp.Core.Dtos`）：
  - 创建：无基类的普通类（`XxxCreateDto`）。
  - 更新：`XxxUpdateDto : BasicAppUDto`（含 `BasicId`）。
  - 状态更新：`XxxStatusUpdateDto : BasicAppDto`（含 `BasicId`）。
  - 分页查询：`XxxPageQueryDto : BasicAppPRDto`（含分页/排序）。
  - 详情：`XxxDetailDto : XxxListItemDto`（追加审计字段）。
  - 列表项：`XxxListItemDto`。
  - 分页返回：`PageResultDtoBase<T>`（`XiHan.Framework.Domain.Shared.Paging.Dtos`）。
- **仓储**：接口 `IXxxRepository : ISaasRepository<TEntity>`；实现 `XxxRepository(ISqlSugarClientResolver) : SaasRepository<TEntity>`，查询用 `CreateQueryable()`，主键字段 `BasicId`。
- **领域服务**：`IXxxDomainService`，命令入参用 `record`（`XxxCreateCommand` 等），返回 `XxxCommandResult`；承载唯一性/环路/跨租户等业务校验。
- **映射**：手写静态 `XxxApplicationMapper`（无 AutoMapper），方法 `ToCreateCommand` / `ToDetailDto` 等。
- **缓存失效**：写路径调用 `ISaasCacheInvalidator` 主动失效；查询路径 `GetOrAddAsync` + `hideErrors:true` 降级查库。
- **DynamicApi**：标注后由框架运行时生成 HTTP 控制器，**无需手写 Controller**。
- **多租户/软删/审计**：实体基类内建；唯一索引末列附 `IsDeleted`（软删后可重建同编码）。

---

## 3. 目标架构与模块定位

CodeGeneration 在 Saas 之上新增"**元数据驱动的代码产出**"能力，定位为**开发期工具模块**（非业务运行时主链路）。整体落在三条主线：

1. **配置面（CRUD）**：数据源 / 表配置 / 列配置 / 模板 / 历史的标准增删改查 —— 完全复用 Saas 约定（§2）。
2. **引擎面（Generation）**：DbFirst 元数据扫描 → 配置建模 → 模板渲染 → 产物（预览/Zip/落盘）→ 历史留痕 —— 本模块**新增的核心抽象**（§5）。
3. **演进面（零代码）**：同一套配置元数据，未来增加"运行时解释执行"消费方式（动态实体/动态 API/动态表单），不产出代码文件（§9）。

```
┌─────────────────────────────────────────────────────────────┐
│ Application（编排/DTO/权限/DynamicApi 自动暴露）              │
│  ├─ AppServices(命令)   ├─ QueryServices(查询)               │
│  ├─ 编排服务 CodeGenerationAppService（预览/生成/下载/导入）  │
│  └─ Contracts / Dtos / Mappers                              │
├─────────────────────────────────────────────────────────────┤
│ Domain                                                       │
│  ├─ Entities(已有 5 实体) / Enums(已有)                      │
│  ├─ Repositories(接口) / DomainServices(业务规则)            │
│  ├─ Permissions(权限码常量)                                  │
│  └─ Generation/Abstractions（引擎契约：渲染/扫描/类型映射/打包/编排）│
├─────────────────────────────────────────────────────────────┤
│ Infrastructure                                               │
│  ├─ Repositories(实现：SaasRepository<T>)                    │
│  └─ Generation（引擎实现：Scriban 渲染 + DbFirst 扫描 + 类型映射 + Zip 打包 + 编排）│
├─────────────────────────────────────────────────────────────┤
│ 复用 Framework：ITemplateService(Scriban) / IDatabaseMetadataProvider(DbFirst) │
│                VirtualFileSystem(内置模板) / DynamicApi / SqlSugar 仓储  │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. 完整文件地图（目标）

> 标记：✅ 本轮已建　🟡 本轮建骨架/空壳（含 TODO）　⬜ 后续阶段

```text
XiHan.BasicApp.CodeGeneration/
├─ Docs/
│   └─ CodeGeneration-Architecture.md                 ✅ 本文
├─ Domain/
│   ├─ Entities/                                       ✅ 已有 5 实体
│   ├─ Enums/CodeGenEnums.cs                           ✅ 已有
│   ├─ Permissions/
│   │   └─ CodeGenPermissionCodes.cs                   ✅ 权限码常量（对齐种子）
│   ├─ Repositories/                                   ✅ 5 仓储接口
│   │   ├─ ICodeGenDataSourceRepository.cs
│   │   ├─ ICodeGenTableRepository.cs
│   │   ├─ ICodeGenTableColumnRepository.cs
│   │   ├─ ICodeGenTemplateRepository.cs
│   │   └─ ICodeGenHistoryRepository.cs
│   ├─ DomainServices/                                 ⬜ 业务规则（唯一性/引用校验/列同步）后续
│   └─ Generation/                                     ✅ 引擎抽象（核心）
│       ├─ CodeGenerationModels.cs                     ✅ 上下文/表模型/产物/结果 records
│       ├─ ITemplateRenderer.cs                        ✅ 单引擎渲染契约
│       ├─ ITemplateRendererResolver.cs               ✅ 按 TemplateEngine 选择渲染器
│       ├─ ITypeMappingProvider.cs                     ✅ DB→C#→TS 类型映射
│       ├─ IDatabaseSchemaImporter.cs                  ✅ DbFirst 元数据→表/列配置
│       ├─ IGeneratedArtifactPackager.cs              ✅ 产物打包（Zip）
│       └─ ICodeGenerationEngine.cs                    ✅ 引擎编排入口
├─ Application/
│   ├─ Abstractions/
│   │   └─ CodeGenerationApplicationService.cs         ✅ 应用服务基类（DynamicApi 组）
│   ├─ Contracts/                                      ✅ 接口（命令+查询+编排）
│   │   ├─ CodeGenDataSourceContracts.cs
│   │   ├─ CodeGenTableContracts.cs
│   │   ├─ CodeGenTableColumnContracts.cs
│   │   ├─ CodeGenTemplateContracts.cs
│   │   ├─ CodeGenHistoryContracts.cs
│   │   └─ ICodeGenerationAppService.cs（编排：导入/预览/生成/下载）
│   ├─ Dtos/                                           ✅ 分组 DTO
│   │   ├─ CodeGenDataSourceDtos.cs
│   │   ├─ CodeGenTableDtos.cs
│   │   ├─ CodeGenTableColumnDtos.cs
│   │   ├─ CodeGenTemplateDtos.cs
│   │   ├─ CodeGenHistoryDtos.cs
│   │   └─ CodeGenerationDtos.cs（导入/预览/生成 I/O）
│   ├─ Mappers/                                        🟡 Template 示范 + 其余 TODO
│   │   └─ CodeGenTemplateApplicationMapper.cs
│   ├─ AppServices/                                    🟡 命令服务空壳（throw TODO）
│   │   ├─ CodeGenDataSourceAppService.cs
│   │   ├─ CodeGenTableAppService.cs
│   │   ├─ CodeGenTableColumnAppService.cs
│   │   ├─ CodeGenTemplateAppService.cs
│   │   └─ CodeGenerationAppService.cs（编排，已接入引擎）
│   └─ QueryServices/                                  🟡 查询服务空壳（throw TODO）
│       ├─ CodeGenDataSourceQueryService.cs
│       ├─ CodeGenTableQueryService.cs
│       ├─ CodeGenTableColumnQueryService.cs
│       ├─ CodeGenTemplateQueryService.cs
│       └─ CodeGenHistoryQueryService.cs
├─ Infrastructure/
│   ├─ Repositories/                                   ✅ 5 仓储实现
│   │   └─ CodeGen*Repository.cs ×5
│   └─ Generation/                                     🟡 引擎实现（Scriban 通道实，其余 TODO）
│       ├─ ScribanTemplateRenderer.cs                  🟡 接 ITemplateService（可用）
│       ├─ RazorTemplateRenderer.cs                    🟡 NotSupported 占位
│       ├─ TemplateRendererResolver.cs                 ✅ 引擎选择
│       ├─ DatabaseSchemaImporter.cs                   🟡 接 IDatabaseMetadataProvider
│       ├─ DefaultTypeMappingProvider.cs               🟡 基础类型映射表
│       ├─ ZipArtifactPackager.cs                      ✅ System.IO.Compression 打包
│       └─ CodeGenerationEngine.cs                     🟡 编排管线（步骤 TODO）
├─ Seeders/                                            ✅ 已有 + 后续补内置模板种子
├─ Extensions/ServiceCollectionExtensions.cs           ✅ 注册仓储/引擎/应用服务
└─ XiHanBasicAppCodeGenerationModule.cs                ✅ 模块装配
```

---

## 5. 代码生成引擎设计（核心）

引擎是本模块相较 Saas 的**新增抽象**，全部置于 `Domain/Generation`（契约）+ `Infrastructure/Generation`（实现），通过 DI 注入，受 `DynamicApi` 暴露的编排服务驱动。

### 5.1 管线（Pipeline）

```
[1] 导入元数据   IDatabaseSchemaImporter
      └─ 主库或 SysCodeGenDataSource 指定的外库
      └─ 复用框架 IDatabaseMetadataProvider（DbFirst：表/列/主键/可空/注释）
      └─ 产出 TableSchema/ColumnSchema → 落地为 SysCodeGenTable/SysCodeGenTableColumn 配置
                 │
[2] 配置建模     CodeGenerationContext
      └─ 合并：表配置 + 列配置 + 类型映射(ITypeMappingProvider) + 模块/命名空间/作者等选项
      └─ 形成模板可消费的强类型上下文（表名/类名/列集合/主键/树表/主子表关系）
                 │
[3] 选择模板     ITemplateRendererResolver
      └─ 按 SysCodeGenTemplate.TemplateEngine 选 ITemplateRenderer（本轮：Scriban）
      └─ 按 TemplateGroup 批量加载模板集（Entity/DTO/Repo/Service/Controller/Vue 等）
                 │
[4] 渲染         ITemplateRenderer.RenderAsync(template, context)
      └─ 文件名/路径由模板的 FileNameExpression/FilePathExpression 渲染得到
      └─ 产出 GeneratedArtifact[]（相对路径 + 内容）
                 │
[5] 产出         GenType 分流
      ├─ Preview     → 直接返回内容（前端预览/diff）
      ├─ Zip         → IGeneratedArtifactPackager 打包字节流，走文件下载
      └─ CustomPath  → 落盘到 GenPath（开发机/受控目录，需白名单与越权校验）
                 │
[6] 留痕         SysCodeGenHistory
      └─ 批次号/耗时/文件数/总大小/产物清单/模板快照/表快照/错误信息/操作人
```

`ICodeGenerationEngine` 是该管线的统一编排入口，应用层编排服务 `CodeGenerationAppService` 仅做权限、事务、DTO 转换与历史写入，业务步骤下沉到引擎。

### 5.2 引擎契约（要点）

- `ICodeGenerationEngine`
  - `Task<GenerationResult> PreviewAsync(GenerationRequest request, ct)`
  - `Task<GenerationResult> GenerateAsync(GenerationRequest request, ct)`（含 Zip/落盘）
- `ITemplateRenderer`（单引擎）：`TemplateEngine Engine { get; }`；`Task<string> RenderAsync(string template, CodeGenerationContext ctx, ct)`；`TemplateRenderValidation Validate(string template)`
- `ITemplateRendererResolver`：`ITemplateRenderer Resolve(TemplateEngine engine)`
- `IDatabaseSchemaImporter`：`Task<TableSchema?> ImportTableAsync(string table, string? dataSource, ct)`；`Task<IReadOnlyList<string>> ListTablesAsync(string? dataSource, ct)`
- `ITypeMappingProvider`：`ColumnTypeMapping Map(DatabaseType db, string dbType)`（→ C# 类型 + TS 类型 + 默认 HtmlType/QueryType）
- `IGeneratedArtifactPackager`：`Task<byte[]> PackAsync(IEnumerable<GeneratedArtifact> files, ct)`

### 5.3 与框架基础设施的接驳

| 引擎能力 | 复用的框架设施 | 说明 |
| --- | --- | --- |
| 元数据扫描 | `IDatabaseMetadataProvider`（`XiHan.Framework.Data.SqlSugar.Metadata`） | DbFirst 读表/列；多连接 `connectionConfigId` |
| 模板渲染 | `ITemplateService` / `ScribanTemplateEngine`（`XiHan.Framework.Templating`） | Scriban 渲染 + 语法校验 |
| 内置模板存放 | `VirtualFileSystem`（嵌入式资源） | 内置模板随程序集分发，运行时加载 |
| HTTP 暴露 | `DynamicApi` | 编排服务自动成 API，无需手写 Controller |
| 数据访问 | `SaasRepository<T>` / SqlSugar | 配置 CRUD 复用 Saas 仓储基类 |

> 以上设施均已通过传递引用（CodeGeneration → Saas → … → Core，Core 已 PackageReference 全部框架包）可用，**无需修改 csproj**。

---

## 6. 类型映射体系

`ITypeMappingProvider` 把数据库列类型翻译为多目标类型，是"DbFirst → 全栈代码"的关键。骨架内置一张基础映射表（`DefaultTypeMappingProvider`），覆盖常见 MySQL/SqlServer/PostgreSQL 类型；可后续做成数据驱动（字典/配置表）以支持自定义。

| DB 类型（示例） | C# 类型 | TS 类型 | 默认 HtmlType | 默认 QueryType |
| --- | --- | --- | --- | --- |
| bigint / int | long / int | number | InputNumber | Equal |
| varchar / text | string | string | Input / Textarea | Like |
| datetime / timestamp | DateTimeOffset | string | DateTimePicker | Between |
| bit / boolean | bool | boolean | Switch | Equal |
| decimal / numeric | decimal | number | InputNumber | Between |

---

## 7. 权限、多租户、缓存、API 对齐

- **权限码**：沿用种子既有 `code_gen:{read,create,update,delete,export,import,execute}`，集中为常量 `CodeGenPermissionCodes`，方法级 `[PermissionAuthorize(...)]` 引用。生成/导入用 `execute`/`import`，下载用 `export`。
- **多租户**：所有实体已多租户隔离；引擎产物与历史按租户隔离；外部数据源（`SysCodeGenDataSource`）连接串加密存储，读写走加密适配器。
- **缓存**：模板按 `TemplateGroup` 高频读取，可走 `GetOrAddAsync` 缓存 + 写路径失效（参照 Saas `ISaasCacheInvalidator` 模式，后续接入）。
- **DynamicApi 分组**：基类标注 `[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务")]`，与 Saas 组区隔。

---

## 8. 安全要点（务必在引擎实现阶段落实）

1. **落盘越权**：`GenType.CustomPath` 必须路径白名单 + 规范化校验，禁止 `..` 穿越；生产环境默认禁用落盘，仅允许 Zip/Preview。
2. **外库连接串**：加密存储与传输，测试连接成功方可保存；扫描使用只读账号。
3. **模板即代码**：Scriban 模板可执行表达式，模板编辑须 `import` 权限 + 审核；渲染设超时（框架 `RenderTimeout`）与大小上限。
4. **多租户产物隔离**：历史与下载令牌绑定租户与操作人，防越权下载他人产物。

---

## 9. 低代码 → 零代码 演进路线（一套元数据，两种消费）

核心思想：**`SysCodeGenTable` + `SysCodeGenTableColumn` 是"实体/表单/查询"的元数据**。低代码把它编译成代码文件；零代码把它在运行时解释执行。两者共享同一份配置，互不重写。

```
            ┌────────────── 同一套配置元数据 ──────────────┐
            │  SysCodeGenTable / SysCodeGenTableColumn      │
            │  （含 HtmlType/QueryType/校验/字典/关系）     │
            └───────────────┬───────────────┬──────────────┘
                            │               │
              低代码（本轮）│               │ 零代码（后续阶段）
                            ▼               ▼
            生成可编译代码文件        运行时解释执行
            Entity/DTO/Repo/Service   ├─ 动态实体/动态建表（SqlSugar CodeFirst 动态类型）
            /Controller/Vue 页面      ├─ 动态 API（DynamicApi + 通用 CRUD 端点 + 元数据校验）
            → 二次开发                ├─ 动态表单（前端按列元数据渲染表单/表格/查询）
                                      └─ 不产代码、不重编译、配置即生效
```

### 9.1 演进阶段

- **阶段一（本轮·低代码）**：补齐 CRUD + 引擎抽象 + Scriban 渲染 + 内置全栈模板，实现"配置 → 生成代码 → 下载/落盘"。
- **阶段二（低代码增强）**：前端可视化配置（数据源/表/列/模板/预览 diff/历史）、内置模板沉淀、Razor/T4 渲染器（如确需）。
- **阶段三（零代码运行时引擎）**：新增 `Runtime` 子域：
  - 动态实体注册（基于元数据生成 SqlSugar 实体或动态表 + 通用仓储）；
  - 通用动态 API（一个受元数据驱动的 CRUD 端点族，复用 DynamicApi 与权限/多租户/FLS）；
  - 前端动态渲染器（按列元数据驱动表单/表格/查询/详情）。
- **阶段四（零代码平台化）**：在线表单设计器、流程/审批编排、规则引擎、页面编排（与既有 RBAC/ABAC/FLS、任务调度、导入导出、通知打通）。

### 9.2 框架侧需补强（零代码阶段，跨 XiHan.Framework）

> 这些属于"后续我要整个项目具备零代码能力"的框架级前置，**本轮不动框架**，仅登记为路线项：

- 动态实体/动态表：SqlSugar 动态建表与运行时实体类型缓存；
- 通用动态仓储与查询表达式（由 `QueryType` 元数据翻译为 SqlSugar 条件）；
- 元数据驱动的动态 API 约定（DynamicApi 已具运行时生成控制器能力，需要"元数据 → 端点"的约定层）；
- 动态校验/FLS：把列级校验与字段级安全接入既有 Authorization/Validation；
- 前端：元数据驱动的 schema-form / schema-table（与既有 `schema-page` 文档对齐）。

---

## 10. 分阶段实施计划与验收

| 阶段 | 范围 | 验收标准 |
| --- | --- | --- |
| **S0 骨架（已完成）** | 本文 + 接口/DTO/服务空壳 + 引擎抽象 + Scriban 渲染器 + 仓储 + DI 装配 | 编译通过；DynamicApi 可见端点；架构可评审 |
| **S1 CRUD 落地（已完成）** | 4 个聚合的领域服务 + 5 个映射 + 命令/查询实现 + 导入闭环 + 历史留痕 + DI | 数据源/表/列/模板增删改查与分页可用；导入库表→建表/列配置；生成写历史；权限/多租户/密钥脱敏生效 |
| **S2 引擎落地（已完成）** | 渲染器确定性字典绑定 + 全栈 Scriban 模板 ×11（后端 8 + 前端 3）+ 内置模板种子 | 后端实体/DTO/仓储/契约/映射/命令/查询 + 前端 TS 类型/API/SchemaPage 页面，均就绪并种入（IsBuiltIn）；前后端 DynamicApi 路由对齐；**待真实表跑通验证** |
| **S3 前端管理页（已完成）** | `/develop/codeGen`（表配置/数据源/模板/历史 四标签 + 导入/列配置/生成下载弹窗） | API 层 ×13 + 页面 ×9（type-check/eslint/oxlint 全过）；菜单已解除隐藏；**待真实环境联调** |
| **S4 零代码运行时** | 框架动态实体/动态 API/动态表单 + 运行时子域 | 配置即生效的运行时 CRUD（不产代码） |

---

## 11. 决策记录（ADR 摘要）

- **D1 模板引擎**：保留多引擎抽象，本轮仅实现 Scriban；Razor/T4 占位 + TODO。内置模板与 `SysCodeGenTemplate.TemplateEngine` 默认值后续统一为 `Scriban`（避免默认 Razor 与现实不符）。
- **D2 引擎位置**：引擎契约置于 `Domain/Generation`（领域语义：代码产出是本模块的核心领域），实现置于 `Infrastructure/Generation`。
- **D3 不改 csproj**：Templating/Data 经 Core 传递引用已可用。
- **D4 不引入 AutoMapper**：与 Saas 一致，手写静态 Mapper。
- **D5 落盘默认禁用**：生产仅 Zip/Preview，落盘需白名单（安全 §8）。
- **D6 零代码复用同一元数据**：不另起一套 schema，降低双维护成本（§9）。

---

## 12. 交付清单

### S0 架构骨架（已完成）
- [x] 设计文档（本文）
- [x] 引擎抽象（`Domain/Generation`：模型 + 6 契约）
- [x] 引擎实现骨架（`Infrastructure/Generation`：Scriban 渲染器接通、解析器、扫描器、类型映射、Zip 打包、编排管线）
- [x] 权限码常量（`Domain/Permissions/CodeGenPermissionCodes`）
- [x] 仓储接口 ×5 + 实现 ×5
- [x] 应用服务基类 + Contracts（命令/查询/编排）+ 分组 DTO + DI 装配

### S1 CRUD 落地（已完成）
- [x] 领域服务 ×4（数据源/表/列/模板）：唯一性、唯一默认源、级联软删、内置模板保护、密钥 AES 加密
- [x] 应用层映射器 ×5（命令模式手写静态 Mapper）
- [x] 命令应用服务 ×4 + 查询应用服务 ×5（替换空壳，分页用 `QueryConditions`）
- [x] 导入闭环：`ImportTableAsync`（扫描→类型映射建表/列配置→落库→返回详情）
- [x] 历史留痕：`GenerateAsync` 写 `SysCodeGenHistory`（批次/耗时/文件数/操作人）
- [x] 领域服务 DI 显式注册（`AddCodeGenerationDomainServices`）
- 已知约束（见各实现 risks）：密钥用固定口令 AES（at-rest，非 KMS）；连接测试依赖目标库 ADO 驱动已部署；OperatorIp 暂未采集

### S2 引擎落地（已完成）
- [x] 渲染器 `ScribanTemplateRenderer` 确定性字典绑定（字典重载，PascalCase 键，规避 Scriban 成员重命名）+ `IsBaseColumn` 过滤 + `ClassNameCamel/Kebab`、列 `TsProperty`（camelCase）+ Camelize/Kebabize
- [x] 后端全栈模板 ×8：Entity / Dtos / IRepository / Repository / Contracts / Mapper / AppService / QueryService（`Templates/Backend/*.sbn`）；命令/查询方法实体限定命名 → DynamicApi 路由可预测
- [x] 前端全栈模板 ×3：Types（TS DTO）/ Api（双客户端 + 动作名对齐后端路由）/ Page（SchemaPage 列表页 + 弹窗表单，直连模块路径开箱即用）（`Templates/Frontend/*.sbn`）
- [x] 内置模板种子 `SysCodeGenTemplateSeeder`（Order=34，BuiltInTemplate 带 FileExtension/FilePathExpression，按资源后缀读取，兼容 Backend/Frontend）+ 双 csproj（release + Local）EmbeddedResource + DI
- 已知局限：Between 列暂退化为等值过滤；业务列按基元类型假设（枚举列需补 using/字典）；唯一/业务索引不自动推断；状态列无 updateStatus 分支；生成前端需在目标库存在对应业务表
- ⚠ 待验证：模板不可本地执行、生成代码不可本地编译，需在服务器对一张真实表跑一次生成确认（导入→预览→生成→检查产物可编译）

### S3 前端管理页（已完成）
- [x] API 层 `src/api/modules/codegen/*`（×13：数据源/表/列/模板/历史/编排 类型+模块），裸 `createDynamicApiClient` 直调真实路由（与后端 DynamicApi 动作名逐一对齐），登记进 `@/api` 桶
- [x] 管理页 `src/views/develop/code-gen/*`（×9）：index.vue 四标签（表配置/数据源/模板/历史）+ 导入表/列配置/生成下载/表编辑 弹窗；SchemaPage/NDataTable 列表，zip 经 Base64→Blob 下载
- [x] 菜单解除隐藏（`SysMenuSeeder` 翻转 develop/code_gen 为可见+启用）
- [x] 前端验证：`vue-tsc --noEmit` 整项目 0 错误；eslint + oxlint 限定 codegen 目录 0 错误
- ⚠ 待真实环境联调（后端部署 + 前端构建后，走通 导入→配置→预览→生成下载）

### 待办
- [ ] 真实表生成验证（服务器，端到端联调）
- [ ] 零代码运行时（S4）
