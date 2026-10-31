# 多租户 SaaS

本页说明 6.0 多租户的标识、租户目录、开通流程、成员、套餐与配额，适用于后端开发者与平台运维人员。

多租户在 6.0 里是三件事的组合：框架解析租户上下文并严格过滤租户库实体；Tenancy 包管租户的业务（开通、套餐、成员、配额、所有权）；Runtime 把租户连接与分片接到 SqlSugar 上。

## 标识与上下文

| 项 | 6.0 |
| --- | --- |
| 租户 ID | 字符串，文法 `^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$`（即 5.x 的租户编码），创建后不可改；`platform` 是保留 ID |
| 平台 | 不是一个租户：平台主体的令牌没有 `tenant` 声明；平台数据不实现 `IMultiTenant` |
| 解析链 | 令牌 `tenant` 声明 → Baggage `xihan.tenant`；不支持查询串与 `X-Tenant-Id` 头 |
| 隔离方式 | `Field`（缺省，共库、按 `tenant_id` 过滤）与 `Database`（独立库）；5.x 的 Schema 隔离已删除 |

框架仓储对实现 `IMultiTenant` 的实体做严格相等过滤，没有租户上下文时看不到任何行；`XiHan:MultiTenancy:RequireTenant` 为 `false`，平台主体可以调用平台侧操作。

## 租户目录与业务实体

租户目录使用框架 `MultiTenancy.Database` 的 `TenantRecord`，记录 ID、显示名、是否启用、`ConnectionString`、`DatabaseProvider` 等属性，目录为单例并带有界缓存。`SysTenant` 是 Tenancy 的业务实体，记录套餐、到期时间、配额、管理员（所有者）、隔离方式与独立库状态。开通、启停、改名与修改连接时，两者在同一个工作单元中同步写入。

租户停用（`tenants.suspend`）后，目录将其标记为禁用，带该租户声明的调用被框架拒绝（`MULTITENANCY_DENIED_001`），登录同样被拒绝。

租户有到期时间（`expiresAt`，为空即不限），它随目录属性同步：

- 到期时间一过，登录（各种方式）、刷新令牌、切换租户与模仿登录即被拒绝（`IDENTITY_DENIED_019`），不等作业。
- 内建作业 `tenancy.tenants.expire` 每 5 分钟把过了到期时间、仍生效的租户标为到期（`Expired`）并停用目录，之后带该租户声明的调用也被拒绝。不吊销会话、不发通知。
- 续期：先改到期时间（`tenants.update`），再恢复（`tenants.resume`）；没延长就恢复，逐项失败 `TENANCY_CONFLICT_015`。

## 开通三步

```text
tenants.create                        写 SysTenant + 登记租户目录（启用），绑定套餐（不填即缺省套餐）
  └─（独立库时）tenants.configure-database → tenants.initialize-database
tenants.initialize-admin              建租户管理员（首次登录必须改密），成为所有者，并初始化租户内置角色
```

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `tenants.create` / `update` / `get` / `list` | `tenancy.tenants.create` / `update` / `read` | 平台侧 |
| `tenants.delete` | `tenancy.tenants.delete` | 还有用户的租户不能删（`TENANCY_CONFLICT_003`） |
| `tenants.suspend` / `resume` | `tenancy.tenants.suspend` | 批量启停；停用与到期的都能恢复，要没过到期时间（`TENANCY_CONFLICT_015`） |
| `tenants.change-edition` | `tenancy.tenants.update` | 换套餐，进权限变更日志 |
| `tenants.configure-database` | `tenancy.tenants.configure-database` | 设隔离方式与独立库连接（`ISecretProtector` 加密）；就绪后不能再改 |
| `tenants.initialize-database` | `tenancy.tenants.initialize-database` | 建库、只建租户库实体的表、登记结构版本；幂等，失败记原因可重试 |
| `tenants.initialize-admin` | `tenancy.tenants.initialize-admin` | 初始化管理员 |
| `tenants.transfer-owner` | `tenancy.tenants.transfer-owner` | 所有权转移给注册地用户或生效中的普通成员 |
| `tenants.quotas` | `tenancy.tenants.read` | 各租户的用量与配额（平台侧） |
| `tenants.mine` | 登录即可 | 我能进入的租户（控制中心、个人中心「我的租户」） |

独立库支持的库类型：`postgresql`、`sqlite`、`mysql`、`sqlserver`（表结构脚本只为 PostgreSQL 编写，其他库由建表按实体完成）。跨库写入是顺序提交，不是两阶段提交，见[数据库配置](./database)。

## 成员与切换

| 成员类型 | 说明 |
| --- | --- |
| 注册地用户 | `SysUser.HomeTenantId` 为该租户，可直接进入 |
| 普通成员 | 别处注册的账号加入该租户，带生效 / 到期 |
| 运维支持（`Support`） | 只能是平台账号，平台运维进入租户排障用 |

`tenant-members.list / create / delete`（`tenancy.tenant-members.*`，平台侧）。登录后用 `auth.switch-tenant` 在注册地、生效中的成员租户与平台之间切换：会话续用、令牌重签、前端整页重建。

## 套餐与配额

`SysEdition` / `SysEditionGrant`：

| 字段 | 说明 |
| --- | --- |
| 白名单 | 租户能生效的权限码（作用侧 Tenant / Both），至多 500 个；与有效权限求交（套餐门控） |
| 席位上限 / 存储上限（MB） | 为空不限；租户自己可以覆盖 |
| 价格 / 计费周期 / 是否免费 | 展示与订阅用，免费套餐不能标价 |
| 缺省套餐 | 新租户不指定时绑定它 |

种子 `tenancy.editions` 写四个套餐：

| 套餐 | 白名单 | 席位 | 存储 | 价格 |
| --- | --- | --- | --- | --- |
| Free（缺省） | 成员、组织、角色的只读 | 5 | 1 GB | 免费 |
| Basic | Free + 用户、组织、参数、文件的日常管理 | 20 | 10 GB | 99 / 月 |
| Professional | Basic + 角色管理、授权与指派 | 100 | 100 GB | 299 / 月 |
| Enterprise | 创建时全部租户侧权限 | 不限 | 不限 | 按年 |

配额经 `Contracts` 的 `ITenantQuota` 约束租户一侧的建用户、上传与秒传；头像不计入租户存储。`subscription.get`（`tenancy.subscription.read`）给租户看自己的套餐、期限与用量（「我的订阅」页）。

## 平台扫租户库

日志写入与清理、独立库的初始化与升级这类后台工作，在自建作用域里经框架 `TenantShardScope.BindTenantAsync` 按租户目录绑定分片，再开工作单元；不在同一个上下文里切换租户。

## 演示租户

`BasicApp:Seed:EnableDemoData` 打开时，种子 `tenancy.demo` 写演示租户 `demo` 与其管理员 `admin`（首次登录须改密，初始密码见 `DemoTenantSeeder`）。

## 下一步

- [组织架构](./organization) · [权限管理](./permission) · [数据模型](./data-model)
