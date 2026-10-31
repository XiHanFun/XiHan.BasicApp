# 数据库配置

6.0 用 SqlSugar 作为数据层（框架 `Persistence.SqlSugar` 的「方式一」），**PostgreSQL 是缺省与唯一经过 CI 验证的库**；集成测试另用 SQLite 跑快速用例。不引入 EF Core 作为平行的业务数据层。

## 平台库连接

连接写在框架的绑定节 `XiHan:Bindings:db`：

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

- 数据库是**启动硬要求**：绑定缺失即 `RUNTIME_VALIDATION_001` 中断启动，不回退到任何内存实现。
- `provider` 缺省 `postgresql`，由 `Runtime` 的 `DatabaseProviders` 映射成 SqlSugar 的库类型。
- 不提交真实连接串：`appsettings.json` 留空，开发值写在 `appsettings.Development.json`，部署用环境变量 `XiHan__Bindings__db__value`。

## 方式一回调：`SqlSugarRuntime.Configure`

宿主以 `new SqlSugarPersistenceFeature(SqlSugarRuntime.Configure)` 装配持久化，回调负责：

| 项 | 内容 |
| --- | --- |
| 库类型与连接串 | 读 `XiHan:Bindings:db` |
| 命名约定 | 表名、列名 snake_case（`SysUserRole` → `sys_user_role`） |
| 操作人 | `AopEvents.DataExecuting` 填 `CreatedBy` / `ModifiedBy` / `DeletedBy` |
| 字符串缺省长度 | 没有 `[LongText]` / `[StringLength]` / `[MaxLength]` 的列一律 `varchar(512)` |
| 差异日志 | 宿主设 `DiffLog = DiffLogBuffer.IsLogged`：全审计实体的写入带差异事件，供数据变更日志使用 |

框架随后施加协议约定（并发戳、软删除、租户列、时间转换）。

## 多库：默认连接与租户分片

作用域客户端带两个连接：`default`（平台库）与 `shard`（带分片的租户调用才有）。实体落点：

1. `[Tenant(configId)]` 显式指定的，按指定；
2. 实现 `IMultiTenant` 的实体落 `shard`；没有独立库的租户（字段隔离）调用没有 `shard` 连接，这些实体与平台库同库、靠 `tenant_id` 过滤；
3. 其余落默认连接。

这正是「库决定协议」：平台库实体不实现 `IMultiTenant`，永远在默认连接；租户库实体实现它，随租户分库。独立库租户的连接串存在租户目录（`TenantRecord` 的 `ConnectionString` / `DatabaseProvider` 属性），以 `ISecretProtector` 密文保存，由框架按租户目录建分片连接、Runtime 解密。

::: warning 跨库事务不是两阶段提交
工作单元经 `AsTenant()` 在全部连接上开启、提交、回滚，但提交是**顺序**的：第二个连接提交失败时第一个已经提交。需要严格原子的写入放在同一个库里；租户开通这类同时写平台库与租户库的路径由操作做补偿。
:::

## 数据访问的三种方式

| 场景 | 用什么 | 约束 |
| --- | --- | --- |
| 按 ID 取、按规约列、计数、增删改、批量 | `IRepository<TEntity, Guid>`（源生成登记） | 自动处理租户、软删除、并发戳、时间 |
| 分页、投影、联表、聚合 | 包内 `Queries/XxxQueries.cs`，用 `ISqlSugarClient` | **必须**以 `TenantScopedQuery.For<T>(client, context)` 起手（补租户与软删除条件），连接经 `SqlSugarUnitOfWork.ConnectionFor(typeof(T))` 取 |
| 按月分表的日志 | 仓储按 `[SplitTable(SplitType.Month)]` 自动走分表入口，`EntityQuery<T>.Within(时间范围)` 限定扫描的表 | 跨表聚合仍走 `Queries/` |

框架没有全局查询过滤器：直接用 `ISqlSugarClient` 而不经 `TenantScopedQuery` 起手，就会漏掉租户与软删除条件。

## 建表：开发与正式两条路

| 环境 | 方式 |
| --- | --- |
| 开发 | `XiHan:Persistence:SqlSugar:InitTables = true`（Development 缺省）：启动时按已登记的实体建缺表、加缺列 |
| 正式 | `Host/Schema/<版本>/*.sql` + 版本台账，由表结构升级器在启动时执行，见[表结构升级](./upgrade) |

`InitTables` 只建缺表、加缺列。给**已有行**的表新增非可空列时：数值、枚举、布尔、`Guid` 列在 PostgreSQL 上按 CLR 缺省值回填；非可空的字符串与日期时间列不受支持——实体里声明成可空，或重建开发库。

## 测试库

| 用例 | 库 |
| --- | --- |
| 绝大多数集成测试 | SQLite 文件库，经 TestServer 走完整管道 |
| PostgreSQL 用例（两库冒烟、并发发号、Conformance、升级基线） | 读环境变量 `XIHAN_TEST_POSTGRES`（服务器级连接串，不含库名），没设置时跳过 |

CI（`next-ci.yml`）起一个 `postgres:17-alpine` 服务并设置 `XIHAN_TEST_POSTGRES`。

## 下一步

- [数据模型](./data-model) · [表结构升级](./upgrade) · [多租户 SaaS](./multi-tenancy)
