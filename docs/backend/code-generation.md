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

一次生成铺开**后端 16 件 + 前端 7 件**的整套 CRUD，均为内置模板（`IsBuiltIn=true`，分组 `backend-crud` / `frontend-crud`）。

除页面 `index.vue` 外，每个产物都成对登记：**自动侧**重新生成时整体覆盖，**手动侧**仅首次创建、此后永不触碰。二者在语言层面拼接——C# 数据类经 `partial` 合并，行为类经抽象基类与派生类继承，前端经 re-export / transform 组合。所以改表结构重新生成不会冲掉手写实现。

| 模板编码 | 自动侧（每次覆盖） | 手动侧（仅首次创建） |
| --- | --- | --- |
| `backend.entity` | <code v-pre>{{ ClassName }}.Generated.cs</code> | <code v-pre>{{ ClassName }}.cs</code> |
| `backend.dtos` | <code v-pre>{{ ClassName }}Dtos.Generated.cs</code> | <code v-pre>{{ ClassName }}Dtos.cs</code> |
| `backend.irepository` | <code v-pre>I{{ ClassName }}Repository.Generated.cs</code> | <code v-pre>I{{ ClassName }}Repository.cs</code> |
| `backend.repository` | <code v-pre>{{ ClassName }}Repository.Generated.cs</code> | <code v-pre>{{ ClassName }}Repository.cs</code> |
| `backend.contracts` | <code v-pre>I{{ ClassName }}Contracts.Generated.cs</code> | <code v-pre>I{{ ClassName }}Contracts.cs</code> |
| `backend.mapper` | <code v-pre>{{ ClassName }}ApplicationMapper.Generated.cs</code> | <code v-pre>{{ ClassName }}ApplicationMapper.cs</code> |
| `backend.appservice` | <code v-pre>{{ ClassName }}AppServiceBase.Generated.cs</code> | <code v-pre>{{ ClassName }}AppService.cs</code> |
| `backend.queryservice` | <code v-pre>{{ ClassName }}QueryServiceBase.Generated.cs</code> | <code v-pre>{{ ClassName }}QueryService.cs</code> |
| `frontend.types` | <code v-pre>{{ ClassNameKebab }}.types.generated.ts</code> | <code v-pre>{{ ClassNameKebab }}.types.ts</code> |
| `frontend.api` | <code v-pre>{{ ClassNameKebab }}.generated.ts</code> | <code v-pre>{{ ClassNameKebab }}.ts</code> |
| `frontend.schema` | <code v-pre>{{ ClassNameKebab }}.schema.generated.ts</code> | <code v-pre>{{ ClassNameKebab }}.schema.ts</code> |
| `frontend.page` | —— | `index.vue` |

页面与 schema 按 `TemplateType` 分化，各自另有一套登记：单表 `frontend.schema` / `frontend.page`，主子表 `frontend.schema.masterdetail` / `frontend.page.masterdetail`，树表 `frontend.schema.tree` / `frontend.page.tree`。产物文件名相同，一张表只会命中其中一套。

前端产物落到 `src/api/modules/<module>/` 与 `src/views/<module>/<class-kebab>/`（路径表达式里 `ModuleName` 会 `string.downcase`）。生成的页面与手写页面同构：`SchemaPage` 驱动列表与搜索，`XEditModal` + `XhFormRoot` 承载表单弹窗，控件取 `~/components` 的 `XInput` / `XSelect` / `XNumberInput` / `XTreeSelect`，提示走 `~/composables` 的 `toast`，枚举下拉走 `useEnumOptions`。文案为中文字面量，接 i18n 需自行替换。

除模板产物外，引擎每次还追加**二阶产物**（目录 `_GeneratedMenuPermission/`）：

- <code v-pre>{{ClassName}}PermissionCodes.cs</code>——权限码常量类（资源段取表名，`{资源}:{操作}` 两段式）。
- <code v-pre>{{ClassName}}PermissionDefinitions.cs</code>——权限定义片段。
- <code v-pre>{{ClassName}}PageRegistry.snippet.txt</code>——`PageDescriptor` / `ButtonDescriptor` 粘贴片段。
- <code v-pre>{{ClassName}}PermissionSeeder.cs</code> 与 <code v-pre>{{ClassName}}MenuSeeder.cs</code>——种子骨架。
- `README.md`——落地说明：权限码表、按钮→权限码映射、`SysMenu` 菜单规格，以及并入源码后的 Seeder / 升级脚本接线清单。

::: tip 从旧版本升级
前端模板此前产出的是 naive-ui 页面，现已整体迁到 XiHan.UI。已生成过代码的工程重新生成时：

- `{kebab}.generated.ts` 的导出由 `xxxBaseManagementApi` 改名为 `xxxBaseApi`，而 `{kebab}.ts` 与 `index.vue` 是仅首次创建的手动文件、不会被覆盖，需手工把这两处的 `xxxManagementApi` / `xxxBaseManagementApi` 改成 `xxxApi` / `xxxBaseApi`。
- 旧 `index.vue` 引用的 `naive-ui` 与旧 schema 里的 `scrollX` 在当前前端都已不存在，那些文件本来就编译不过，建议删掉后重新生成。
- bigint 列的 TS 类型由 `number` 改为 `string`（后端 `LongJsonConverter` 把 long 全部序列化为字符串）。存量表配置**不用管**：渲染期会按 C# 类型归一化，库里存着 `ts_type='number'` 也照样产出正确的产物。升级脚本 `UpdateScripts/4.0.4` 只是顺带把库里的配置刷成一致，好让列配置界面显示的类型与实际产物对得上。
:::

::: warning 日期时间列目前是文本框
纯日期列（`date`）用日期选择器，按本地年月日提交，不会因时区换算退掉一天。

而日期时间列（`datetime` / `timestamp` / `datetimeoffset`）渲染成带格式校验的文本框：组件库的 `XDatePicker` 只到日，用它承载会在编辑时把时分秒抹成本地零点。等 `XDatePicker` 补上 `show-time`（headless 层已支持 `showTime` / `timeGranularity`）再切回选择器。

时间列（`time`）同理，也是文本框 + `HH:mm(:ss)` 校验。二进制列用文本框承载 Base64，接真实上传需自行替换成上传组件。
:::

::: warning 按钮码必须先落到 PageRegistry
生成页面的行/页面操作用 `permission: '{页面码}.{按钮键}'` 门控，这是服务端下发的**按钮码**。
把 <code v-pre>{{ClassName}}PageRegistry.snippet.txt</code> 里的 `ButtonDescriptor` 条目粘进 `PageRegistry.Buttons` 之前，
这些按钮不会显示，前端 `view-permission-hygiene` 门禁也会逐条列出未登记的码并判红。
:::

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
