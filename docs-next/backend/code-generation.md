# 代码生成（规划中 · N3）

::: warning 尚未实现
6.0 还没有 CodeGeneration 包，没有数据源、表结构、模板与生成历史的页面和操作。本页记录总体方案里的设计，落地时以代码为准。5.x 的代码生成文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/backend/code-generation)。
:::

## 定位

CodeGeneration 是 Tier 3 可选模块，计划在 N3 落地。N3 的退出条件之一：**代码生成能生成一个 6.0 包与前端模块，并编译通过**。

## 为什么要重写模板

5.x 的模板生成的是 Dynamic API 应用服务、`*.types.ts` 与手写 facade；6.0 的写法完全不同，28 个 Scriban 模板要按 6.0 重写，产物应当与手写代码同一套约定：

| 产物 | 6.0 写法 |
| --- | --- |
| 实体 | `App*` / `TenantScoped*` 基类，按平台库 / 租户库选择（见[实体基类](./entity)） |
| 操作 | `Operations/<资源>/<动词>.cs`，一方法一个 `[Operation]`，显式 `Expose`，返回 `Result` |
| 查询 | `Queries/`，以 `TenantScopedQuery.For<T>` 起手 |
| 权限 | `<包>Permissions.cs` 的 `PermissionDescriptor` |
| 页面 | `<包>Pages.cs` 的页面登记 |
| 文案 | `<包>Messages.cs` 与 `Localization/<区域>.json` × 10 |
| 前端 | `src/modules/<包>/views/**`（SchemaPage）与模块文案；契约由 `openapi.json` 生成，**不再生成 API 客户端代码** |

## 设计要点

- 引擎与数据源沿用 5.x 的模型：数据源、表、列、模板、生成历史；
- 模板引擎是 Scriban；
- **零代码只读运行时**：`codegen-runtime.get-schema` / `list`，让未生成代码的表也能以只读列表浏览；
- 资源名以 `codegen-*` 为前缀，权限码 `codegeneration.<资源>.<动作>`。

## 下一步

- [开发流程](./development)：现在手写一个能力包与操作的完整步骤
