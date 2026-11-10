来源：https://basicapp.docs.xihanfun.com/backend/data-permission

# 数据权限与字段安全

本页说明数据范围与字段安全的规则、登记方式与管理操作，适用于后端开发者与权限配置人员。

权限码决定能否调用某个操作，数据范围决定能看到哪些行，字段安全决定行里哪些字段可读、可写。两者都在 Access 包实现：

- 数据范围由各包在查询与按 ID 取单条处经 `Runtime` 的 `DataScopeQuery` 显式套用；
- 字段安全由各包登记「哪些操作读、建、改哪个实体」，Access 的中间件与响应过滤器统一套用，操作本身不写字段安全代码。

## 数据范围

### 五种范围

| 值 | 含义 |
| --- | --- |
| `SelfOnly` | 只看自己（新建租户角色的缺省） |
| `DepartmentOnly` | 本人所在的部门 |
| `DepartmentAndChildren` | 本人所在的部门及其全部下级 |
| `All` | 不受限 |
| `Custom` | 指定的部门（每个部门可选含下级） |

### 来源与合并

- 角色经 `roles.create` / `update` 设置 `dataScope` 与自定义部门（`SysRoleDataScope`）。平台角色只能是 `All`（`ACCESS_VALIDATION_004`）；`Custom` 至少一个部门（`ACCESS_VALIDATION_005`）。
- 用户级覆盖（`SysUserDataScope`）设置后整体取代该用户各角色的数据范围，不取并集；没有覆盖时按角色取并集，任一为 `All` 即不受限。
- 角色只认令牌里的角色（本会话激活的、本租户启用的），不展开继承，也不看委托。
- 受限时把部门集合展开成用户集合，总含本人。
- 平台主体与没有装配 Access 的宿主不受数据范围限制。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `user-data-scopes.get` | `access.user-data-scopes.read` | 一个用户的覆盖 |
| `user-data-scopes.set` | `access.user-data-scopes.update` | 覆盖为一种范围（`Custom` 要列出部门），不带范围即清除覆盖 |

两个权限码只在租户侧，用户须注册在当前租户或是它的外部成员。

### 在查询中套用

列表以 `DataScopeQuery.ResolveAsync(context)` 取判定，按「归属人列」或再加「部门列」生成 `owner IN (...) [OR department IN (...)]`：

```csharp
// Organization 的部门成员列表：归属人列是成员的用户 ID，部门列是所在部门
var query = TenantScopedQuery.For<SysUserDepartment>(unitOfWork.ConnectionFor(typeof(SysUserDepartment)), context)
    .ApplyDataScope(await DataScopeQuery.ResolveAsync(context), static m => m.UserId, static m => m.DepartmentId);
```

没有归属人列的实体用 `ApplyDataScopeByCreator`，按审计列 `CreatedBy`（创建人的用户 ID）生成 `created_by IN (...)`；没有创建人的行（系统写入）受限时看不到：

```csharp
// Files 的文件库列表：按上传人
var query = rows.Query<SysFile>(SideRows.SideOf(context))
    .ApplyDataScopeByCreator(await DataScopeQuery.ResolveAsync(context), static f => f.CreatedBy);
```

按 ID 取单条或批量处理时用判定的 `Allows(userId)`，不在范围内按找不到处理。

目前套用的地方：

| 包 | 位置 |
| --- | --- |
| Identity | 用户列表、`users.get`、`users.detail`、修改、重置密码，以及删除、停用、锁定、解锁、重置双因素、登录策略等批量操作；在线会话列表 |
| Organization | 部门成员列表 |
| Files | 文件库列表（按上传人；下载与签名地址按文件的访问级别） |
| Notification | 通知管理列表（按创建人；收件箱不受影响） |

导出中心只列本人提交的任务，不再套数据范围。

## 字段安全

### 规则

`SysFieldSecurityRule`：侧 × 目标 × 实体 × 字段，同一侧同一目标同一字段只有一条（`ACCESS_CONFLICT_006`）。规则只在所属一侧生效。

| 维度 | 取值 |
| --- | --- |
| 目标类型 | `Role`（此刻直接持有该角色的用户）、`User`（本人）、`Department`（该部门及其下级的成员，只在租户侧） |
| 读取方式（`FieldMaskStrategy`） | `None` 明文、`Hidden` 返回空值、`FullMask` 按原长替换成 `*`、`PartialMask` 保留前后几位、`Hash` SHA-256 前 16 位小写十六进制、`Redact` 有值时显示固定文本 |
| 能否改写 | 只读，或可改；读受保护且可改即「只写」：看不到原值但能填新值 |

### 命中与合并

- 用户规则认本人；角色规则认此刻持有的角色（令牌里的角色与有效指派的交集、角色启用，只认直接持有，不看继承）；部门规则认所在部门及其上级（停用的部门不算）。
- 同一字段多条命中：读取方式取最严（从宽到严依次是 `None`、`PartialMask`、`Hash`、`FullMask`、`Redact`、`Hidden`），部分脱敏的保留位数取最少，固定文本取第一条，任一只读即只读。
- 启用的规则按（侧、实体）缓存 60 秒，写入在提交之后失效。

### 套用

规则由 Access 统一套用，HTTP、gRPC、MCP 与内部调用（导出、AI 工具）拿到的都是套过的结果：

| 环节 | 位置 | 行为 |
| --- | --- | --- |
| 查询门控 | `FieldSecurityMiddleware`（Handle 阶段） | 按读受保护的字段筛选或排序即 `ACCESS_DENIED_006`；登记的关键字字段里有读受保护的，关键字搜索同样拒绝 |
| 写入 | `FieldSecurityMiddleware` | 新建时填了只读字段、修改时改了只读字段即 `ACCESS_DENIED_007`；修改时交回原值、脱敏值或空值即保留库里的原值 |
| 响应 | `FieldSecurityResponseFilter`（框架的 `IOperationResponseFilter`，操作成功、编码之前） | 读、建、改的响应里的记录按读取方式打码 |

没有规则的调用原样放行。登录、刷新、个人中心这类本人自己的资料没有登记，不套用。用户目录的关键字搜索由 Identity 自己收窄：只搜没受保护的登录名与显示名，两个都受保护时返回空页。

### 登记实体与操作

各包在 `Configure` 里用 Contracts 的 `AddFieldSecurity` 登记：

```csharp
services.AddFieldSecurity("organization", OrganizationJsonContext.Default, fields => fields
    .Entity<PositionResponse>("organization.positions", "organization.positions.title", "organization.positions.fields.")
        .Reads(ListPositions.Id, GetPosition.Id)
        .Creates(CreatePosition.Id)
        .Updates<UpdatePositionRequest>(UpdatePosition.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysPosition>(context, request.Id, PositionResponse.From))
        .Keyword("code", "name"));
```

- 实体码是 `<包>.<资源>`；字段由响应类型的 JSON 元数据生成，与接口字段同名，可空性取自注解。主键、租户、并发戳、软删除等框架列以及嵌套对象与集合不进目录。
- 字段显示名的文案键是前缀加 snake_case 的字段名，前端没有这个键时显示字段名。
- 记录不在响应根上时用 `ReadsAt(操作, "属性")`；修改操作要给出取改动前记录的委托（`Runtime` 的 `FieldSecurityOriginal.FindAsync`）。
- 两个包登记同一实体码即装配失败。

目前登记的实体：

| 包 | 实体码 |
| --- | --- |
| Identity | `identity.users`、`identity.sessions` |
| Tenancy | `tenancy.tenants`、`tenancy.editions` |
| Organization | `organization.positions` |
| Access | `access.roles`、`access.permissions`、`access.constraint-rules`、`access.policies` |
| Settings | `settings.configs`、`settings.dicts`、`settings.dict-items`、`settings.numbering-rules`、`settings.numbering-allocations` |
| Notification | `notification.notifications`、`notification.message-templates`、`notification.emails`、`notification.sms`、`notification.email-configs`、`notification.sms-configs`、`notification.bot-configs`、`notification.telegram-bots` |
| Files | `files.files`、`files.file-storages`、`files.storage-configs` |
| Auditing | `auditing.access-logs`、`auditing.open-api-logs`、`auditing.operation-logs`、`auditing.login-logs`、`auditing.exception-logs`、`auditing.diff-logs`、`auditing.condition-hits` |
| OpenPlatform | `openplatform.oauth-apps` |
| Maintenance | `maintenance.versions`、`maintenance.migrations` |
| Printing | `printing.print-templates` |
| Workflow | `workflow.workflow-definitions`、`workflow.workflow-instances` |
| CodeGeneration | `codegeneration.codegen-datasources`、`codegeneration.codegen-tables`、`codegeneration.codegen-templates` |
| AI | `ai.ai-providers`、`ai.ai-prompts`、`ai.ai-assistants`、`ai.knowledge-documents` |

可选模块关掉时，它登记的实体不进目录。

### 管理操作

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `field-security.entities` | `access.field-security.read` | 已登记的实体与可配置字段 |
| `field-security.list` / `get` | `access.field-security.read` | |
| `field-security.create` / `update` | `access.field-security.create` / `update` | |
| `field-security.delete` | `access.field-security.delete` | 批量，逐项结果 |

权限码两侧都有。页面 `access.field-security`（`/access/field-security`）。

校验错误：

| 错误码 | 情形 |
| --- | --- |
| `ACCESS_VALIDATION_014` / `015` | 实体或字段不在目录 |
| `ACCESS_VALIDATION_016` | 读取方式不适用：非文本字段只能明文或隐藏，响应里不可空的字段不能隐藏 |
| `ACCESS_VALIDATION_017` | 明文且可改，什么都没限制 |
| `ACCESS_VALIDATION_018` | 部分脱敏的保留位数不在 0–32，或前后都是 0 |
| `ACCESS_VALIDATION_019` | 固定文本缺失或超过 100 个字符 |
| `ACCESS_VALIDATION_020` | 部门目标只在租户侧 |
| `ACCESS_VALIDATION_021` | 目标角色或部门已停用 |

目标不在这一侧时按找不到报（`ACCESS_NOTFOUND_001` / `002` / `003`）。删除角色时连带删除以它为目标的规则；新建、修改、删除记权限变更日志（`FieldSecurity`）。

## 前端

前端不实现安全边界，只改善可用性：受保护的列显示后端给的打码值，只读字段在表单里置灰。见[前端权限与脱敏](../frontend/permission)。

## 下一步

- [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [组织架构](./organization)
