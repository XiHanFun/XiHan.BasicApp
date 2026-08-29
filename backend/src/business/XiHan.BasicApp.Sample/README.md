# XiHan.BasicApp.Sample

仓库里最小的一个业务模块，用来回答「在 XiHan.BasicApp 上加一块自己的业务，要写哪些东西」。

答案是三样：**一个模块类 + 若干实体 + 若干仓储**。表由框架在启动时按实体自动建，
仓储继承 `SaasRepository<T>` 即自动注册进容器，都不需要手写登记。

## 文件

| 文件 | 作用 |
| --- | --- |
| `XiHanBasicAppSampleModule.cs` | 模块类：`[DependsOn(Saas)]`，服务注册与启动自检 |
| `Domain/Entities/SampleNote.cs` | 普通业务实体，未声明模块数据源 |
| `Domain/Entities/SampleErpOrder.cs` | 标了 `[ModuleDataSource("Erp")]` 的实体 |
| `Domain/Entities/SampleModuleDataSources.cs` | 模块数据源名常量 |
| `Infrastructure/Repositories/*.cs` | 两个仓储，写法完全一样 |
| `Infrastructure/MultiTenancy/*.cs` | 给库隔离租户补挂自己模块库的提供器与其配置 |

## 顺带演示：模块分库 × 租户分库

两条**正交**的维度。租户维度先定「用哪一套布局」——一个主库加上挂在它下面的模块库；
模块维度再在这套布局内部选库：

```text
                     ┌── SampleNote ─────→ Default（主库）
平台态 / 字段隔离租户 ┤
                     └── SampleErpOrder ─→ Default_Erp（所有租户共享的模块库）

                     ┌── SampleNote ─────→ Tenant_{租户Id}（该租户的主库）
库隔离租户（不配模块）┤
                     └── SampleErpOrder ─→ Default_Erp（回落共享模块库）

                     ┌── SampleNote ─────→ Tenant_{租户Id}（该租户的主库）
库隔离租户（配了模块）┤
                     └── SampleErpOrder ─→ Tenant_{租户Id}_Erp（该租户的模块库）
```

模块库的连接标识由主连接派生（`{主连接}_{模块名}`），所以模块名不占用顶层 `ConfigId` 命名空间，
跟租户连接标识撞不上；同一个模块在不同布局下自然是不同的库。

注意两个仓储的写法**完全一样**——落哪个库由实体上的特性和当前租户决定，仓储这层不需要知道。

启动时模块会把实际解析结果打进日志，配置是否生效一眼可见：

```text
[平台态] SampleNote → 连接 Default（PostgreSQL）
[平台态] SampleErpOrder → 连接 Default_Erp（PostgreSQL）
```

## 模块库怎么配

模块库挂在主连接下面，条目只写模块名和连接串，其余字段留空即继承主连接：

```json
{
  "ConfigId": "Default",
  "ConnectionString": "Server=127.0.0.1;Database=XiHanBasicApp;...",
  "DbType": "PostgreSQL",
  "IsAutoCloseConnection": true,
  "ModuleDataSourceConfigs": [
    {
      "ModuleDataSource": "Erp",
      "ConnectionString": "Server=127.0.0.1;Database=XiHanBasicAppErp;..."
    }
  ]
}
```

- 连接串**留空**：该模块不单独分库，直接用主库
- 条目**整条缺失**：视为未配置，实体解析时直接抛异常，不会静默落到主库

## 验证二维路由

默认配置下所有租户共用一个 `Default_Erp` 模块库，与不启用该特性时行为一致。要看到二维效果：

1. 启动一次，让种子把演示租户建出来
2. 在 SaaS 租户管理里把某个租户改成**库隔离**并填上它的主库连接串
3. 从 `sys_tenant` 表取这个租户 Id，填进 `appsettings.Development.json` 的 `Sample:TenantModuleDataSources`：

```json
"Sample": {
  "TenantModuleDataSources": {
    "1962xxxxxxxxxxxxx": {
      "Erp": "Server=127.0.0.1;Database=XiHanBasicAppErp_T1;Username=postgres;Password=postgres;TrustServerCertificate=true;"
    }
  }
}
```

4. 重启，日志里会多出这一组，同一个实体落到了不同的库：

```text
[租户 1962xxxxxxxxxxxxx] SampleNote → 连接 Tenant_1962xxxxxxxxxxxxx（PostgreSQL）
[租户 1962xxxxxxxxxxxxx] SampleErpOrder → 连接 Tenant_1962xxxxxxxxxxxxx_Erp（PostgreSQL）
```

**一行代码都不用改。**

第 3 步只做模块维度：跳过它，该租户的 `SampleErpOrder` 会回落共享的 `Default_Erp`——
「租户主库独立、模块库仍共享」是默认行为，不需要配置。

租户 Id 用配置而不是常量，是因为它是雪花值、由种子在运行期生成，没有编译期常量可用。
真实业务若要把这份映射存进数据库，照 `SaasTenantConnectionProvider` 读 `SysTenant` 的写法做即可
（记得读元数据时切平台上下文并自行缓存）。

## 不需要它的时候

删掉本工程、`XiHan.BasicApp.slnx` 里的登记、WebHost 的 `ProjectReference` 与 `[DependsOn]`，
再删掉 appsettings 里的 `Sample` 节与主连接下的 `ModuleDataSourceConfigs` 即可。

⚠️ 已经建过表的库里，`Sample_Note` 与 `Sample_Erp_Order` 不会自动回收，需要自行清理。
