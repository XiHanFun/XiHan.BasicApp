# XiHan.BasicApp.CodeGeneration

## 概述
代码生成模块：以「数据库表/列配置 + 模板」为元数据，生成对齐 `XiHan.BasicApp.Saas` 分层（DDD + CQRS + 多租户）的全栈代码——后端实体、DTO、仓储、命令/查询服务，前端类型、接口、页面 schema 与页面，以及菜单与权限的接线代码。另提供按列配置直接查询业务表数据的只读运行时（零代码方向）。

## 能力
- 表配置：DbFirst 导入与同步表结构，按列推断 C#/TS 类型、控件、查询方式与字典/枚举/常量选项；可逐列调整列表、新增、编辑、查询开关、必填与唯一。
- 模板类型：单表、树表（父级列 + 显示名列）、主子表（主表明细区按需加载子表）。
- 包含操作：可裁剪新增、编辑、删除、导出、导入、状态切换、打印与对应按钮；列表与详情始终生成。不选即缺省集（增删改与导入导出），状态切换与打印须显式勾选；导入逐行调用新增接口，勾导入须同时勾新增（保存与生成两处都校验）。
- 导出：列表页本地导出 CSV，并接入导出中心异步导出——后端生成 `XxxExportProvider`（业务类型即页面码，复用查询服务分页，执行时校验导出权限），框架按约定注册，无需手工登记。
- 选项来源：枚举（后端枚举元数据，本地化标签）、系统字典（字典管理里的字典，按字典项编码存；列配置里从已有字典中选）、常量候选项。三者在表单是下拉，列表显示选项名称，搜索是下拉，导入按名称反查值。字典选择器要求列为 string，没选字典时生成直接失败。
- 已有实体：本仓库的表一般由实体自动建出。导入这类表生成时沿用那个实体，不再生成实体与实体手动文件，生成的仓储、映射、服务按实体所在命名空间引用它；表配置的类名须与实体一致，否则生成直接失败。只有外部库的表（没有实体）才生成实体，生成的实体带 `[GeneratedCode("XiHan.CodeGen", …)]` 标记，下次照常覆盖。沿用已有实体时唯一列的索引要自己加在实体上；项目里已有手写的同名仓储等文件时，生成到项目会整体拒绝，先移走它（自定义代码写进生成后的手动文件）。
- 关联（外键）：选项来源选「关联表」或「关联树」，指向另一张表配置（可以是导入进来的系统表，如部门），本列存被关联记录的主键（须为 long）。关联表要选显示列（文本列）；关联树要求目标是配好父级列的树表，显示列缺省取其名称列。生成物不焊外键、不做 JOIN：本表查询服务多一个选项接口（与列表同一个查看权限），仓储按目标实体查「主键 + 显示列（+ 上级）」；前端表单出下拉或树形下拉，列表显示名称、搜索下拉、导入按名称反查都走这份选项。选项一次全量返回，适合分类、部门这类参考数据。
- 上传：显示类型为图片上传 / 文件上传的文本列或 long 列，表单出文件引用上传控件（上传到文件中心、字段只存文件主键），列表里图片出缩略图、文件出打开入口。使用者需要文件中心的上传与查看权限（`saas:file:create` / `saas:file:read`）。二进制（byte[]）列仍按 Base64 文本承载。
- 唯一：列配置勾「唯一」后，实体带租户内唯一索引（`TenantId + 列 + IsDeleted`，软删后可重建），新增与编辑前查重，重复时提示「{列名}「值」已存在。」；空值不查重。布尔与二进制列不能设唯一，生成直接失败。已有库要自行补索引。
- 状态切换：要求表里有 `EnableStatus` 类型、勾了「列表」的状态列（多列时取名为 Status 的那列），否则生成直接失败。后端出 `Update{类名}StatusAsync`（独立的状态权限码 `{资源}:status`），前端出行内「启用/停用」与多选批量启停，按钮码 `{页面码}.status`。
- 打印：行内「打印」按钮跟查看权限走（按钮码 `{页面码}.print`），取详情后把选项列换成显示名称、图片换成可访问地址，按页面码取打印模板预览。后端产出 `XxxPrintDataSource`（编码即页面码，字段为详情里的业务列与创建时间，附示例数据），须手工在模块里 `services.RegisterPrintDataSource(XxxPrintDataSource.Definition)` 登记（模块须依赖打印模块），再到「打印模板」页新建同编码的模板。使用者另需打印模板的使用权限 `print-template:use`。
- 导入：列表页 CSV 导入，模板列即新增表单的列（不进列表的新增列以隐藏字段进导入），必填随表单；下拉按选项文本反查值，日期接受 `2026/9/30` 等常见写法并按本地时间归一。
- 生成方式：预览、生成并下载（Zip）、生成到项目（后端写进与命名空间同名的模块项目，前端写进前端工程，只在开发环境开启，见「配置」）。
- 内置模板随程序版本走：`SysCodeGenTemplateSeeder` 把 `Templates/**/*.sbn` 嵌入资源种进平台库，启动时回刷内容；改了模板需重启后端才会落到库里。

## 产物与写入策略
每个产物成对登记：自动文件（`.Generated.cs` / `.generated.ts`，重新生成总是覆盖）与手动文件（仅首次创建，之后永不触碰）。自定义代码写在手动文件里：后端经 partial 合并或继承生成基类后 override，前端在 `.schema.ts` / `.ts` 里对 base 做 transform 或覆盖同名键。

| 层 | 自动产物 | 手动产物 |
| --- | --- | --- |
| 后端 | 实体、DTO、仓储接口与实现、应用契约、映射器、`XxxAppServiceBase`、`XxxQueryServiceBase`、`XxxExportProvider`、`XxxPrintDataSource`（未勾导出/打印时只留说明） | 各自的 partial 与 `XxxAppService` / `XxxQueryService` |
| 前端 | `*.types.generated.ts`、`*.generated.ts`、`*.schema.generated.ts` | `*.types.ts`、`*.ts`、`*.schema.ts`、`index.vue` |
| 接线 | `XxxPermissionCodes`、`XxxPermissionDefinitions`、`XxxPermissionCatalog`、`XxxMenuPages` | 无（另附 PageRegistry 片段，供并进模块自己的页面登记表时用） |

接线产物相对后端模块项目根：权限码常量与权限定义在 `Domain/Permissions/`，权限目录登记 `XxxPermissionCatalog` 与菜单登记 `XxxMenuPages` 在 `Infrastructure/Seeders/`，说明与 PageRegistry 片段在 `_GeneratedMenuPermission/`。两个登记类按约定注册，由 SaaS 的汇总种子在权限目录、菜单两个阶段最后（+90）统一写入：不需要 `AddDataSeeder`、没有种子顺序号，重启后端即生效。页面挂在表配置所选的「父菜单」（平台目录，按菜单码挂靠）下，未选即顶级菜单。生成的菜单不带 I18nKey，直接显示业务名称；要多语言时改成 `menu.{页面码中 . 与 - 换成 _}` 并在前端各语言 `menu.ts` 补键。

## 生成代码的约定
- 报文可空性跟列本身走，与 C# DTO 一致；「必填」只管表单校验。非空列留空时文本发空串、数字发 0；下拉、日期、时间、long 标识没有说得通的缺省值，非空即按必填校验。
- 树表写接口挡住自环、挂到自己的下级与删除带子节点的父级；更新、删除找不到记录时报错，不静默成功。
- 页面按钮用按钮码 `{页面码}.{create|update|delete|export|import|status|print}` 门控，按钮码由菜单登记的按钮行下发；前端权限码卫生测试同时扫描 `PageRegistry.cs`、`*MenuSeeder.cs` 与 `*MenuPages.cs`。
- 列表查询入参（`buildPageQuery`）与导入换算（`toCreateInputFromImport`）生成在 `*.schema.generated.ts`，随表结构更新；列表取数与导出中心共用同一份查询入参。导出、导入按钮码写在生成的页面元信息里，取消勾选后重新生成按钮即消失；首次生成后再勾选，只需在 `index.vue` 的 resource 里补 `create` / `export` 一行；状态切换与打印的处理函数在 `index.vue` 里，首次生成后再勾选须对照预览把 `resource.updateStatus`、`onAction` 分支与处理函数并进去。
- 模块名须能推导出合规页面码（`[a-z][a-z0-9_-]*`），否则生成直接失败，避免产出过不了前端门禁的页面码。
- 生成页面的文案为中文字面量，接多语言时换成 `t()` 并补语言包。

## 架构与职责
- `Domain/Entities`、`Domain/Enums`：代码生成领域模型（数据源、表、列、模板、生成历史）
- `Domain/Generation`：引擎契约与共享推导（命名、类型事实、列名判定）
- `Domain/Permissions`：权限码 `code_gen:{read,create,update,delete,import,execute}`
- `Application/*`：Dynamic API 命令/查询服务（分组 `BasicApp.CodeGen`）、DTO、映射器、页面登记表
- `Infrastructure/Generation`：Scriban 渲染器（列级控件与表单口径的唯一判据）、引擎编排、DbFirst 导入、类型映射、Zip 打包、受控落盘、接线产物生成器
- `Infrastructure/Inference`：列配置推断规则
- `Infrastructure/Repositories`、`Infrastructure/Seeders`：仓储实现与种子（内置模板、权限目录、菜单）
- `Templates/Backend`、`Templates/Frontend`：内置 Scriban 模板（嵌入资源）

## 配置
配置节 `CodeGeneration`，只在 `appsettings.Development.json` 里配置（其他环境不配置即只能生成并下载）：
- `EnableGenerateToProject`：是否允许生成到项目，缺省 `false`
- `BackendRootPath`：后端源码根（相对宿主内容根，开发配置 `../..`），在其下分组目录里找 `<命名空间>/<命名空间>.csproj`
- `FrontendRootPath`：前端工程根（相对宿主内容根，开发配置 `../../../../frontend`），须含 `package.json`
- `TablePrefixes`：推导类名时去掉的表名前缀，缺省 `Sys_,Saas_`

找不到或找到多个同名项目、产物路径越界时整体拒绝，一个文件都不写；手动文件已存在时跳过。

## 依赖关系
- `XiHanBasicAppSaasModule`（多租户、仓储基类、权限、菜单与种子基类）
- 复用框架：Scriban 模板、`XiHan.Framework.Data`（DbFirst 元数据）、Dynamic API

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppCodeGenerationModule))]
public class MyModule : XiHanModule
{
}
```

## 目录结构
```text
XiHan.BasicApp.CodeGeneration/
  Domain/
    Entities/  Enums/  Generation/  Permissions/  Repositories/  DomainServices/
  Application/
    Abstractions/  AppServices/  Contracts/  Dtos/  Mappers/  Pages/  QueryServices/
  Infrastructure/
    Generation/  Inference/  Repositories/  Seeders/
  Templates/
    Backend/  Frontend/
  Extensions/
  XiHanBasicAppCodeGenerationModule.cs
```
