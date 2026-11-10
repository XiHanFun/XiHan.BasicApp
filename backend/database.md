来源：https://basicapp.docs.xihanfun.com/backend/database

# 数据库配置

本页说明数据库连接、从库与 SQL 日志、多库落点、数据访问方式与建表策略，适用于后端开发者与部署人员。

数据层是 SqlSugar（框架 `Persistence.SqlSugar` 的「方式一」：应用提供连接配置回调）。PostgreSQL 14+ 是缺省库，表结构脚本只为它写，CI 对它跑真实库用例；集成测试另用 SQLite 跑快速用例。不引入 EF Core 作为平行的业务数据层。

## 平台库连接

连接写在框架的服务绑定 `XiHan:Bindings:db`：

```json
{
  "XiHan": {
    "Bindings": {
      "db": {
        "type": "connection:relational",
        "value": "Host=<主机>;Port=5432;Database=<库名>;Username=<用户>;Password=<密码>",
        "provider": "postgresql"
      }
    }
  }
}
```

- 数据库是启动硬要求：绑定缺失或为空即 `RUNTIME_VALIDATION_001` 中断启动，不回退到任何内存实现。
- `provider` 不写即 `postgresql`（也认 `postgres` / `pgsql` / `npgsql`），另认 `sqlite`、`mysql`、`sqlserver`（`mssql`）；写了别的即 `RUNTIME_VALIDATION_002`。
- 不提交真实连接串：`appsettings.json` 不写绑定，本机开发写在 `appsettings.Development.json`，部署用环境变量 `XiHan__Bindings__db__value` 或 Kubernetes Service Binding。
- 语句超时 `XiHan:Persistence:SqlSugar:CommandTimeoutSeconds`（缺省 300 秒）。

## 从库与 SQL 日志

`BasicApp:Database` 只管平台库：

| 键 | 缺省 | 说明 |
| --- | --- | --- |
| `Replicas` | 不配 | 平台库的从库列表，每项 `ConnectionString` 必填、`HitRate` 是读权重。配了之后只读工作单元读从库，会写的工作单元读写都走主库 |
| `LogSql` | `false` | 记录每条 SQL（只记语句，不记参数值），只在排查时打开 |
| `LogSqlErrors` | `true` | 记录执行出错的 SQL |
| `LogSlowSql` | `true` | 记录慢 SQL |
| `SlowSqlThresholdMilliseconds` | `1000` | 慢 SQL 阈值 |

从库只给平台库，独立库租户读写都走它自己的库。从库的缺省权重、健康探测、摘除与恢复在 `XiHan:Persistence:SqlSugar:Replicas`。决定授权的读取（会话门禁、角色指派、权限规则、访问策略等）经 `StoreScope.ReadPrimaryAsync` 钉在主库，不读落后的从库。SQL 日志的类别是 `XiHan.BasicApp.Sql`。

## 方式一回调：`SqlSugarRuntime`

宿主以 `new SqlSugarPersistenceFeature(SqlSugarRuntime.ConfigureWith(configuration)) { DiffLog = DiffLogBuffer.IsLogged }` 装配持久化。回调对默认连接调用一次，带分片的调用再对 `shard` 连接调用一次：

| 项 | 内容 |
| --- | --- |
| 库类型与连接串 | 读绑定 `db`（分片连接由框架按租户目录推出） |
| 命名约定 | 表名、列名 snake_case（`SysUserRole` → `sys_user_role`）；显式写了 `[SugarTable]` / `[SugarColumn]` 名字的不改 |
| 操作人 | `AopEvents.DataExecuting` 填 `CreatedBy` / `ModifiedBy` / `DeletedBy` |
| 字符串缺省长度 | 没有 `[LongText]` / `[StringLength]` / `[MaxLength]` 的字符串列一律 `varchar(512)` |
| 差异日志 | 全审计实体的写入带差异事件，供数据变更日志使用；标了 `[DiffLogIgnored]` 的实体除外 |
| 从库与 SQL 日志 | 按 `BasicApp:Database` 挂上 |

框架随后施加协议约定：主键、并发戳（长度 32）、软删除、租户列、`DateTimeOffset` 按 UTC 往返、可空列。

## 多库：默认连接与租户分片

作用域客户端带两个连接：`default`（平台库）与 `shard`（带分片的租户调用才有）。框架按顺序决定实体落点：

1. 实体带 `[Tenant(configId)]` 的，落指定的连接；
2. 实现 `IMultiTenant` 且调用有 `shard` 连接的，落分片连接；
3. 其余落默认连接。

共库（字段隔离）的租户调用没有 `shard` 连接，租户库实体与平台库同库、靠 `tenant_id` 过滤。独立库租户的连接串存在租户目录（`TenantRecord` 的 `ConnectionString` / `DatabaseProvider` 属性），以 `ISecretProtector` 密文保存；框架按租户目录建分片连接，Runtime 的 `ProtectedTenantShardResolver` 解密，解不开即 `RUNTIME_UNAVAILABLE_004`，不降级为共库。

::: warning 跨库事务不是两阶段提交
工作单元在第一次写入时开事务，之后首次取用的连接再补开；提交与回滚逐连接进行，是顺序提交：第二个连接提交失败时第一个已经提交。需要严格原子的写入放在同一个库里。租户开通只写平台库（租户与框架租户目录同一个工作单元）；独立库的建库建表是单独的操作 `tenants.initialize-database`，结果写回租户，失败可改连接后重试。
:::

## 数据访问

| 场景 | 用什么 | 约束 |
| --- | --- | --- |
| 按 ID 取、按规约列、计数、增删改、批量 | `IRepository<TEntity, Guid>`（Feature 里 `AddGeneratedRepositories()` 登记） | 自动处理租户、软删除、并发戳、时间；`EntityQuery` 的 `Take` 只收 1–1000 |
| 分页、投影、联表、聚合 | 包内 `Queries/`，用 `ISqlSugarClient` | 必须以 `TenantScopedQuery.For<T>(context)` 或 `For<T>(client, context)` 起手（按落点取连接，补软删除与租户条件）；平台库里分侧的实体再接 `OwnedByCurrentTenantOrPlatform(context)` 或 `OwnedByCurrentTenant(context)` |
| 平台一侧的租户库实体 | Runtime 的 `SideRows` | 框架仓储要求调用带租户；平台一侧的行（`platform`）经它按侧读写，写之前开事务 |
| 按月分表的日志 | 仓储按 `[SplitTable(SplitType.Month)]` 自动走分表入口，`EntityQuery<T>.Within(from, to)` 限定扫描的表；`Queries/` 里用 `.SplitTable(from, to)` | 不给时间范围即扫全部分表 |
| 独立于调用事务的读写 | `Runtime.Persistence.StoreScope.ReadAsync` / `ReadPrimaryAsync` / `WriteAsync` | 失败计数、会话门禁、读穿缓存的加载 |
| 后台对租户库的工作 | 框架 `TenantShardScope` 或 Contracts 的 `ITenantScopeRunner` | 在自建作用域里按租户绑定分片 |

框架没有全局查询过滤器：直接用 `ISqlSugarClient` 而不经 `TenantScopedQuery` 起手，就会漏掉租户与软删除条件。直接用客户端写入（`Insertable` 等）之前要先调 `SqlSugarUnitOfWork.EnsureTransactionAsync`，写入才在本次调用的事务里。

只读操作（动词 `get` / `list` 或声明 `ReadOnly = true`）跑在只读工作单元里，写入即 500 `PERSISTENCE_UNEXPECTED_001`；要补建的行放到写操作里建，读路径缺行按缺省值读。

## 建表策略

| 环境 | 方式 |
| --- | --- |
| 开发 | `XiHan:Persistence:SqlSugar:InitTables` 为 `true`（`appsettings.Development.json` 已打开）：启动时按已登记的实体建缺表、加缺列，再补建部分唯一索引 |
| 正式 | `Host/Schema/<版本>/*.sql` 与版本台账，由表结构升级器在启动时执行，见[表结构升级](./upgrade) |

`InitTables` 只建缺表、加缺列。给已有行的表新增非可空列时：数值、枚举、布尔、`Guid` 列在 PostgreSQL 上按 CLR 缺省值回填；非可空的字符串与日期时间列不受支持，应在实体里声明成可空，或重建开发库。

## 测试库

| 用例 | 库 |
| --- | --- |
| 绝大多数集成测试 | SQLite 临时库，经 `BasicAppTestServer` 走完整管道 |
| PostgreSQL 用例（两库冒烟、从库读主库、并发发号、Provider 一致性、表结构基线等） | 读环境变量 `XIHAN_TEST_POSTGRES`（服务器级连接串，不含库名），测试自建临时库；没设置即跳过 |

CI（`.github/workflows/ci.yml`）起一个 `postgres:17-alpine` 服务并设置 `XIHAN_TEST_POSTGRES`。

## 下一步

- [数据模型](./data-model) · [表结构升级](./upgrade) · [多租户 SaaS](./multi-tenancy)
