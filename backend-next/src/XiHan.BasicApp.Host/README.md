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

Development 打开 `XiHan:Persistence:SqlSugar:InitTables`，启动按已登记的实体建表（含框架 Provider 的 `tenant_record`、`permission_grant_record`、`audit_record`、`outbox_message`），软删除实体的部分唯一索引随后补建。生产库的表结构由 `Schema/<版本>/*.sql` 与版本台账管理（`schema upgrade` 命令，或 `BasicApp:Schema:AutoUpgrade` 开着时启动自动执行），6.0.0 基线是 `Schema/6.0.0/0001-baseline.sql`。

## 配置

- `XiHan:Bindings:db`：平台库连接串（`value`）与库类型（`provider`，缺省 `postgresql`）。缺失即 `RUNTIME_VALIDATION_001` 中断启动。
- `XiHan:Security:Jwt:Secret`：HS256 密钥，至少 32 字节；`appsettings.json` 留空，由环境配置或密钥存储提供。不提交真实密钥。
- `BasicApp:ApiDocs:Enabled`：是否映射 `/openapi.json` 与 `/scalar`；没写时 Development 为真、其余为假。
- `BasicApp:Auth`：访问令牌分钟数、刷新令牌天数、登录失败上限与锁定分钟数、密码最短长度与历史深度、会话门禁缓存秒数（缺省见 `appsettings.json`）。
- `BasicApp:Seed`：`EnableDemoData`（演示数据，Development 打开）与 `SuperAdminPassword`（初始超级管理员密码；留空沿用 `SuperAdmin@123`，部署前改掉）。

## 命令

```bash
# 导出接口文档（不启动应用、不连数据库）；前端 gen-contracts 与 CI 漂移门禁读它。
# dotnet run 以工程目录为工作目录，--out 给绝对路径（仓库根执行时用 $PWD）
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- contracts export --out "$PWD/frontend-next/contracts"

# 种子：按号段顺序执行各包登记的种子（可重复执行；--list 只列出，--only identity.platform 只跑指定的）
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- seed

# 框架内建命令：help / graph / pipeline / operations / invoke <操作 ID> [--json ...]
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- operations
```

命令模式用同一套装配启动宿主但不监听端口（不与正在运行的实例争抢 9710 / 9711）；命令经完整内核管道执行，日志只写 stderr，stdout 只承载命令输出（成功是 JSON，失败是 `{"code","detail"}`，退出码按错误 KIND）。

## 初始账号

`seed` 写入平台账号 `superadmin`（密码来自 `BasicApp:Seed:SuperAdminPassword`，缺省 `SuperAdmin@123`）与它的全局授予 `*`。用它登录（`POST /api/auth:login`，租户留空），再创建租户与租户用户。

Development 打开 `BasicApp:Seed:EnableDemoData` 时，`tenancy.demo` 种子另写演示租户 `demo`（管理员 `admin` / `Demo@123456`，首次登录须改密），`organization.demo` 再在该租户里写部门 `hq → rd / mkt`、岗位 `manager / engineer` 并把管理员放进总部；开通正式租户走 `POST /api/tenants` 再 `POST /api/tenants/{id}:initialize-admin`。
