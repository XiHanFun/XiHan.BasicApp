# 数据权限与字段安全

权限码回答「能不能调这个操作」；数据范围回答「能看到哪些行」；字段安全回答「行里的哪些字段能看、能改」。后两者**不在**框架的授权判定链上：6.0 的响应是不可变记录，没有 5.x 的响应过滤器，由各包在查询、投影与写入处经 `Runtime` / `Contracts` 的接口显式套用。

## 数据范围

### 五种范围

| 值 | 含义 |
| --- | --- |
| `SelfOnly` | 只看归属人是自己的行 |
| `DepartmentOnly` | 本人所在部门 |
| `DepartmentAndChildren` | 本人所在部门及其下级 |
| `All` | 全部 |
| `Custom` | 指定部门（可含子部门） |

### 来源与合并

- **角色**：`roles.create` / `update` 带 `dataScope` 与自定义部门（`SysRoleDataScope`）。平台角色只能是 `All`；租户角色不写即只看自己。
- **用户级覆盖**（`user-data-scopes.get` / `set`，权限 `access.user-data-scopes.read` / `update`，只在租户侧）：设了即**整体取代**该用户各角色的数据范围，不设即按角色取并集。
- 平台主体、以及没有装 Access 的宿主一律不受限。
- 数据范围不随角色继承、不随委托。

### 怎么套到查询上

```csharp
// Organization 的部门成员查询：归属人列是成员的用户 ID，部门列是成员所在部门
var query = TenantScopedQuery.For<SysUserDepartment>(client, context)
    .ApplyDataScope(await DataScopeQuery.ResolveAsync(context), static m => m.UserId, static m => m.DepartmentId);
```

`ApplyDataScope` 按「归属人列」或「部门列」生成 `owner IN (...) [OR department IN (...)]`。目前套用的地方：

| 包 | 位置 |
| --- | --- |
| Identity | 用户列表、详情、修改、重置密码与批量操作；在线会话列表 |
| Organization | 部门成员列表 |

文件、导出、通知等列表**还没有**按数据范围收窄。

## 字段安全

### 规则

`SysFieldSecurityRule`：侧 × 目标 × 实体 × 字段，同一侧同一目标同一字段只有一条。

| 维度 | 取值 |
| --- | --- |
| 目标类型 | 角色（直接持有）、用户、部门（含下级，只在租户侧） |
| 读取方式（`FieldMaskStrategy`） | `None` 明文、`Hidden` 隐藏、`FullMask` 全部星号、`PartialMask` 部分脱敏（保留前后位数）、`Hash` 哈希（SHA-256 前 16 位）、`Redact` 固定文本 |
| 能否改写 | 只读 / 可写 |

### 命中与合并

- 用户规则认本人；角色规则认此刻持有的角色（令牌声明与有效指派的交集，只认直接持有，不看继承）；部门规则认所在部门及其上级（停用的不算）。
- 多条命中：读取方式**取最严**、部分脱敏的保留位数**取最少**、任一只读即只读。
- 启用的规则按（侧、实体）缓存 60 秒，写入即失效；规则只在所属一侧生效。

### 各包怎么套用

实体与字段要先登记（`AddFieldSecurityCatalog`），再在三处显式调用 `IFieldSecurity`（`Runtime` 的 `FieldSecurityQuery.ResolveAsync` 取合并后的 `FieldRules`）：

| 位置 | 行为 |
| --- | --- |
| 投影 | 响应 DTO 投影时按读取方式打码 |
| 查询门控 | 按受保护字段筛选或排序 → `<包>_DENIED_*`（用户目录是 `IDENTITY_DENIED_011`）；关键字搜索不再覆盖受保护字段，能搜的字段都受保护时直接返回空页 |
| 写入 | 新建时填了只读字段、修改时改了只读字段 → 拒绝（`IDENTITY_DENIED_012`）；修改时交回原值、脱敏值或空值即保留原值 |

本人自己的资料（登录、刷新、个人中心）不套用。

目前登记的实体只有用户目录 `identity.users`：登录名、显示名、邮箱、手机号、最近登录。其他实体的字段安全随后续收尾补登记。

### 管理操作

| 操作 | 权限码 |
| --- | --- |
| `field-security.entities` | `access.field-security.read`：已登记的实体与字段（显示名用各包的文案键） |
| `field-security.list` / `get` | `access.field-security.read` |
| `field-security.create` / `update` / `delete` | `access.field-security.create` / `update` / `delete` |

校验：实体或字段不在目录、读取方式不适用（非文本只能明文或隐藏，不可空的不能隐藏）、明文且可改写、保留位数不合规、固定文本缺失或过长都有各自的 `ACCESS_VALIDATION_*`。删除角色时连带删除以它为目标的规则；新建、修改、删除进权限变更日志。

## 前端

管理端不实现安全边界，只改善可用性：受保护的列显示后端给的打码值，只读字段在表单里置灰。见[前端权限与脱敏](../frontend/permission)。

## 下一步

- [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [组织架构](./organization)
