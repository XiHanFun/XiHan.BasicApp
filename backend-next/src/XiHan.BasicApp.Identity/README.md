# XiHan.BasicApp.Identity

Tier 1 身份能力包。N0 是最小切片，用来验证骨架：一个带租户、点分三段权限码、事务、分页、DataAnnotations 校验的操作在 HTTP、gRPC、MCP 三个协议上服务。

| 文件 | 内容 |
| --- | --- |
| `IdentityFeature` | 清单 `xihan.basicapp.identity`（提供 `basicapp.identity@1`，依赖 `persistence.relational@1`、`security.authorization@1`、`multitenancy.context@1`、`basicapp.runtime@1`）；显式登记仓储、查询、权限、操作 |
| `IdentityPermissions` | `identity.users.read`、`identity.users.create`（`PermissionDescriptor`：作用侧 Both，创建进审计） |
| `IdentityErrors` / `IdentityMessages` / `Localization/` | `IDENTITY_<KIND>_<NNN>` 错误码、消息键与 10 种区域文案 |
| `Domain/Entities` | `SysUser`（平台库，`HomeTenantId` 列，不实现 `IMultiTenant`）、`SysUserStatistics`（租户库，`TenantScopedAuditedEntity`，随租户分库） |
| `Operations/Users` | `users.create`（`POST /api/users`，201）、`users.get`（`GET /api/users/{id}`）、`users.list`（`POST /api/users:list`，带 `PageRequest`）；每个操作显式写 `Expose` |
| `Queries/UserQueries` | 直接用 SqlSugar 客户端的分页查询，以 `TenantScopedQuery.For<SysUser>` 起手 |

## 暴露面（方案 5.4 操作表）

- 管理类写操作只 `Http`；`users.get` / `users.list` 是用户目录，另加 `Mcp | Ai` 与 `Grpc`（内部服务查用户），gRPC 与 MCP 的冒烟落在这两个上。
- 操作表由 `Architecture.Tests` 的 `OperationExposureTests` 守住：超出 `Http` 的操作逐个登记，没登记的必须恰好是 `Http`。
