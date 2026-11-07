# 访问策略与权限条件

本页说明访问策略、登录失败自动封禁与权限条件的规则、操作与命中记录，适用于后端开发者与安全配置人员。

两套 ABAC 能力都在 Access 包，共用框架的条件引擎（`Security.Abstractions.Conditions`：十五个运算符，IP 集合可写 CIDR，时段可以跨零点）与同一份属性目录（`AccessConditionAttributes`）：

| | 访问策略 | 权限条件 |
| --- | --- | --- |
| 作用 | 判定一次调用能否进入，与持有的权限无关 | 判定持有的某个权限码在什么条件下生效 |
| 挂在哪里 | 平台层 / 租户层的独立策略 | 某个授权绑定（角色或用户在某个权限码上的授予或拒绝） |
| 管道位置 | `AccessPolicyMiddleware`，Tenant 阶段（认证与租户都已知） | `PermissionConditionMiddleware`，Authorize 阶段，框架授权之后 |
| 拒绝时 | `ACCESS_DENIED_004`（403）；登录时 `ACCESS_DENIED_005` | `ACCESS_DENIED_003`（403），或按模式只记录 |
| 开关 | `BasicApp:Access:AccessPolicyEnabled`（缺省 `true`） | `BasicApp:Access:PermissionConditionMode`（缺省 `Enforce`） |

配置见[配置参考](../configuration#basicapp-access)。

## 条件属性

属性名不区分大小写。

| 前缀 | 属性 |
| --- | --- |
| `subject.` | `userId`、`userName`、`tenantCode`（平台为 `platform`）、`roleCodes`、`departmentCodes`、`mainDepartmentCode`、`positionCodes`、`isImpersonating` |
| `resource.` | `operation`（操作 ID）、`permissionCodes`（本次调用要求的权限码）、`path`、`method` |
| `environment.` | `ip`、`geo.country` / `geo.province` / `geo.city` / `geo.isp`（IP 归属地）、`client.userAgent`、`client.kind`（未登录为 `anonymous`，否则为调用协议）、`date`、`time`、`hour`、`weekday`（周一为 1） |

- 部门与岗位类属性只在租户侧有值。
- 日期、时刻、小时与星期按 `BasicApp:Access:ConditionTimeZone`（缺省 `Asia/Shanghai`）判定。
- `policies.attributes` 与 `permission-conditions.attributes` 返回同一份目录，带每个属性可用的运算符与认证前能否取得。

::: warning IP 类条件与反向代理
放在反向代理后时，要把代理的地址或网段写进 `BasicApp:ForwardedHeaders`（见[配置参考](../configuration#basicapp-forwardedheaders)），`environment.ip` 与归属地才是客户端的。
:::

## 访问策略

### 求值规则

- 平台层作用于全部外部调用（含租户的调用）；租户层只作用于已登录的本租户调用。平台层拦下即不看租户层。
- 只管外部协议（HTTP、gRPC、MCP）。作业、消息处理、工作流与命令行不求值，也不记命中；没有来源的内部调用（如 AI 工具）在已经求值过的外部调用之内。
- 层内逐条求值此刻有效的策略：有放行命中即不拒绝，否则第一条命中的拒绝策略拦下。求值顺序按优先级（越小越先）、再按编码。
- 动作：`Deny` 拒绝、`Allow` 放行、`LogOnly` 只记录；状态 `Active` / `Disabled`；可设生效与失效时间。
- 判定本身出错时放行并记警告。
- 登录时机：各种登录方式开会话之前、`auth.switch-tenant` 之前，由登录守卫按要进入的一侧与账号此刻的角色再求一次平台层与目标租户层，拦下即不签发令牌（`ACCESS_DENIED_005`）。
- 命中记录里的时机：平台策略只引用认证前就取得到的属性（地址、归属地、时间、User-Agent、路径与方法）记为「认证前」，否则「认证后」；登录守卫的命中记为「登录时」。
- 启用的策略按侧缓存 60 秒，写入在提交之后失效。

策略把自己锁在外面时，由运维把 `BasicApp:Access:AccessPolicyEnabled` 设为 `false` 后重启。

### 写入规则

- 编码在本侧唯一、不含空白；条件 1 到 100 条、至多 20 个组：组内 AND、组间 OR。
- 新建（含从模板新建）一律是只记录、启用；改动作与启停只经 `policies.set-action` / `policies.set-status`。
- 防自锁：改动之后仍是启用中、此刻有效的拒绝策略，且按发起这次修改的请求求值会命中时，拒绝保存（`ACCESS_VALIDATION_041`）。

### 操作

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `policies.list` / `get` / `templates` / `attributes` | `access.policies.read` | 列表缺省按求值顺序 |
| `policies.create` / `update` | `access.policies.create` / `update` | |
| `policies.set-action` / `set-status` | `access.policies.update` | 单条 |
| `policies.delete` | `access.policies.delete` | 批量，逐项结果 |
| `policies.trial` | `access.policies.trial` | 已有策略或草稿条件按模拟的请求属性只求这一条、不看有效期，逐组逐条件说明 |

权限码两侧都有，写操作进操作日志。页面 `access.policies`（`/access/policies`）。

### 六个模板

| 模板 | 用途 | 种子写入时 |
| --- | --- | --- |
| `preset.ip-blacklist` | IP 黑名单（初始值是文档保留网段，要替换） | 拒绝、停用 |
| `preset.admin-office-network` | 角色 `admin` 只能从办公网络调用 | 拒绝、停用 |
| `preset.sensitive-ops-network` | 导出、删除、模仿、授权等敏感操作只能从办公网络调用 | 拒绝、停用 |
| `preset.off-hours` | 周末与工作日 08:00–20:00 之外的访问 | 只记录、启用 |
| `preset.overseas` | 归属地已知且不是中国的访问 | 只记录、启用 |
| `preset.block-scripts` | 没有 User-Agent 或是常见脚本与爬虫的（`curl`、`python-requests`、`Go-http-client`、`Wget`、`Scrapy`、`HttpClient`），开放平台的签名调用除外 | 只记录、启用 |

办公网段的占位值是 `203.0.113.0/24`。种子 `access.policies` 按模板在平台层补写还没有的编码，已有的不动。启用拒绝类之前替换网段，并先试运行。演示数据打开时，种子 `access.demo-policies` 在演示租户 `demo` 写两条只记录的策略（`demo.off-hours`、`demo.overseas`）。

### 登录失败自动封禁

系统参数 `access.security.login-auto-ban`（JSON；没建即关闭；写错即 `ACCESS_VALIDATION_043`，不计数）：

| 字段 | 缺省 | 范围 |
| --- | --- | --- |
| `isEnabled` | `false` | |
| `windowMinutes` | `10` | 1–1440，固定计数窗口 |
| `maxFailures` | `20` | 1–10000，同一地址跨账号的失败上限 |
| `banMinutes` | `30` | 1–43200，首次封禁时长 |
| `maxBanMinutes` | `1440` | 不小于 `banMinutes`，至多 43200 |

- 认证失败（密码、两步验证、验证码登录）按调用来源地址计数，达到上限即封禁；24 小时内再犯，时长翻倍到上限。这次调用命中了平台层的放行策略时不计。
- 封禁中的地址在访问策略之前即被拒绝：503 `ACCESS_UNAVAILABLE_001`，带重试秒数，并记一条编码 `login-auto-ban` 的命中。`AccessPolicyEnabled` 为 `false` 时封禁照常生效。
- 计数与封禁在共享缓存里，只当减速手段，不当安全边界。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `login-bans.list` | `access.login-bans.read` | 此刻生效的参数与当前封禁 |
| `login-bans.lift` | `access.login-bans.lift` | 提前解封并清掉该地址的计数；没封着即 `ACCESS_NOTFOUND_009` |

两个权限码只在平台一侧。

## 权限条件

### 结构与语义

条件存在 `SysPermissionCondition`，挂在一个授权绑定（侧 × 角色或用户 × 权限码 × 授予或拒绝）上：

- 挂在授予上是「满足才授予」，挂在拒绝上是「满足才拒绝」。
- 同组的子句 AND、组间 OR；每个绑定至多 5 组、每组至多 10 条，条件值至多 2000 个字符；停用的子句不参与判定。
- 绑定没了（撤销、授予与拒绝互换、删除角色）即连同删除。

框架的权限过滤器拿不到请求属性，所以分两步：

1. 授予判定是乐观的：有条件的授予算持有，有条件的拒绝不拿走；
2. `PermissionConditionMiddleware` 在框架授权之后，按调用来源（地址、User-Agent、路径与方法）、主体与时刻，只对操作声明的那个权限码精确判定。

授予存储读不到时这次调用以 503 结束，不当作没有条件放行。

### 模式

`BasicApp:Access:PermissionConditionMode`：

| 值 | 不满足时 |
| --- | --- |
| `Enforce`（缺省） | 拒绝（`ACCESS_DENIED_003`），记命中 |
| `LogOnly` | 放行，记命中 |
| `Disabled` | 不判定：有条件的授予即授予、有条件的拒绝即拒绝，不记命中 |

### 操作

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `permission-conditions.list` / `attributes` | `access.permission-conditions.read` | 列表按主体（`subjectKind`、`subjectId`）取，含停用的子句 |
| `permission-conditions.create` / `update` / `delete` | `access.permission-conditions.create` / `update` / `delete` | 删除为批量；写入记权限变更日志（`ConditionChange`） |
| `permission-conditions.trial` | `access.permission-conditions.trial` | 按模拟的时间、地址等试算某人在某权限码上的结论，逐组逐子句说明 |
| `permission-conditions.batch-trial` | `access.permission-conditions.trial` | 影响试算：一个绑定影响到的人（角色含下级角色的成员，至多 200 人）各自的结论 |
| `permission-conditions.inventory` | `access.permission-conditions.trial` | 盘点：本侧每条子句的诊断（属性未登记、旧写法、编译不过、绑定已不在） |

权限码两侧都有。条件在角色页与用户页的授权抽屉里维护，试算与盘点在权限目录页。

## 条件命中记录

两套能力的命中都写进 Auditing 的条件命中日志（`SysConditionHit`，按月分表），同一规则对同一来源一分钟内的命中合并成一行（`hitCount`）：

- 访问策略记全部命中（拒绝、放行、只记录），只有与拦截策略同层的拒绝标为拦截；平台层的命中落在平台一侧。
- 权限条件只记不满足的那次，`Enforce` 记为拒绝并拦截，`LogOnly` 记为只记录。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `condition-hits.list` / `get` | `auditing.condition-hits.read` | 只看当前一侧：来源、策略或权限码、动作、时机、是否拦截、操作、路径、地址与 User-Agent |

页面 `auditing.condition-hits`（`/auditing/condition-hits`）。

## 下一步

- [权限管理](./permission) · [约束规则与审批](./approval) · [日志审计](./logging)
