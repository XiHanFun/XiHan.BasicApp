# 开发流程

本页以在 Settings 包中新增参数操作为例，说明在 6.0 后端新增一个接口的完整步骤，适用于后端开发者。阅读前建议先了解[架构总览](./introduction)。

## 一、确定契约

先写清楚四件事，它们是包的公开面，破坏性变更只在大版本做：

| 项 | 文法 | 例 |
| --- | --- | --- |
| 操作 ID | `<资源>.<动词>`，资源复数 kebab | `configs.create`、`users.reset-password` |
| 权限码 | `<包>.<资源>.<动作>`，资源段与操作 ID 同词 | `settings.configs.create` |
| 错误码 | `<包大写>_<KIND>_<NNN>` | `SETTINGS_CONFLICT_001` |
| 消息键 | `<操作 ID>.title` / `.description`，以及包内的错误文案键 | `configs.create.title` |

动词约定：标准动词 `create / get / list / update / delete`；写动作用动词原形（`lock`、`reset-password`、`transfer-owner`）；查询变体用 `list-<限定>` 或独立资源，`get` / `list` 以外的只读动词一律声明 `ReadOnly = true`。对已有对象按标识的写操作（删除、启停、授权、撤销）只提供批量形态。

## 二、定义请求与响应

请求、响应是 `record`，与操作放在 `Operations/<资源>/` 下。单字段约束用 DataAnnotations，它们会被源生成器折进操作的 JSON Schema，同时进入 OpenAPI 与前端生成的类型：

```csharp
public sealed record CreateConfigRequest(
    [Required, StringLength(128, MinimumLength = 3), RegularExpression(ConfigKeys.Pattern)] string Key,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    ConfigDataType DataType = ConfigDataType.Text,
    [StringLength(65536)] string? Value = null,
    bool IsEncrypted = false,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Description = null);
```

- 跨字段规则实现 `IValidatableObject`，文案用 `key.L()` 取调用方语言。
- 用属性写的请求 DTO，有缺省值的属性用 `set` 而不是 `init`：源生成的序列化把 `init` 属性当构造参数，JSON 里没写的成员会以 `null` 覆盖缺省值（`ContractDtoTests` 守护）。
- 时间字段用 `DateTimeOffset`（UTC）；主键 `Guid`；租户 ID `string`。
- 请求与响应类型登记进本包的 `<包>JsonContext`（源生成的 `JsonSerializerContext`），枚举按字符串序列化。
- XML 文档注释的 `<summary>` 只写一句，细节写进 `<remarks>` / `<param>`：DTO 的注释会成为 OpenAPI 的字段说明，`XmlDocumentationTests` 检查格式。

## 三、实现操作

```csharp
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.ConfigsCreate)]
public sealed class CreateConfig(IRepository<SysConfig, Guid> configs, ConfigValues values, IIdGenerator<Guid> ids)
    : IOperation<CreateConfigRequest, ConfigResponse>
{
    public const string Id = "configs.create";

    public async ValueTask<Result<ConfigResponse>> HandleAsync(CreateConfigRequest request, InvocationContext context)
    {
        var owner = SettingsScope.OwnerOf(context);
        var key = request.Key.Trim();
        if (await configs.CountAsync(EntityQuery.All<SysConfig>()
                .Where(new Specification<SysConfig>(c => c.OwnerTenantId == owner && c.Key == key)), context.CancellationToken) > 0)
        {
            return Error.Conflict(SettingsErrors.ConfigKeyTaken, SettingsMessages.ConfigKeyTaken, MessageArguments.Of(("key", key)));
        }

        var config = new SysConfig { Id = ids.NewId(), OwnerTenantId = owner, Key = key, /* … */ };
        var added = await configs.AddAsync(config, context.CancellationToken);
        return added.IsFailure ? added.Error! : ConfigResponse.From(config);
    }
}
```

要点：

- 一方法一操作，类名是动词短语（`CreateConfig`、`DeleteConfigs`），操作 ID 写成 `const string Id`。
- `Expose` 必须显式写：没写即编译错误（框架诊断 `XHG004` 在 `.editorconfig` 里设为 error）。管理操作只 `Expose.Http`；要进 MCP / gRPC 的只读操作登记到 `OperationExposureTests`。
- 授权：`Permission = <包>Permissions.Xxx`，自助操作写 `Policy = "authenticated"`；匿名操作两者都不写，并登记到架构测试的匿名白名单。
- 失败是值：可恢复的失败返回 `Error.Validation / NotFound / Conflict / Denied / Unavailable(...)`，不抛业务异常；异常只用于编程错误与基础设施故障。
- 无需编写事务代码：管道的 Transaction 阶段为每次调用开工作单元，成功提交、失败回滚；写库前需要显式开启时用 `SqlSugarUnitOfWork.EnsureTransactionAsync`。
- 取消：`context.CancellationToken` 一路传下去。
- 租户与软删除：仓储（`IRepository<TEntity, Guid>`）自动处理；分页、投影、联表放进 `Queries/`，以 `TenantScopedQuery.For<T>(client, context)` 起手，见[数据模型](./data-model)。

操作由 `context.AddGeneratedOperations()` 统一登记，不需要逐个注册。

## 四、权限、错误与文案

```csharp
// SettingsPermissions.cs
public const string ConfigsCreate = "settings.configs.create";
public static IReadOnlyList<PermissionDescriptor> All { get; } =
[
    new(ConfigsCreate, "Create configuration", PermissionScope.Both, audited: true,
        description: "Create configuration entries on the caller's side, including tenant overrides of platform entries."),
    // …
];
```

- `PermissionScope`：`Platform` / `Tenant` / `Both`，平台主体与租户主体只能命中各自作用侧；`audited: true` 的操作写操作日志。
- 权限目录、菜单的权限门控、套餐白名单都从这份登记派生，不再有手写的权限常量清单或按钮码。
- 错误码常量写在 `<包>Errors.cs`，文案键写在 `<包>Messages.cs`，译文写在 `Localization/<区域>.json`：

```json
{
  "configs.create.title": "新建参数",
  "configs.create.description": "在本侧新建参数：键在本侧唯一，值按数据类型校验……",
  "permissions.settings.configs.create.name": "新建参数配置",
  "openapi.tags.configs.name": "参数配置"
}
```

10 个区域文件的键集必须一致，且与常量一一对应（`PackageStructureTests`）。`<操作>.title` / `.description` 同时是 OpenAPI 摘要与 MCP 工具说明，生成器会在说明末尾自动追加授权要求，不要手写。

## 五、登记页面（可选）

在 `<包>Pages.cs` 登记页面：

```csharp
public static PageDescriptor Configs { get; } =
    new("settings.configs", "/settings/configs", "settings.configs.title", "lucide:sliders-horizontal", 400, SettingsPermissions.ConfigsRead);
```

组件路径 = 路由去前导斜杠 + `/index`，前端对应 `src/modules/settings/views/configs/index.vue`。执行 `seed` 后 `access.menus` 种子把页面对齐成菜单。

## 六、测试

| 项目 | 放什么 |
| --- | --- |
| `XiHan.BasicApp.Architecture.Tests` | 无需为单个操作编写；新增操作后运行一次，检查 Expose、权限码文法、资源段一致、文案键集、XML 文档有没有违规 |
| `XiHan.BasicApp.Integration.Tests` | 端到端用例：经 TestServer 走完整管道（认证、租户、权限、事务、校验），SQLite 缺省；PostgreSQL 用例读 `XIHAN_TEST_POSTGRES` |

一个操作至少覆盖：成功路径、权限不足、租户隔离（另一个租户看不到 / 改不了）、校验失败的字段级 `errors`、冲突与找不到的错误码。

```bash
dotnet build backend-next/XiHan.BasicApp.slnx -c Release --no-restore
dotnet test --solution backend-next/XiHan.BasicApp.slnx -c Release --no-build
```

## 七、导出契约

```bash
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- contracts export --out "$PWD/frontend-next/contracts"
cd frontend-next && pnpm gen:contracts
```

`contracts export` 不启动应用、不连数据库，输出 `openapi.json` 与 `realtime.json`；`gen:contracts` 生成 `packages/contracts/*`。后端、契约文件、生成物与前端页面在同一个提交里，CI 的契约漂移门禁会重新导出并比对。

## 八、提交

- Conventional Commits，说明用中文：`feat(next): …`；
- 提交前 `git diff --check`、相关测试与构建；
- 改了表结构就同步 `Host/Schema/<版本>/*.sql`（见[表结构升级](./upgrade)）。

## 常用命令

```bash
# 框架内建命令：help / graph / pipeline / operations
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- operations   # 每行：ID、稳定性、暴露、权限或策略
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- pipeline     # 管道各阶段的中间件次序
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- graph        # Feature 与能力依赖图
# BasicApp 自己的命令
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- seed --list
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- schema upgrade
```

命令模式用同一套装配但不监听端口，日志只写 stderr，stdout 只承载命令输出（成功是 JSON，失败是 `{"code","detail"}`，退出码按错误 KIND）。框架的 `invoke` 只发布 `Expose.CommandLine` 的操作，BasicApp 的操作都不在命令行上发布。

## 下一步

- [请求生命周期](./request-lifecycle) · [权限管理](./permission) · [前端开发流程](../frontend/development)
