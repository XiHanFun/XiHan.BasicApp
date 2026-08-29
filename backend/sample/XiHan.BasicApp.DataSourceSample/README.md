# 实体数据源（模块分库）验证示例

验证框架 `XiHan.Framework.Data` 的实体数据源路由：实体标 `[DataSource("Erp")]` 后，
它的仓储读写与建表都固定落在该 `ConfigId` 的库上，而租户上下文保持统一。

用 SQLite 起三个库文件（`default.db` / `erp.db` / `mes.db`），**不需要任何外部依赖**，
跑完直接看三个文件里各有哪些表、各有几行，物理上证明分库成立。

## 跑

```bash
dotnet run --project backend/sample/XiHan.BasicApp.DataSourceSample
```

全部断言通过时退出码为 0，任一项不符为 1，可直接接进脚本。每次运行都会先删掉三个 `.db` 重新建，可反复执行。

## 验的四件事

| # | 验证点 | 判据 |
| --- | --- | --- |
| 一 | 实体声明的数据源能被解析 | 未标注返回 null；框架 `[DataSource]` 与 SqlSugar 原生 `[Tenant]` 都能识别 |
| 二 | 仓储实际拿到的连接 | `GetClientForEntity` 返回的客户端 `ConfigId` 与声明一致 |
| 三 | 建表归属 | 每个库只收自己的实体，未声明数据源的实体不进模块专属库 |
| 四 | 物理隔离 | 另开连接直读三个 `.db` 文件，各自只有自己的表且各一行 |
| 五 | fail-closed | 声明的 `ConfigId` 没有对应连接时抛异常，不回落默认库 |

## 三个实体

| 实体 | 声明 | 落库 |
| --- | --- | --- |
| `SampleUser` | 无 | `default.db`（跟随租户上下文解析） |
| `ErpOrder` | `[DataSource("Erp")]` | `erp.db` |
| `MesTask` | `[Tenant("Mes")]`（SqlSugar 原生） | `mes.db` |

另有一个 `OrphanEntity` 声明了未配置的 `NotConfigured`，专门用来触发 fail-closed。

## 两点工程约定

**直接引框架源码，不走 `props/framework.props`。** 该文件按解决方案名决定引源码还是 NuGet 包，
而本示例验证的 `[DataSource]` 尚未随 NuGet 包发布，引包必然编译不过。所以 csproj 里是写死的
`ProjectReference`，并在框架源码缺失时给出明确报错。

**刻意不登记进 `XiHan.BasicApp.slnx`。** 那份解决方案恒走 NuGet（这是它的设计意图——要能被单独
clone 的人打开），把本示例放进去会让整个解决方案编译失败。需要与 `XiHan.BasicApp` 并列检出
`XiHan.Framework` 才能构建本示例。
