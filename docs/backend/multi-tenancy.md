# 多租户 SaaS

本页说明租户的标识与上下文、租户目录、开通流程、成员、套餐与配额，适用于后端开发者与平台运维人员。

多租户是三件事的组合：框架解析租户上下文并严格过滤租户库实体；Tenancy 包管租户的业务（开通、套餐、成员、配额、所有权）；Runtime 把租户连接与分片接到 SqlSugar 上。

## 标识与上下文

| 项 | 说明 |
| --- | --- |
| 租户 ID | 字符串，文法 `^[a-z0-9][a-z0-9._-]{0,63}$`，创建后不可改；`platform` 是保留 ID |
| 平台 | 不是一个租户：平台主体的令牌没有 `tenant` 声明；平台库实体不实现 `IMultiTenant`，要区分「平台的还是某租户的」用 `OwnerTenantId`（平台为 `platform`） |
| 解析链 | 令牌 `tenant` 声明 → Baggage `xihan.tenant`（只来自内部调用：作业、消息处理、工作流）；不支持查询串与 `X-Tenant-Id` 头。HTTP、gRPC、MCP 入站丢掉调用方写的 `xihan.tenant`、`xihan.principal`：平台令牌带 `baggage` 头也还在平台一侧，进租户走 `auth.switch-tenant`。CloudEvents 入站的 `xihantenant` 能选租户，这是协议的设计，所以 `/events` 缺省关闭，打开后要 `runtime.cloud-events.deliver`，这个权限只在平台一侧，只授给平台的服务账号 |
| 隔离方式 | `Field`（缺省，共库、按 `tenant_id` 过滤）与 `Database`（独立库） |

框架仓储对实现 `IMultiTenant` 的实体做严格相等过滤，没有租户上下文时看不到任何行；`XiHan:MultiTenancy:RequireTenant` 为 `false`，平台主体可以调用平台侧操作。

## 租户目录与业务实体

租户目录是框架 `MultiTenancy.Database` 的 `tenant_record`：租户 ID、显示名、是否启用、`ConnectionString`、`DatabaseProvider` 等属性，带有界缓存（`XiHan:MultiTenancy:Database`）。`SysTenant` 是 Tenancy 的业务实体：套餐、到期时间、席位与存储上限、管理员（所有者）、隔离方式与独立库状态。开通、启停、改名与修改连接时，两者在同一个工作单元中同步写入；目录里的连接串是密文，由 Runtime 的分片解析器解密。

租户停用（`tenants.suspend`）后，目录将其标记为禁用，带该租户声明的调用被框架拒绝（`MULTITENANCY_DENIED_001`），登录同样被拒绝。

租户有到期时间（`expiresAt`，为空即不限），它随目录属性同步：

- 到期时间一过，登录（各种方式）、刷新令牌、切换租户与模仿登录即被拒绝（`IDENTITY_DENIED_019`），不等作业。
- 内建作业 `tenancy.tenants.expire` 每 5 分钟把过了到期时间、仍生效的租户标为到期（`Expired`）并停用目录，之后带该租户声明的调用也被拒绝。不吊销会话、不发通知。
- 续期：先改到期时间（`tenants.update`），再恢复（`tenants.resume`）；没延长就恢复，逐项失败 `TENANCY_CONFLICT_015`。

## 开通三步

```text
tenants.create                        写 SysTenant + 登记租户目录（启用），绑定套餐（不填即缺省套餐）
  └─（独立库时）tenants.configure-database → tenants.initialize-database
tenants.initialize-admin              建租户管理员（系统参数 identity.password 打开时首次登录必须改密），成为所有者，并建租户内置角色 admin
```

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `tenants.create` / `update` / `get` / `list` | `tenancy.tenants.create` / `update` / `read` | 平台侧；套餐不存在 `TENANCY_NOTFOUND_004`，已停用 `TENANCY_CONFLICT_016` |
| `tenants.delete` | `tenancy.tenants.delete` | 还有用户的租户不能删（`TENANCY_CONFLICT_003`） |
| `tenants.suspend` / `resume` | `tenancy.tenants.suspend` | 批量启停；停用与到期的都能恢复，要没过到期时间（`TENANCY_CONFLICT_015`） |
| `tenants.change-edition` | `tenancy.tenants.update` | 换套餐，进权限变更日志 |
| `tenants.configure-database` | `tenancy.tenants.configure-database` | 设隔离方式与独立库连接（`ISecretProtector` 加密）；独立库就绪后不能再改（`TENANCY_CONFLICT_013`） |
| `tenants.initialize-database` | `tenancy.tenants.initialize-database` | 建库、只建租户库实体的表、在该库登记结构版本；幂等，失败记原因，改连接后可重试 |
| `tenants.initialize-admin` | `tenancy.tenants.initialize-admin` | 每个租户只能一次；独立库要先初始化数据库（`TENANCY_CONFLICT_014`） |
| `tenants.transfer-owner` | `tenancy.tenants.transfer-owner` | 所有权转移给注册地用户或生效中的普通成员；新所有者进 `admin` 角色，原所有者保留已有角色 |
| `tenants.quotas` | `tenancy.tenants.read` | 各租户的用量与配额（平台侧） |
| `tenants.mine` | 登录即可 | 我能进入的租户（控制中心、个人中心「我的租户」） |

独立库支持的库类型：`postgresql`、`sqlite`、`mysql`、`sqlserver`（表结构脚本只为 PostgreSQL 编写，见[表结构升级](./upgrade)）。跨库写入是顺序提交，不是两阶段提交，见[数据库配置](./database)。

## 成员与切换

| 成员类型 | 说明 |
| --- | --- |
| 注册地用户 | `SysUser.HomeTenantId` 为该租户，可直接进入 |
| 普通成员（`Member`） | 别处注册的账号加入该租户，带生效 / 到期 |
| 运维支持（`Support`） | 只能是平台账号，平台运维进入租户排障用 |

`tenant-members.list / create / delete`（`tenancy.tenant-members.read / create / delete`，平台侧）。登录后用 `auth.switch-tenant` 在注册地、生效中的成员租户与平台之间切换：会话续用、令牌重签、前端整页重建。

### 外部成员

生效中的普通成员在这个租户里叫外部成员，算租户内的用户（运维支持成员不算）：

- `users.list`、`users.get`、`users.detail` 带出他们，响应标 `isExternalMember`；不是本租户成员的账号查不到，也搜不到。
- 能配本租户的角色（`roles.assign`）、部门（`memberships.create`、部门负责人）、数据范围（`user-data-scopes.set`）与直授（`user-grants.*`、权限条件），也能作为委托人、字段安全目标与约束规则目标。切进租户后这些权限才算数，回到注册地不带过去。
- 账号本身只有注册地（或平台）能动：`users.update`、`users.reset-password`、`users.lock`、`users.disable`、`users.delete`、`users.reset-two-factor`、`users.set-login-policy` 在成员租户里一律按不存在回答。`users.detail` 不给账号安全一节，今日统计与当前会话只算这个租户里的。
- 被移出租户时（`tenant-members.delete`）解除他在这个租户的角色，再加回来不带旧角色；部门归属与直授留着，人不在这一侧就用不上。成员关系到期不解除，只是看不到、用不上。

### 运维支持成员

生效中的运维支持成员不算租户内的用户，只进用户管理：

- `users.list`、`users.get`、`users.detail` 带出他们，响应标 `isSupportMember`（注册在平台，`isExternalMember` 也为真）。别的选人入口用 `users.list` 的 `isSupportMember eq false` 筛掉他们。
- 账号只读，与外部成员相同，由平台维护。最近登录时间与 IP 是平台账号的全局信息，租户一侧不下发。
- 能配本租户的角色（`roles.assign`、`roles.update-member`），切进租户后按角色使用；权限条件与访问策略的试算认他们。到期或被移出之后切不进去，留在里面的会话续签被拒（`IDENTITY_DENIED_006`）；被移出时解除角色，再加回来不带旧角色。
- 不能配部门、数据范围与直授，也不能作为委托人、字段安全目标与约束规则目标。
- 不占席位，不进模仿候选、群发收件人、工作流受理人与聊天目录。

## 套餐与配额

`SysEdition` / `SysEditionGrant`：

| 字段 | 说明 |
| --- | --- |
| 白名单 | 租户能生效的权限码（作用侧 Tenant / Both），至多 500 个；与有效权限求交（套餐门控，见[权限管理](./permission#套餐门控)） |
| 席位上限 / 存储上限（MB） | 为空不限；租户自己的上限优先于套餐的 |
| 价格 / 计费周期 / 是否免费 | 展示与订阅用，免费套餐不能标价 |
| 缺省套餐 | 新租户不指定时绑定它；必须启用（`TENANCY_CONFLICT_017`），要停用先把别的启用套餐设为缺省 |
| 状态 | 启用 / 停用，启停并入 `editions.update`。停用后不能再分配给租户（开通或换套餐到它即 `TENANCY_CONFLICT_016`，下拉也不列）；已绑定的租户不受影响，白名单与配额照旧 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `editions.list` / `get` | `tenancy.editions.read` | 平台侧 |
| `editions.create` / `update` / `delete` | `tenancy.editions.create` / `update` / `delete` | 还有租户绑定的不能删（`TENANCY_CONFLICT_007`） |
| `editions.set-permissions` | `tenancy.editions.update` | 整体替换白名单，绑定的租户立即跟随，进权限变更日志 |

种子 `tenancy.editions` 写四个套餐，只首写（编码已存在即跳过）：

| 编码 / 名称 | 白名单 | 席位 | 存储 | 价格 |
| --- | --- | --- | --- | --- |
| `free` / 免费版（缺省） | 租户侧全部 `read` 权限 | 5 | 1 GB | 免费 |
| `basic` / 基础版 | 免费版 + Identity、Organization、Settings、Files 的管理权限（不含模仿登录） | 20 | 10 GB | 99 / 月 |
| `pro` / 专业版 | 基础版 + Access 的全部权限 | 100 | 100 GB | 299 / 月 |
| `enterprise` / 企业版 | 创建时的全部租户侧权限 | 不限 | 不限 | 按年 |

配额经 `Contracts` 的 `ITenantQuota` 约束租户一侧的建用户、上传与秒传（平台主体代租户办事不受限，已超额的只报告不阻止既有数据）；头像不计入租户存储。`subscription.get`（`tenancy.subscription.read`，租户侧）给租户看自己的套餐、期限与用量（「我的订阅」页）。

## 平台扫租户库

日志写入与清理、独立库的初始化与升级这类后台工作，在自建作用域里经框架 `TenantShardScope.BindTenantAsync` 按租户目录绑定分片，再开工作单元；不在同一个上下文里切换租户。要以某个租户的身份发起一次完整调用（种子、后台写入）用 `Contracts` 的 `ITenantScopeRunner`。

## 演示租户

`BasicApp:Seed:EnableDemoData` 打开时：

- 种子 `tenancy.demo` 写演示租户 `demo`（绑定企业版）、其管理员 `admin`、平台超级管理员在 `demo` 的成员身份（演示切换租户与模仿），以及流程演示要用的成员账号 `manager`、`lead`、`hr`（密码同管理员，初始密码见 `DemoTenantSeeder`；系统参数 `identity.password` 打开时首次登录都须改密）。
- 种子 `access.demo` 在 `demo` 里建角色 `employee`（只看本人数据，工作流的查看与发起、聊天的查看与发送，指派给三个成员）与 `hr_specialist`（自定义数据范围研发部与市场部，指派给 `hr`），可选模块的权限码只授出已登记的。
- 种子 `access.demo-policies` 在 `demo` 里写两条只记录的访问策略（`demo.off-hours`、`demo.overseas`，内容取同名模板），让命中记录页有数据。
- 种子 `settings.demo` 写两个平台字典（`demo_customer_level` 带缺省项与停用项，`demo_region` 是树形）；`notification.demo` 写三条草稿通知（平台的上手指南，`demo` 里一条按部门、一条按角色定向），发布之后才展开收件人。

组织与流程的演示数据见[组织架构](./organization#演示数据)与[工作流](./workflow#演示流程)。这几个种子都只首写：写过一次（含已删除的）就不再碰。

## 下一步

- [组织架构](./organization) · [权限管理](./permission) · [数据模型](./data-model)
