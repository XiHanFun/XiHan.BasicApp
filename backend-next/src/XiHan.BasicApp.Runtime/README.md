# XiHan.BasicApp.Runtime

Tier 0 基座：把框架的持久化、租户与管道接到 BasicApp 的约定上。提供能力 `basicapp.runtime@1`，各能力包在清单里依赖它。

| 类型 | 用途 |
| --- | --- |
| `SqlSugarRuntime.Configure` | SqlSugar 方式一回调：`XiHan:Bindings:db` 的连接串与 `provider`（缺省 PostgreSQL）、`AopEvents.DataExecuting` 填操作人、表名与列名 snake_case；协议约定由框架随后施加。租户分片的绑定由框架按租户属性 `ConnectionString` / `DatabaseProvider` 推出后交到同一回调 |
| `AuditorStampMiddleware` + `AuditorStamp` | Transaction 阶段把当前主体 ID 放进异步本地值，回调据此填 `CreatedBy` / `ModifiedBy` / `DeletedBy`。方式一的 `ConnectionConfig` 在装配时只建一份、没有作用域，回调拿不到作用域服务，异步本地值是能读到当前主体的方式（N0 冒烟结论） |
| `TenantScopedQuery.For<T>` | 包内 `Queries/` 的起手式：按实体落点取连接（`SqlSugarUnitOfWork.ConnectionFor`），补软删除与租户条件；`OwnedByCurrentTenantOrPlatform` / `OwnedByCurrentTenant` 给 `IOwnedByTenant` 实体用 |
| `DataScopeQuery` | 数据范围：取当前主体的判定（没装 Access 即不受限），`ApplyDataScope` 按归属人列 / 部门列、`ApplyDataScopeByCreator` 按审计列 `CreatedBy` 套到查询上 |
| `ActiveUniqueIndexes` | 软删除实体的部分唯一索引（实体上的 `[ActiveUniqueIndex]`，`… WHERE is_deleted = false`）：`RuntimeFeature` 在框架建表之后、`TenantSchemas` 初始化独立库时补建 |
| `DatabaseProviders` / `SnakeCaseNaming` | 库类型名映射；命名约定 |
| `ISecretProtector` | 可逆加密（ASP.NET Core Data Protection）：按用途隔离，密文带 `dp:` 前缀，解不开即失败、不回退明文；多实例共享密钥时配 `BasicApp:DataProtection:KeysDirectory` |

## 配置

```json
{ "XiHan": { "Bindings": { "db": { "type": "connection:relational", "value": "Host=…;Database=…", "provider": "postgresql" } } } }
```

数据库是启动硬要求：绑定缺失以 `RUNTIME_VALIDATION_001` 中断启动，不回退到任何内存实现。

建表时没写长度与类型的字符串列一律 `varchar(512)`（`SqlSugarRuntime.DefaultStringLength`）；写了 `[LongText]` / `[StringLength]` / `[MaxLength]` 的列交给框架约定（长文本或声明的长度），集成测试 `PersistenceMappingTests` 守住。
