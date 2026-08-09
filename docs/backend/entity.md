# 实体基类

新增实体前先读这页：基类怎么选、有哪些列是自动来的、多租户列是什么语义、软删与唯一索引怎么配合、命名和索引有哪些强制规范。选错基类的返工成本很高。

全部数据表的清单见 [数据模型](./data-model)。

## 先选对基类

所有 BasicApp 实体都继承 `XiHan.BasicApp.Core.Entities` 下的基类，它们各自对应框架的 `SugarMultiTenant*` 系列——**都自带 `TenantId` 列**。

| 基类 | 主键 | 审计列 | 软删 | 用在哪 |
| --- | --- | --- | --- | --- |
| `BasicAppEntity` | 雪花 `long` | 无 | 否 | 极简实体、纯关联表 |
| `BasicAppEntityWithIdentity` | 数据库自增 `long` | 无 | 否 | 需要自增序号的场景 |
| `BasicAppCreationEntity` | 雪花 `long` | 创建三列 | 否 | **日志 / 流水**等只增不改的表 |
| `BasicAppModificationEntity` | 雪花 `long` | 仅修改三列 | 否 | 会改但不删的配置类 |
| `BasicAppDeletionEntity` | 雪花 `long` | 仅删除四列 | 是 | 少见 |
| **`BasicAppFullAuditedEntity`** | 雪花 `long` | 创建 + 修改 + 删除 | **是** | **绝大多数业务实体的默认选择** |
| `BasicAppAggregateRoot` | 雪花 `long` | 全套 | 是 | 聚合根（带领域事件能力） |

选择准则：

- **拿不准就用 `BasicAppFullAuditedEntity`**——业务表几乎都需要「谁建的、谁改的、软删可恢复」。
- **日志、流水、审计类用 `BasicAppCreationEntity`**：只增不改不删，带上修改/删除列纯属浪费，而且这类表通常还要**按月分表**。
- 纯多对多关联表（如 `SysUserRole`）通常也是创建型（硬删）。

## 自动来的列

### 主键与并发

| 列 | 类型 | 说明 |
| --- | --- | --- |
| `Basic_Id` | `bigint` | **主键**。注意列名是 `Basic_Id` 不是 `Id`——手写 SQL / 排查时别找错。默认**非自增**，由应用侧雪花算法生成 |
| `Row_Version` | `bigint` | 乐观并发标识，SqlSugar 的 `IsEnableUpdateVersionValidation` 生效 |

雪花参数在配置节 `XiHan:DistributedIds:SnowflakeId`。

::: danger 多节点必须逐节点改 `WorkerId`
两个节点同 `WorkerId` 会生成**重复主键**，且不会立刻报错——等到唯一约束冲突时数据已经乱了。`BaseTime` 与位长一经上线不可更改。
:::

::: warning 主键在 API 里是字符串
`long` 经框架的 `LongJsonConverter` 统一序列化成 JSON **字符串**（避免 JavaScript Number 精度溢出）。前端契约里主键类型是 `string`，DTO 字段名是 `basicId`。
:::

### 审计三件套

| 阶段 | 列 | 备注 |
| --- | --- | --- |
| 创建 | `Created_Time` / `Created_Id` / `Created_By` | `Created_Time` 非空；三列都是 `IsOnlyIgnoreUpdate`（更新时不覆盖） |
| 修改 | `Modified_Time` / `Modified_Id` / `Modified_By` | 可空 |
| 删除 | `Is_Deleted` / `Deleted_Time` / `Deleted_Id` / `Deleted_By` | `Is_Deleted` 非空、默认 `false` |

时间列都是 `DateTimeOffset`，**存储恒为 UTC**，输出时按请求头 `X-Timezone` 换算。

::: tip 这些列由 AOP 自动注入
走的是 SqlSugar 的 `DataExecuting` AOP，**业务代码不要手动赋值**——修改三列会被 AOP 无条件覆盖；创建列只在值为默认值时才填充，手动赋值会留下与实际操作人不符的审计信息。
:::

::: warning 别对时间列做单列标量投影
`DateTimeOffset` 列不要用 `Select(x => x.CreatedTime)` 这种单列标量投影——值类型的 `ChangeType` 路径会在 `DateTime` → DTO 转换时崩。要整行取实体、走属性绑定。
:::

### 多租户列

`Tenant_Id`（`bigint`，`IsOnlyIgnoreUpdate`）每个实体都有，语义**不是可空的**：

- **平台级/全局记录统一 `TenantId = 0`**（「平台租户」占位），**不得使用 NULL**。
- 业务租户 Id 从 1 开始，0 号由平台保留。
- 判定全局记录一律用 `TenantId == 0`；需要 `IsGlobal` 语义时在实体 `Expand` 里做**派生只读属性** `IsGlobal => TenantId == 0`，**不落库**（避免与 `TenantId` 漂移）。
- 合并查询全局 + 私有：`WHERE TenantId IN (0, {currentTenantId})`。
- `TenantId` 由租户上下文自动注入，**禁止业务代码直接操纵**。

::: danger 读写口径不对称
**读共享、写不共享**——全局过滤器放行 `TenantId=0` 的行让租户能读到平台数据，但租户上下文里**禁止改写/删除**非本租户行（含全局行）。维护全局数据的唯一入口是平台态（`ICurrentTenant.Change(null)`）。

详见 [多租户 SaaS](./multi-tenancy)。
:::

## 软删与唯一索引

支持软删的实体，其**唯一索引（`UX_*`）末列统一附加 `IsDeleted`**，使唯一性只约束未删除行——软删后可以再建同编码记录。

代价要知道：**同一编码至多保留一条软删行**。要第二次软删同编码记录，服务层得先物理清理旧的软删行。

纯创建型（`Creation`）的关联表与日志表是**硬删**、无 `IsDeleted`，唯一索引保持原样。

## 命名与索引规范

| 对象 | 规范 | 示例 |
| --- | --- | --- |
| 表名 | `snake_case`，`Sys_{名}` | `Sys_User_Department` |
| 列名 | `snake_case` | `Created_Time`、`Tenant_Id` |
| `[SugarTable]` 命名参数 | **无空格** | `SugarTable(TableName = "...", TableDescription = "...")` |
| `[SugarColumn]` 命名参数 | **有空格** | `SugarColumn(ColumnName = "...", ColumnDescription = "...")` |
| 时间字段 | 一律 `XxxTime`；过期统一 `ExpirationTime` | `CreatedTime` / `ExpirationTime` |
| 排序字段 | `Sort` | — |
| 缩写 | `OAuth` / `CSharp` 等整体保留 | `SysOAuthApp` |

**审计三件套索引**（强制）：

- 所有具体实体：`IX_{table}_TeId_CrTi`、`IX_{table}_CrId`
- `FullAudited` / `AggregateRoot` 额外：`IX_{table}_TeId_IsDe`
- 日志类分表实体把 `{table}` 换成 `{split_table}`

## 按月分表

日志与流水类实体用 SqlSugar 的分表能力：

```csharp
[SugarTable(TableName = "Sys_Access_Log_{year}{month}{day}", TableDescription = "系统访问日志表"),
 SplitTable(SplitType.Month)]
```

物理表名形如 `Sys_Access_Log_20260801`。查询分表数据要走 SqlSugar 的分表 API（按时间范围定位物理表），**不能当普通表直接查**。

哪些表分了表见 [数据模型](./data-model#日志-均按月分表)。

## 实体分文件约定

一个实体通常拆成三个文件（`partial`）：

| 文件 | 内容 |
| --- | --- |
| `Domain/Entities/SysXxx.cs` | 主体：`[SugarTable]` + `[SugarIndex]` + 落库字段 |
| `Domain/Entities/Expands/SysXxx.Expand.cs` | **不落库**的派生属性（`[SugarColumn(IsIgnore = true)]`），如 `IsGlobal`、显示名 |
| `Domain/Entities/Aggregates/SysXxx.Aggregate.cs` | 聚合行为（少数实体才有，如 `SysTenant` / `SysConstraintRule`） |

枚举放 `Domain/Entities/Enums/SysXxx.Enum.cs`。

::: tip 生成的代码不焊外键
代码生成器不产出 `Navigate` / `LEFT JOIN` / 显示属性 / 物理外键，跨表关联一律由业务层手写。见 [代码生成](./code-generation)。
:::

## 加一个实体的清单

| 步骤 | 做什么 |
| --- | --- |
| 1 | 选基类（默认 `BasicAppFullAuditedEntity`） |
| 2 | 写 `[SugarTable]` + 字段 + `[SugarIndex]`（含审计三件套索引） |
| 3 | 派生属性放 `Expands/`，枚举放 `Enums/` |
| 4 | 加仓储接口与实现（继承 `SaasRepository`，自动注册） |
| 5 | 全新库由 CodeFirst 建表；存量库在 `WebHost/UpdateScripts/{version}.sql` 补建表/列/索引脚本 |

## 相关页面

- [数据库配置](./database)：连接、初始化开关、主从、分表查询
- [数据模型](./data-model)：全部数据表清单
- [开发流程](./development)：加一个完整功能纵切片
- [框架 · Data 包](https://framework.docs.xihanfun.com/packages/data)：仓储、查询过滤器、AOP 的框架实现
