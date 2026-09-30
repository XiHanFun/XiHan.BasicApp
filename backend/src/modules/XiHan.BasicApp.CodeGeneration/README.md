# XiHan.BasicApp.CodeGeneration

## 概述
代码生成模块：以「数据库表/列配置 + 模板」为元数据，生成对齐 `XiHan.BasicApp.Saas` 分层（DDD + CQRS + 多租户）的全栈代码——后端实体、DTO、仓储、命令/查询服务，前端类型、接口、页面 schema 与页面，以及菜单与权限的接线代码。另提供按列配置直接查询业务表数据的只读运行时（零代码方向）。

## 能力
- 表配置：DbFirst 导入与同步表结构，按列推断 C#/TS 类型、控件、查询方式与字典/枚举/常量选项；可逐列调整列表、新增、编辑、查询开关与必填。
- 模板类型：单表、树表（父级列 + 显示名列）、主子表（主表明细区按需加载子表）。
- 包含操作：可裁剪新增、编辑、删除写接口与对应按钮；列表与详情始终生成。
- 生成方式：预览、Zip 打包、落盘到白名单目录（默认禁用，见「配置」）。
- 内置模板随程序版本走：`SysCodeGenTemplateSeeder` 把 `Templates/**/*.sbn` 嵌入资源种进平台库，启动时回刷内容；改了模板需重启后端才会落到库里。

## 产物与写入策略
每个产物成对登记：自动文件（`.Generated.cs` / `.generated.ts`，重新生成总是覆盖）与手动文件（仅首次创建，之后永不触碰）。自定义代码写在手动文件里：后端经 partial 合并或继承生成基类后 override，前端在 `.schema.ts` / `.ts` 里对 base 做 transform 或覆盖同名键。

| 层 | 自动产物 | 手动产物 |
| --- | --- | --- |
| 后端 | 实体、DTO、仓储接口与实现、应用契约、映射器、`XxxAppServiceBase`、`XxxQueryServiceBase` | 各自的 partial 与 `XxxAppService` / `XxxQueryService` |
| 前端 | `*.types.generated.ts`、`*.generated.ts`、`*.schema.generated.ts` | `*.types.ts`、`*.ts`、`*.schema.ts`、`index.vue` |
| 接线 | `XxxPermissionCodes`、`XxxPermissionDefinitions` | `XxxPermissionSeeder`、`XxxMenuSeeder`（另附 PageRegistry 片段，二选一） |

接线产物落在 `_GeneratedMenuPermission/`，附 README 说明如何并入源码：复制到目标模块、确认种子 Order 并注册、重建库后由既有种子链生效。生成的菜单不带 I18nKey，直接显示业务名称；要多语言时改成 `menu.{页面码中 . 与 - 换成 _}` 并在前端各语言 `menu.ts` 补键。

## 生成代码的约定
- 报文可空性跟列本身走，与 C# DTO 一致；「必填」只管表单校验。非空列留空时文本发空串、数字发 0；下拉、日期、时间、long 标识没有说得通的缺省值，非空即按必填校验。
- 树表写接口挡住自环、挂到自己的下级与删除带子节点的父级；更新、删除找不到记录时报错，不静默成功。
- 页面按钮用按钮码 `{页面码}.{create|update|delete}` 门控，按钮码由菜单种子的按钮行下发；前端权限码卫生测试同时扫描 `PageRegistry.cs` 与 `*MenuSeeder.cs`。
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
配置节 `CodeGeneration`：
- `EnableCustomPathDisk`：是否允许落盘到自定义路径，缺省 `false`
- `AllowedRootPaths`：允许落盘的根目录白名单（路径穿越 fail-closed）
- `TablePrefixes`：推导类名时去掉的表名前缀，缺省 `Sys_,Saas_`

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
