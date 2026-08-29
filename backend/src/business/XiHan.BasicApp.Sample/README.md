# XiHan.BasicApp.Sample

仓库里最小的一个业务模块，用来回答「在 XiHan.BasicApp 上加一块自己的业务，要写哪些东西」。

答案是三样：**一个模块类 + 若干实体 + 若干仓储**。表由框架在启动时按实体自动建，
仓储继承 `SaasRepository<T>` 即自动注册进容器，都不需要手写登记。

## 文件

| 文件 | 作用 |
| --- | --- |
| `XiHanBasicAppSampleModule.cs` | 模块类：`[DependsOn(Saas)]`，服务注册与启动自检 |
| `Domain/Entities/SampleNote.cs` | 普通业务实体，未声明数据源 |
| `Domain/Entities/SampleErpOrder.cs` | 标了 `[DataSource("Erp")]` 的实体 |
| `Domain/Entities/SampleDataSources.cs` | 逻辑数据源名常量 |
| `Infrastructure/Repositories/*.cs` | 两个仓储，写法完全一样 |
| `Infrastructure/MultiTenancy/*.cs` | 租户级数据源提供器与其配置 |

## 顺带演示：模块分库 × 租户分库

两条**正交**的维度。实体只声明「属于哪个逻辑数据源」，落到哪条连接由「数据源名 + 当前租户」共同决定：

```text
                    ┌── SampleNote ─────→ Default（主库）
平台态 / 普通租户 ──┤
                    └── SampleErpOrder ─→ Erp（所有租户共享的模块库）

                    ┌── SampleNote ─────→ Tenant_{租户Id}（该租户的主库）
配了独立库的租户 ───┤
                    └── SampleErpOrder ─→ Erp_Tenant_{租户Id}（该租户的模块库）
```

注意两个仓储的写法**完全一样**——落哪个库由实体上的特性和当前租户决定，仓储这层不需要知道。

启动时模块会把实际解析结果打进日志，配置是否生效一眼可见：

```text
[平台态] SampleNote → 连接 Default（PostgreSQL）
[平台态] SampleErpOrder → 连接 Erp（PostgreSQL）
```

## 验证二维路由

默认配置下所有租户共用一个 `Erp` 库，与不启用该特性时行为一致。要看到二维效果：

1. 启动一次，让种子把演示租户建出来
2. 从 `sys_tenant` 表取一个租户 Id
3. 填进 `appsettings.Development.json` 的 `Sample:TenantDataSources:Erp`：

```json
"Sample": {
  "TenantDataSources": {
    "Erp": {
      "1962xxxxxxxxxxxxx": "Server=127.0.0.1;Database=XiHanBasicAppErp_T1;Username=postgres;Password=postgres;TrustServerCertificate=true;"
    }
  }
}
```

4. 重启，日志里会多出这一组，同一个实体落到了不同的库：

```text
[租户 1962xxxxxxxxxxxxx] SampleNote → 连接 Default（PostgreSQL）
[租户 1962xxxxxxxxxxxxx] SampleErpOrder → 连接 Erp_Tenant_1962xxxxxxxxxxxxx（PostgreSQL）
```

**一行代码都不用改。**

租户 Id 用配置而不是常量，是因为它是雪花值、由种子在运行期生成，没有编译期常量可用。
真实业务若要把这份映射存进数据库，照 `SaasTenantConnectionProvider` 读 `SysTenant` 的写法做即可
（记得读元数据时切平台上下文并自行缓存）。

## 不需要它的时候

删掉本工程、`XiHan.BasicApp.slnx` 里的登记、WebHost 的 `ProjectReference` 与 `[DependsOn]`，
再删掉 appsettings 里的 `Sample` 节与 `ConfigId` 为 `Erp` 的连接即可。

⚠️ 已经建过表的库里，`Sample_Note` 与 `Sample_Erp_Order` 不会自动回收，需要自行清理。
