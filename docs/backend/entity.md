# 实体基类

本页说明应用的实体基类、字段约定、平台数据的建模规则与领域事件，适用于新增实体的后端开发者。

框架只提供实体协议接口（`IEntity<TKey>`、`IHasConcurrencyStamp`、`IHasCreationTime`、`IHasModificationTime`、`ISoftDeletable`、`IMultiTenant`、`IAggregateRoot<TKey>` 等），不提供基类。BasicApp 在 `XiHan.BasicApp.Contracts/Entities/` 里用这些接口组合出一组应用基类，并补上框架没有的「操作人」字段（`IHasCreator`、`IHasModifier`、`IHasDeleter`）。

## 基类一览

| 基类 | 组合 | 字段 | 用途 |
| --- | --- | --- | --- |
| `AppEntity` | `IEntity<Guid>`、`IHasConcurrencyStamp` | `Id`、`ConcurrencyStamp` | 关联表、无审计 |
| `AppRecord` | `AppEntity` + `IHasCreationTime` + `IHasCreator` | `CreatedAt`、`CreatedBy` | 只增的记录：日志、流水、投递记录、授予规则行 |
| `AppAuditedEntity` | `AppRecord` + `IHasModificationTime` + `IHasModifier` + `ISoftDeletable` + `IHasDeleter` | `ModifiedAt`、`ModifiedBy`、`IsDeleted`、`DeletedAt`、`DeletedBy` | 缺省选择 |
| `AppAggregateRoot` | `AppAuditedEntity` + `IAggregateRoot<Guid>` | `DomainEvents` | 有行为、要发领域事件的聚合 |
| `TenantScopedEntity` / `TenantScopedRecord` / `TenantScopedAuditedEntity` / `TenantScopedAggregateRoot` | 上面四个 + `IMultiTenant` | `TenantId` | 只用于租户库实体 |

叫 `TenantScoped*` 是为了不与框架 `MultiTenancy.Database` 的租户目录实体 `TenantRecord` 重名。哪些实体用哪组基类见[数据模型](./data-model)。

## 字段约定

| 字段 | 类型 | 谁维护 |
| --- | --- | --- |
| `Id` | `Guid`，UUID v7（经 `IIdGenerator<Guid>` 取） | 操作里 `ids.NewId()` |
| `ConcurrencyStamp` | 32 位字符串 | 框架仓储，更新时比对 |
| `CreatedAt` / `ModifiedAt` / `DeletedAt` | `DateTimeOffset`，库内 UTC | 框架仓储 |
| `CreatedBy` / `ModifiedBy` / `DeletedBy` | `string?`，值为主体 ID | Runtime 的 SqlSugar 回调（见下） |
| `IsDeleted` | `bool` | 框架仓储：删除即置位，查询自动排除 |
| `TenantId` | `string` | 框架仓储按当前租户写入并严格过滤 |

### 操作人字段的填充

框架没有操作人字段。Runtime 的 `AuditorStampMiddleware` 在管道 Transaction 阶段把当前已认证主体的 ID 放进异步本地值（`AuditorStamp`），SqlSugar 的 `AopEvents.DataExecuting` 回调据此在插入、更新、软删除时填 `CreatedBy` / `ModifiedBy` / `DeletedBy`。

不经依赖注入，是因为方式一的连接配置在装配时只建一份，回调里拿不到作用域服务，异步本地值是能读到当前主体的方式。作业与种子里的写入没有主体，操作人字段为空；要以某个主体写入时用 `AuditorStamp.Enter(主体 ID)` 开一段范围。

## 平台数据：不实现 `IMultiTenant`

框架仓储对实现了 `IMultiTenant` 的实体做严格相等过滤：没有租户上下文时看不到任何行。平台管理员的调用没有租户上下文，所以平台库实体不能靠租户列区分平台与租户。

平台库实体（用户、会话、租户、套餐、角色与授权、菜单、参数、字典、模板、各类配置等）不实现 `IMultiTenant`。需要区分数据属于平台还是某个租户时，实体实现 `IOwnedByTenant`，以显式列 `OwnerTenantId` 记录属主，平台的值固定为 `platform`（`TenantIds.Platform`）。账号 `SysUser` 用 `HomeTenantId`（注册地，平台账号为 `platform`），会话 `SysUserSession` 用 `ActiveTenantId`（会话当前所在的租户，平台为空）；登录等还没有租户上下文的操作按这些列显式查询。

Runtime 的 `TenantScopedQuery` 为 `IOwnedByTenant` 提供两个查询扩展：

| 扩展 | 语义 | 用在 |
| --- | --- | --- |
| `OwnedByCurrentTenantOrPlatform(context)` | 读共享：当前租户的加平台的；没有租户时只有平台的 | 读参数、字典、模板，租户覆盖平台 |
| `OwnedByCurrentTenant(context)` | 写不共享：只认当前一侧 | 修改、删除 |

## 实体示例

```csharp
[SugarTable(TableName = "sys_config", TableDescription = "系统参数（平台库实体，不实现 IMultiTenant）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Key), Name = "ux_sys_config_owner_tenant_id_key")]
public sealed class SysConfig : AppAuditedEntity, IOwnedByTenant
{
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "key", Length = 512, IsNullable = false, ColumnDescription = "参数键")]
    public string Key { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "group", Length = 512, IsNullable = true, ColumnDescription = "分组（自由文本，用于列表归类）")]
    public string? Group { get; set; }

    [SugarColumn(ColumnName = "value", IsNullable = true, ColumnDescription = "参数值")]
    [LongText]
    public string? Value { get; set; }

    [SugarColumn(ColumnName = "data_type", IsNullable = false, ColumnDescription = "数据类型")]
    public ConfigDataType DataType { get; set; } = ConfigDataType.Text;

    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public SettingStatus Status { get; set; } = SettingStatus.Active;

    // 其余列同样写法
}

public enum ConfigDataType
{
    Text = 0,
    Number = 1,
    Boolean = 2,
    Json = 3,
    Array = 4,
}
```

- 类名以 `Sys` 开头。表名、列名不靠命名约定推出（缩写会被拆错，`SysOAuthApp` 会推成 `sys_o_auth_app`），实体显式写 `[SugarTable(TableName, TableDescription)]`，映射到列的属性写 `[SugarColumn(ColumnName, Length, IsNullable, ColumnDescription)]`；名字用小写下划线（`sys_oauth_app`、`owner_tenant_id`），说明取 `<summary>` 那一句，可空与属性类型一致。基类的公共列（`id`、`concurrency_stamp`、`created_at`、`tenant_id` 等）在 Contracts 的基类上写好了。
- 字符串列在 `[SugarColumn]` 里写 `Length`（常规短文本 512），长文本不写长度、另标 `[LongText]`；不再用 `[StringLength]` / `[MaxLength]` 给实体定长度。给已有行的表加列时，非可空的字符串与日期列要声明成可空。
- 索引名明确写出：`[SugarIndex("ix_<表>_<列>…", …)]`，唯一的 `ux_` 开头，按月分表的用 `{split_table}` 占位；软删除实体的唯一约束写 `[ActiveUniqueIndex(…, Name = "ux_<表>_<列>…")]`（部分唯一索引），不软删除的用唯一 `[SugarIndex]`，见[索引与唯一性](./data-model#索引与唯一性)。
- 实体类的 `<remarks>` 讲这个实体为什么这样设计：为什么单独成表、落哪个库、结构与唯一性的取舍、为什么软删或只追加、已知限制；每行一个决定和理由，不罗列操作与错误码，也不套固定分段。
- 架构测试 `EntityMappingTests` 守护上面几条：缺表名、列名、说明、长度，可空与类型不符，或索引名不合 `ix_` / `ux_` + 表名 + 列即失败。
- 枚举在库里存整数，JSON 里是字符串（各包 `JsonContext` 按字符串序列化）。只被一个实体用到的枚举写在那个实体的文件里（实体类之后，同一命名空间），两个以上实体共用的才单独成文件（如 `SettingStatus.cs`）。
- 写入极频繁或只存密文与计数的实体（会话、用户设置、账号安全、OAuth 授权码与令牌、邮件短信的发送记录、聊天会话与成员等）在类上标 `[DiffLogIgnored]`，不进数据变更日志；其余全审计实体缺省都记前后值。

## 领域事件

`AppAggregateRoot` 带事件缓冲，派发流程：

1. 聚合在方法里 `Raise(事件)`。
2. 每种事件在发出方的 `Configure` 里 `AddDomainEvent(topic, type, jsonTypeInfo, subject)` 显式登记，主题格式 `basicapp.<包>.<事件>`。
3. 工作单元在提交前经发件箱把事件打成 CloudEvents 信封，与业务写入同事务（宿主已装 `OutboxFeature`），再由后台投递。
4. 聚合记了没登记的事件，提交时调用失败（`MESSAGING_VALIDATION_003`）。

主题常量集中在 Contracts 的 `Topics`，事件记录放在 Contracts 里发出方的目录（订阅方不引用发出方的包）。目前登记的领域事件：

| 主题 | 发出 | 订阅 |
| --- | --- | --- |
| `basicapp.organization.membership-changed` | Organization：加入或移出部门（`SysUserDepartment` 是聚合根） | Chat：同步部门群成员（见[在线聊天](./chat#部门群同步)） |

订阅方在自己的 `Configure` 里 `AddMessageHandler<THandler>(topic)` 登记。投递至少一次、可能乱序，处理器要按处理时的当前状态核对，重复处理无副作用。其余跨包协作走 Contracts 里的接口（如经 `IMessageSender` 排队邮件与短信），推送走 `IRealtimePush`。

## 下一步

- [数据模型](./data-model)：哪些实体在平台库、哪些在租户库
- [数据库配置](./database) · [多租户 SaaS](./multi-tenancy)
