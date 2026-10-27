# XiHan.BasicApp.Host

6.0 宿主（方案 5.13）。装配：Full Profile + `SqlSugarPersistenceFeature(SqlSugarRuntime.Configure)` + `JwtSecurityFeature` + `DatabaseMultiTenancyFeature` / `DatabaseSecurityFeature` / `DatabaseAuditingFeature` + `OutboxFeature` + `RuntimeFeature` + 各能力包。

| 端点 | 来源 |
| --- | --- |
| `/health/startup`、`/health/ready`、`/health/live` | `MapXiHanHealth` |
| `/api/**` | `MapXiHanOperations`：操作 ID 推导路由，前缀 `XiHan:Protocols:Http:RoutePrefix = /api` |
| `/openapi.json`、`/scalar` | `BasicApp:ApiDocs:Enabled`（缺省只在 Development 打开）；一份文档、一个生成器 |
| `/mcp` | `MapXiHanMcp`：`Expose.Mcp` 的操作 |
| `/xihan.basicapp.<Service>/<Method>`、`/operations.proto` | `MapXiHanGrpc` / `MapXiHanProto`：`Expose.Grpc` 的操作，Kestrel 的 `Grpc` 端点（HTTP/2，只监听 127.0.0.1） |
| `/` | 应用名、版本与环境 |

## 运行

```bash
# 需要本地 PostgreSQL（appsettings.Development.json：127.0.0.1:5432，postgres/postgres）并先建库
createdb -h 127.0.0.1 -U postgres xihan_basicapp_next
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development
```

Development 打开 `XiHan:Persistence:SqlSugar:InitTables`，启动按已登记的实体建表（含框架 Provider 的 `tenant_record`、`permission_grant_record`、`audit_record`、`outbox_message`）；表结构的正式管理（`Schema/<版本>/*.sql` + 版本台账）随 N1 落地。

## 配置

- `XiHan:Bindings:db`：平台库连接串（`value`）与库类型（`provider`，缺省 `postgresql`）。缺失即 `RUNTIME_VALIDATION_001` 中断启动。
- `XiHan:Security:Jwt:Secret`：HS256 密钥，至少 32 字节；`appsettings.json` 留空，由环境配置或密钥存储提供。不提交真实密钥。
- `BasicApp:ApiDocs:Enabled`：是否映射 `/openapi.json` 与 `/scalar`；没写时 Development 为真、其余为假。

## 命令

```bash
# 导出接口文档（不启动应用、不连数据库）；前端 gen-contracts 与 CI 漂移门禁读它
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- contracts export --out frontend-next/contracts
```

## N0 的开发数据

N0 没有登录与种子：

1. 用 `frontend-next/scripts/dev-token.mjs` 按 Development 的密钥签一枚令牌（`sub`、`tenant`、`role` 声明）。
2. 在平台库里准备租户与授权行（N1 的 `seed` 命令取代这一步）：

```sql
INSERT INTO tenant_record (id, display_name, is_enabled, properties_json, created_at, concurrency_stamp)
VALUES ('acme', 'Acme', true, '{}', now(), md5(random()::text));
INSERT INTO permission_grant_record (id, subject_kind, subject_id, permission, tenant_id, created_at)
VALUES (gen_random_uuid(), 'role', 'admin', 'identity.users.read', '', now()),
       (gen_random_uuid(), 'role', 'admin', 'identity.users.create', '', now());
```
