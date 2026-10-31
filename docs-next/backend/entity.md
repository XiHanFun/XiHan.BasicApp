# 实体基类

本页说明 6.0 的实体基类、字段约定与平台数据的建模规则，适用于新增实体的后端开发者。

框架 5 只提供实体协议接口（`IEntity<TKey>`、`IHasConcurrencyStamp`、`IHasCreationTime`、`ISoftDeletable`、`IMultiTenant`、`IAggregateRoot<TKey>`…），不提供基类。BasicApp 在 `XiHan.BasicApp.Contracts/Entities/` 里用这些接口组合出一组应用基类，并补上框架没有的「操作人」字段。

## 基类一览

| 基类 | 组合 | 字段 | 用途 |
| --- | --- | --- | --- |
| `AppEntity` | `IEntity<Guid>`、`IHasConcurrencyStamp` | `Id`、`ConcurrencyStamp` | 关联表、无审计 |
| `AppRecord` | `AppEntity` + `IHasCreationTime` + `IHasCreator` | `CreatedAt`、`CreatedBy` | 只增的记录：日志、流水、授予规则行 |
| `AppAuditedEntity` | `AppRecord` + `IHasModificationTime` + `IHasModifier` + `ISoftDeletable` + `IHasDeleter` | `ModifiedAt`、`ModifiedBy`、`IsDeleted`、`DeletedAt`、`DeletedBy` | 默认选择 |
| `AppAggregateRoot` | `AppAuditedEntity` + `IAggregateRoot<Guid>` | `DomainEvents` | 有行为、要发领域事件的聚合 |
| `TenantScopedEntity` / `TenantScopedRecord` / `TenantScopedAuditedEntity` / `TenantScopedAggregateRoot` | 上面四个 + `IMultiTenant` | `TenantId` | 只用于租户库实体 |

`TenantScoped*` 即方案中的「`TenantXxx` 变体」，如此命名是为了避免与框架 `MultiTenancy.Database` 的租户目录实体 `TenantRecord` 重名。

## 字段约定

| 字段 | 类型 | 谁维护 |
| --- | --- | --- |
| `Id` | `Guid`，UUID v7（框架 `GuidV7IdGenerator`，经 `IIdGenerator<Guid>` 取） | 操作里 `ids.NewId()` |
| `ConcurrencyStamp` | 32 位字符串 | 框架仓储，更新时比对 |
| `CreatedAt` / `ModifiedAt` / `DeletedAt` | `DateTimeOffset`，库内 UTC | 框架仓储 |
| `CreatedBy` / `ModifiedBy` / `DeletedBy` | `string?`，值为主体 ID | Runtime 的 SqlSugar 回调（见下） |
| `IsDeleted` | `bool` | 框架仓储：删除即置位，查询自动排除 |
| `TenantId` | `string` | 框架仓储按当前租户写入并严格过滤 |

### 操作人字段的填充

框架没有操作人字段。`Runtime` 的 `AuditorStampMiddleware` 在管道 Transaction 阶段把当前主体 ID 放进异步本地值（`AuditorStamp`），SqlSugar 的 `AopEvents.DataExecuting` 回调据此在插入、更新、软删除时填 `CreatedBy` / `ModifiedBy` / `DeletedBy`。

为什么不经依赖注入：持久化「方式一」的 `ConnectionConfig` 在装配时只建一份、回调里没有服务提供者，异步本地值是能读到当前主体的方式（N0 冒烟结论，SQLite 与 PostgreSQL 都验证过）。作业与种子里的写入没有主体，操作人字段为空。

## 平台数据：不实现 `IMultiTenant`

框架仓储对实现了 `IMultiTenant` 的实体做严格相等过滤：没有租户上下文时看不到任何行。平台管理员没有租户上下文，因此 5.x 中「`TenantId = 0` 表示平台」的做法在 6.0 不成立。

平台库实体（用户、会话、租户、套餐、角色与授权、菜单、参数、字典、模板、配置等）不实现 `IMultiTenant`。需要区分数据属于平台还是某个租户时，实体实现 `IOwnedByTenant`，以显式列 `OwnerTenantId` 记录属主，平台的值固定为常量 `platform`（`TenantIds.Platform`）。账号域实体（用户、会话）使用 `HomeTenantId`（注册地）列，登录等尚无租户上下文的操作按该列显式查询。

`Runtime` 的 `TenantScopedQuery` 为 `IOwnedByTenant` 提供两个查询扩展：

| 扩展 | 语义 | 用在 |
| --- | --- | --- |
| `OwnedByCurrentTenantOrPlatform()` | 读共享：当前租户的 + 平台的 | 读参数、字典、模板，租户覆盖平台 |
| `OwnedByCurrentTenant()` | 写不共享：只认当前一侧 | 修改、删除 |

## 实体示例

```csharp
public sealed class SysConfig : AppAuditedEntity, IOwnedByTenant
{
    public string OwnerTenantId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Group { get; set; }

    [LongText]
    public string? Value { get; set; }

    public ConfigDataType DataType { get; set; } = ConfigDataType.Text;
    public bool IsEncrypted { get; set; }
    public bool AllowTenantOverride { get; set; }
    public SettingStatus Status { get; set; } = SettingStatus.Active;
    public int Order { get; set; }
}
```

- 类名以 `Sys` 开头，表名、列名由命名约定转成 snake_case（`SysConfig` → `sys_config`，`OwnerTenantId` → `owner_tenant_id`）。
- 没写长度的字符串列缺省 `varchar(512)`；长文本标 `[LongText]`，固定长度用 `[StringLength]` / `[MaxLength]`（`PersistenceMappingTests` 守住）。
- 枚举存字符串还是数值由框架约定处理，JSON 里一律是字符串。
- 不想进数据变更日志的属性标 `[DiffLogIgnored]`。

## 领域事件

`AppAggregateRoot` 带事件缓冲，框架的派发机制是：聚合在方法里记录事件；每种事件在所属包的 `Configure` 里 `AddDomainEvent<TEvent>(topic, type, jsonTypeInfo)` 显式登记，主题格式 `basicapp.<包>.<事件>`；工作单元在 Transaction 阶段提交前经发件箱把事件打成 CloudEvents 信封，与业务写入同事务（宿主已装 `OutboxFeature`）；未登记就发事件是装配错误，第一次调用即暴露。

目前已落地的包还没有登记任何领域事件或消息处理器：跨包协作走 `Contracts` 里的接口（如通知发布时经 `IMessageSender` 直接入队），推送走 `IRealtimePush`。需要异步解耦时再按上面的方式接入。

## 下一步

- [数据模型](./data-model)：哪些实体在平台库、哪些在租户库
- [数据库配置](./database) · [多租户 SaaS](./multi-tenancy)
