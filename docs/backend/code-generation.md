# 代码生成

`XiHan.BasicApp.CodeGeneration` 是一等独立模块，做**数据库优先（DbFirst）的全栈代码生成**：扫描一张已有数据库表的结构 → 落成可编辑的表/列配置 → 用 Scriban 模板渲染出**后端实体到前端页面**的整套 CRUD 代码 → 预览、打 Zip 下载或受控落盘。目标是把"加一张表就要抄一遍八个后端文件 + 三个前端文件"的重复劳动一键铺开。

## 模块全景

生成链路由几个协作角色组成，职责单一、可替换：

| 角色 | 类型 | 职责 |
| --- | --- | --- |
| 编排应用服务 | `CodeGenerationAppService` | 对外入口：权限、事务、DTO 转换、历史留痕；导入 → 预览 → 生成 → 下载 |
| 结构导入器 | `IDatabaseSchemaImporter` / `DatabaseSchemaImporter` | DbFirst 扫描库表元信息（列名/类型/可空/主键） |
| 类型映射器 | `ITypeMappingProvider` / `DefaultTypeMappingProvider` | DB 列类型 → C# 类型 / TS 类型 + 默认表单控件 / 查询方式 |
| 生成引擎 | `ICodeGenerationEngine` / `CodeGenerationEngine` | 管线编排：建模 → 选模板 → 渲染 → 产出 |
| 模板渲染器 | `ITemplateRenderer` / `ScribanTemplateRenderer` | 用**原生 Scriban** 渲染模板（见下文约定） |
| 渲染器解析器 | `ITemplateRendererResolver` | 按 `TemplateEngine` 选渲染器；当前仅 Scriban |
| 打包器 | `IGeneratedArtifactPackager` / `ZipArtifactPackager` | 产物清单 → Zip 字节流 |
| 落盘写入器 | `IGeneratedArtifactWriter` / `FileSystemArtifactWriter` | 受控落盘（默认禁用 + 白名单 + 路径穿越拒绝） |

四张配置实体（均 `BasicAppFullAuditedEntity`，软删、多租户、审计俱全）：

| 实体 | 表 | 作用 |
| --- | --- | --- |
| `SysCodeGenDataSource` | `Sys_CodeGen_DataSource` | 外部数据库连接凭证 + 连通性自检 + DbFirst 扫描，`SourceName` 全局唯一 |
| `SysCodeGenTable` | `Sys_CodeGen_Table` | 一张目标表的生成主配置，`TableName` 全局唯一 |
| `SysCodeGenTableColumn` | `Sys_CodeGen_TableColumn` | 列级配置（类型映射、表单控件、查询方式、字典三分） |
| `SysCodeGenTemplate` | `Sys_CodeGen_Template` | 模板（Scriban 正文 + 文件名/路径表达式），`TemplateCode` 全局唯一 |

生成历史另存 `SysCodeGenHistory`：每次执行生成——无论成败——都写一条留痕（批次号、耗时、文件数、总字节、操作人、失败原因）。

## 三种生成模式

由 `SysCodeGenTable.TemplateType`（枚举 `TemplateType`）决定，也是模板筛选的分组维度：

| 模式 | 枚举 | 适用场景 | 关键配置字段 |
| --- | --- | --- | --- |
| 单表 | `Single` | 扁平 CRUD（如岗位、字典项、普通业务表） | 主键列 |
| 树形 | `Tree` | 自引用层级（如菜单、部门、地区） | `TreeParentColumn`（父级字段）、`TreeNameColumn`（名称字段） |
| 主从 | `MasterDetail` | 一主多从（如订单 + 订单明细） | `MasterTableId`（主表配置）、`MasterForeignKey`（子表外键列） |

无显式指定模板编码时，引擎按表的 `TemplateType` 取该类型下的启用模板集（`GetEnabledByTypeAsync`）。模板**不按业务模块过滤**——CRUD 模板对所有模块通用，只按模板类型分组。

> 树形/主从的结构字段（父级列、名称列、主表、外键）通过 `CodeGenerationContext.Options` 透出给模板（键为 `TreeParentColumn` / `MasterForeignKey` 等），模板按 `TemplateType` 消费。当前内置模板套件以单表为主，树/主从的上下文已就绪，模板可自行扩展。

## 全栈生成：从实体到前端页面

一次生成铺开**后端 8 件 + 前端 3 件**的整套 CRUD，均为内置模板（`IsBuiltIn=true`，分组 `backend-crud` / `frontend-crud`）：

| 模板编码 | 产物 | 文件名表达式 |
| --- | --- | --- |
| `backend.entity` | 实体 | <code v-pre>{{ ClassName }}.cs</code> |
| `backend.dtos` | DTO 集 | <code v-pre>{{ ClassName }}Dtos.cs</code> |
| `backend.irepository` | 仓储接口 | <code v-pre>I{{ ClassName }}Repository.cs</code> |
| `backend.repository` | 仓储实现 | <code v-pre>{{ ClassName }}Repository.cs</code> |
| `backend.contracts` | 应用契约 | <code v-pre>I{{ ClassName }}Contracts.cs</code> |
| `backend.mapper` | 对象映射 | <code v-pre>{{ ClassName }}ApplicationMapper.cs</code> |
| `backend.appservice` | 应用服务 | <code v-pre>{{ ClassName }}AppService.cs</code> |
| `backend.queryservice` | 查询服务 | <code v-pre>{{ ClassName }}QueryService.cs</code> |
| `frontend.types` | TS 类型 | <code v-pre>{{ ClassNameKebab }}.types.ts</code> |
| `frontend.api` | 接口请求 | <code v-pre>{{ ClassNameKebab }}.ts</code> |
| `frontend.page` | 列表页 | `index.vue` |

前端产物落到 `src/api/modules/<module>/` 与 `src/views/<module>/<class-kebab>/`（路径表达式里 `ModuleName` 会 `string.downcase`）。生成的前端页直接用项目的 `SchemaPage` 组件驱动列表 + 表单弹窗，与手写页面同构。

除模板产物外，引擎每次还追加**二阶产物**（目录 `_GeneratedMenuPermission/`）：

- <code v-pre>{{ClassName}}PermissionCodes.cs</code>——权限码常量类（资源段取表名，`{资源}:{操作}` 两段式）。
- `README.md`——落地说明：权限码表、按钮→权限码映射、`SysMenu` 菜单规格，以及并入源码后的 Seeder / 升级脚本接线清单。

> 二阶产物是**待并入源码的代码片段，不是运行时写库**。这符合 BasicApp 的单一事实源 + 菜单即绑约定：把片段并入源码后，全新库由 Seeder 初始化；存量库还要把必要的数据变化纳入同版本 `UpdateScripts`。

## 数据源与表结构

### 数据源

`SysCodeGenDataSource` 管理外部数据库连接（主机/端口/库名/账号/加密密码或连接串），同时直接参与表结构扫描：

- `DatabaseType` 标注连接方言，支持 `MySql` / `SqlServer` / `PostgreSql` / `Oracle` / `Sqlite`。
- 密码/连接串经 `AesHelper` 固定口令**对称加密**存储（`CodeGenDataSourceDomainService` 的 `EncryptSecret`/`DecryptSecret`）；`TestConnectionAsync` 用一个独立探测用的 `SqlSugarClient` 开关一次连接，回写 `LastTestTime` / `LastTestResult` / `LastTestMessage`。
- 保存（`CreateAsync` / `UpdateAsync`）**不强制**先测试连接通过；删除（`DeleteAsync`）当前**未校验**是否仍有 `SysCodeGenTable` 引用，删除前应先检查表配置引用。
- 导入弹窗调用 `codeGenDataSourceApi.options()` 加载数据源下拉；空值代表本系统主库，选择项的值是 `SysCodeGenDataSource.BasicId`。
- `DatabaseSchemaImporter` 首次使用外部数据源时解密连接信息，经 `IDynamicConnectionRegistrar` 按 `DataSourceId` 动态注册 SqlSugar 连接，再调用框架 `IDatabaseMetadataProvider` 扫描。
- 数据源不存在或停用时直接失败，**不会静默回退主库**；已注册连接会复用。

导入后的 `SysCodeGenTable.DataSourceId` 会保留来源数据源，后续“同步表结构”和重新生成仍能定位同一外部库。数据源配置支持 `MySql` / `SqlServer` / `PostgreSql` / `Oracle` / `Sqlite`；这表示元数据扫描支持这些方言，不代表 BasicApp 自身的发布升级 SQL 已跨方言适配。

### 表结构导入

导入是"逆向工程"的入口，由 `CodeGenerationAppService.ImportTableAsync` 闭环完成：

1. **去重**——同一目标表禁止重复配置（`TableName` 全局唯一）。
2. **扫描结构**——`DatabaseSchemaImporter` 接通框架 `IDatabaseMetadataProvider`，只产出数据库层结构（列名/类型/可空/主键/自增/长度/小数位）。
3. **建表配置**——类名默认由表名 `Pascalize`（`sys_user` → `SysUser`），可覆盖命名空间/模块/业务名/作者。
4. **建列配置**——每列经 `ITypeMappingProvider.Map` 预填 C#/TS 类型、默认表单控件（`HtmlType`）与查询方式（`QueryType`），并写默认开关（`IsList=true` / `IsInsert=true` / `IsEdit=true` / `IsQuery=false`）。

导入器有两处贴合本仓约定的健壮处理：

- **大小写还原**：部分库（如 MySQL `lower_case_table_names=1`）返回全小写名，丢失驼峰。导入器反射已注册的 `[SugarTable]` 实体建名称目录，把 `syscodegendatasource` 还原为 `SysCodeGenDataSource`；未注册的外部表保持原样。
- **分表折叠**：带 `[SplitTable]` 的日志类实体物理表按时间分片（如 `sysdifflog_20260601`）。列表时把同实体的所有分片折叠为基础逻辑名（`SysDiffLog`）去重；导入基础名时自动扫最近一个分片取列结构。

### 字段配置

`SysCodeGenTableColumn` 是列级精细控制面，模板据此渲染。常用字段：

| 字段 | 含义 |
| --- | --- |
| `CSharpType` / `CSharpProperty` / `TsType` | 类型与属性名映射（导入预填、可手改） |
| `HtmlType` | 表单控件：`Input` / `Textarea` / `Select` / `Switch` / `DatePicker` / `InputNumber` / `TreeSelect` … |
| `QueryType` | 查询方式：`Equal` / `Like` / `Between` / `In` … |
| `IsList` / `IsInsert` / `IsEdit` / `IsQuery` | 列表显示 / 新增 / 编辑 / 查询开关 |
| `IsRequired` / `ColumnLength` / `MinValue` / `MaxValue` / `RegexPattern` | 表单校验约束 |

**字典三分**（选项列的可选项来源，由 `DictSelectorType` 决定生效字段）：

| `DictSelectorType` | 生效字段 | 含义 |
| --- | --- | --- |
| `DictSelector` | `DictCode` | 关联系统字典类型编码 |
| `EnumSelector` | `EnumTypeName` | 关联枚举全名 |
| `ConstSelector` | `ConstValues` | 内联常量项 JSON |

> **字典三分是纯表单渲染信息，不入生成的领域代码**——它只让前端页把某列渲染成对应下拉/选项，不产生任何跨表关联或外键。

## 模板：基于 Scriban，可自定义

模板存在 `SysCodeGenTemplate.TemplateContent`（BigString）。内置模板由 `SysCodeGenTemplateSeeder` 把 `Templates/Backend/*.sbn`、`Templates/Frontend/*.sbn`（编译为嵌入资源）种入库、标 `IsBuiltIn=true`；用户可新增自定义模板或改动非内置模板。

### 模板变量

引擎把 `CodeGenerationContext` 投影成 PascalCase 键的 Scriban 变量。顶层常用：

```text
ClassName          实体类名（如 SysProduct）
ClassNameCamel     camelCase（sysProduct）— 前端标识/API 对象名
ClassNameKebab     kebab-case（sys-product）— 前端文件名/路由
TableName          数据库表名
TableComment       表注释
Namespace / ModuleName / BusinessName / FunctionName / Author
TemplateType       枚举以名称字符串透出（"Single"/"Tree"/"MasterDetail"）
PrimaryKey         主键列（字典）
Columns            列集合（字典列表）
Options            扩展键（树/主从结构字段、ParentMenuId 等）
```

每个 `Columns` 项（字典）常用键：`ColumnName` / `ColumnComment` / `CSharpType` / `CSharpProperty` / `TsProperty`（camelCase，对应后端 camelCase JSON）/ `TsType` / `IsPrimaryKey` / `IsNullable` / `IsRequired` / `HtmlType` / `QueryType` / `DictSelectorType` / `DictCode`，以及关键的 **`IsBaseColumn`**。

> `IsBaseColumn` 标记基类 `BasicAppFullAuditedEntity` 托管的列（`BasicId` / `TenantId` / `IsDeleted` / 审计四段 / 软删三段）。模板据它跳过这些列，只生成业务属性——否则会重复声明基类已有成员。内置 `Entity.sbn` 里可见 <code v-pre>{{~ if !col.IsBaseColumn ~}}</code> 的用法。

### 文件名 / 路径表达式

模板另有两个表达式字段（本身也走 Scriban 渲染）：

- `FileNameExpression`——输出文件名，如 <code v-pre>{{ ClassName }}Dtos.cs</code>；渲染失败或为空时回退 `ClassName` + `FileExtension`。
- `FilePathExpression`——输出目录（相对路径），拼在文件名前；渲染失败回退无目录输出。

### 约定 ①：生成代码不焊外键关联

**生成的代码不建立任何物理/对象层外键关联**——没有 SqlSugar `Navigate` 导航属性、没有 LEFT JOIN、没有物理外键、没有跨表"显示属性"。跨表关联一律由业务层手写。上文的字典三分（`DictSelector` / `EnumSelector` / `ConstSelector`）保留，但它只是**表单选项来源**，同样不入生成代码。这与代码生成器的既定方向一致：生成物保持自包含、无隐式耦合，关联关系交给人显式表达。

### 约定 ②：用原生 Scriban，而非框架 `ITemplateService`

渲染由 `ScribanTemplateRenderer` 直接用**原生 Scriban** 完成：`Template.Parse(...)` 解析、`ScriptObject` 注入变量、`TemplateContext` 渲染，并关闭成员重命名（`MemberRenamer = member => member.Name`），使模板以确定的 PascalCase 访问变量。

它**刻意不走**框架的 `ITemplateService`——后者对 `string` 的默认引擎是**简单替换引擎，不解析 Scriban 语法**（<code v-pre>{{ }}</code>、`for`、`if`），会把模板原样输出。要真正跑 Scriban 语法就必须绕开它、用原生 Scriban。这一点在 `ScribanTemplateRenderer` 的注释里有明确说明。渲染前可用 `Validate` 做语法校验（`Template.Parse` 报错即返回 `TemplateRenderValidation.Invalid`）。

> 枚举移除了 Razor（需运行时编译、框架不支持，避免"选了报错"的伪能力）；`T4` 在枚举中保留占位，但解析器目前只注册了 Scriban，选其它引擎会抛 `NotSupportedException`。

## 生成流程

对外方法（`CodeGenerationAppService`，经 `[DynamicApi]` 暴露，分组 `BasicApp.CodeGen`）与权限：

| 步骤 | 方法 | 权限码 |
| --- | --- | --- |
| 列库表 | `ListDatabaseTablesAsync` | `code_gen:read` |
| 导入表结构 | `ImportTableAsync` | `code_gen:import` |
| 预览 | `PreviewAsync` | `code_gen:read` |
| 执行生成 | `GenerateAsync` | `code_gen:execute` |

端到端流程：

```text
填写 ConnectionConfigId（框架已注册连接，留空用主库）
  → 列出库表、导入目标表         [ImportTableAsync]
      · 扫结构 + 类型映射 → 落表/列配置
  → 调整表配置（模板类型/命名空间/模块）与列配置（控件/查询/字典三分）
  → 预览                          [PreviewAsync → GenType.Preview]
      · 建 CodeGenerationContext → 逐模板渲染 → 返回产物清单（含内容）
  → 执行生成                      [GenerateAsync]
      · 同渲染核心，再按 GenType 分流产出
      · 无论成败写一条 SysCodeGenHistory 留痕
```

生成方式由 `GenType` 决定：

| `GenType` | 行为 |
| --- | --- |
| `Preview` | 只返回产物清单（含文件内容），不打包不落盘 |
| `Zip` | 打成 Zip，包体以 **Base64** 随 `CodeGenResultDto.PackageBase64` 返回，前端触发下载 |
| `CustomPath` | **受控落盘**到 `SysCodeGenTable.GenPath` |

### 落盘的安全策略（fail-closed）

`CustomPath` 落盘由 `FileSystemArtifactWriter` 把关，绑定配置节 `CodeGeneration`（`CodeGenerationOptions`），**默认禁用**，任一条件不满足即拒绝：

- `EnableCustomPathDisk=false`（默认）→ 拒绝。
- `AllowedRootPaths` 为空 → 拒绝。
- 目标路径不在白名单根目录内 → 拒绝。
- 产物相对路径是绝对路径 / 带盘符 / 拼接后越界（`..` 逃逸）→ 拒绝。

即"默认禁用 + 白名单根目录 + 路径穿越二次校验"，符合本仓 fail-closed 约定。生产要落盘须显式开启并配置白名单。

## 零代码运行时（只读）

`DynamicRuntimeAppService` 提供一条与"生成代码"平行的轻量路径：给定一张**已配置且启用**的 `SysCodeGenTable`，不生成/不编译任何实体代码，直接按其列配置解释执行：

| 方法 | 行为 | 权限码 |
| --- | --- | --- |
| `GetSchemaAsync` | 按 `SysCodeGenTableColumn` 投影字段 schema（属性名、标签、`TsType`/`HtmlType`/`QueryType`、列表/查询/必填开关） | `code_gen:read` |
| `GetPageAsync` | 用 `ISqlSugarClientResolver.GetCurrentClient()` 对表名做动态分页查询（`Queryable<Dictionary<string, object>>().AS(tableName)`） | `code_gen:read` |

表名只取自已配置且启用（`Status = Enabled`）的 `SysCodeGenTable` 记录，从不直接拼接用户传入的表名字符串，因此没有 SQL 注入面；未启用的表配置访问会抛友好异常。当前只做 schema + 列表（只读），写入/DDL 未开放。前端"查看运行时数据"弹窗（表格行操作）即消费这两个接口，适合在正式生成代码前先验证列配置是否符合预期。

## 扩展与二次开发

- **加一种数据库方言**：扩展 `ITypeMappingProvider` 的映射；扫描能力依赖框架 `IDatabaseMetadataProvider`。
- **加/改模板**：新增 `SysCodeGenTemplate`（自定义编码、Scriban 正文、文件名/路径表达式），或改动非内置模板；用模板变量表与 `IsBaseColumn` 约定编写。
- **换渲染引擎**：实现 `ITemplateRenderer`（`Engine` 返回对应 `TemplateEngine`）并注册，`TemplateRendererResolver` 后注册覆盖先注册。
- **生成后并入源码**：按 `_GeneratedMenuPermission/README.md` 的步骤把权限码常量、种子（资源→权限→菜单→授权，Order 用 200+ 段）并入模块；全新库由种子初始化，存量库还要补对应版本的前向升级脚本。

## 下一步

- [框架 · 模板模块](https://framework.docs.xihanfun.com/packages/templating)：框架 `ITemplateService` 的定位与简单替换引擎（为何代码生成绕开它用原生 Scriban）。
- [框架 · 动态 API](https://framework.docs.xihanfun.com/guide/dynamic-api)：`[DynamicApi]` 如何把 `CodeGenerationAppService` 暴露为 REST。
- [权限模型](./permission)：`code_gen:*` 权限码、菜单即绑与二阶产物落地的背景。
- [系统架构](./introduction)：模块在启动聚合中的装配位置。
