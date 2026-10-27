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

## N0 的暂定项（N1 收口）

- `users.create` 同时暴露到 HTTP / gRPC / MCP 只为验证三种协议；按方案 5.4，管理类写操作最终只 `Http`，内部服务需要时再加 `Grpc`。
- 创建用户要求当前租户（租户主体自助建用户）；平台主体代建的入参形态随 N1 的操作表定。
- 没有登录、令牌签发与种子：测试用 `JwtOptions` 的密钥自签令牌，开发环境按 Host README 手工准备租户与授权行。
