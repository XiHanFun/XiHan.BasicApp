# XiHan.BasicApp.Contracts

Tier 0 基座：跨包共享的契约，不含任何装配与基础设施。

| 目录 | 内容 |
| --- | --- |
| `Entities/` | 应用审计协议 `IHasCreator` / `IHasModifier` / `IHasDeleter`（框架只维护时间字段，操作人由 `Runtime` 写库时填充）、`IOwnedByTenant`；应用基类 `AppEntity` → `AppRecord` → `AppAuditedEntity` → `AppAggregateRoot`，以及加了 `IMultiTenant` 的 `TenantScoped*` 变体（只用于租户库实体） |
| `Paging/` | `PageRequest { Page { Index, Size }, Filters[], Keyword, Sorts[] }`（页大小上限 500，约束折进 Schema）与 `PageResult<T> { Items, Total, Page }` |
| `Batch/` | `BatchResult { Succeeded[], Failed[{ id, code }] }`：批量优先，集合入参的写操作统一返回它 |
| `Security/` | `PermissionDescriptor`（码、显示名、作用侧、是否审计、说明）与三段权限码文法；字段安全的 `IFieldSecurity`（由 Access 实现）、合并后的 `FieldRules`（打码、查询门控与写入决定）、`FieldMaskStrategy` 与实体登记 `AddFieldSecurityCatalog` |
| `Settings/` | `IConfigValueReader`（参数的生效值：租户覆盖优先、停用视为不存在、没有值回落缺省值、加密值解密后返回）与 `INumberGenerator`（按规则码发号，带幂等键；`NumberRequest` / `NumberAllocation` / `NumberingScope`），由 `Settings` 包实现 |
| `AppInfo` / `TenantIds` | 应用名与版本；租户 ID 文法与平台属主常量 `platform` |

## 规则

- 库决定协议：平台库实体不实现 `IMultiTenant`，需要属主用 `IOwnedByTenant`；租户库实体用 `TenantScoped*` 基类，由框架仓储严格过滤并随租户分库。
- `TenantScoped*` 即方案 5.5 的「`TenantXxx` 变体」，取这个名字是避免与框架 `MultiTenancy.Database` 的租户目录实体 `TenantRecord` 同名。
- 权限码 `<包>.<资源>.<动作>`，操作 ID 的资源段与权限码的资源段同词；按钮即操作，没有第二套按钮码。
