# 访问策略与权限条件

本页说明访问策略与权限条件两类 ABAC 能力的规则、操作与日志，适用于后端开发者与安全配置人员。

两套 ABAC 能力共用一个条件引擎（框架 `Security.Abstractions.Conditions`，十五个运算符、CIDR、跨零点时段、时区）与同一份属性目录（Access 的 `AccessConditionAttributes`）：

| | 访问策略 | 权限条件 |
| --- | --- | --- |
| 作用 | 判定一次调用能否进入，与持有的权限无关 | 判定持有的某个权限码在什么条件下生效 |
| 挂在哪里 | 平台层 / 租户层的独立策略 | 某个授权绑定（角色或用户的某个权限码）上 |
| 管道位置 | Tenant 阶段（认证与租户都已知之后） | Authorize 阶段，框架授权之后 |
| 失败 | `ACCESS_DENIED_004`（403） | 按模式拒绝或只记录 |

两者的每一次命中都记一条条件命中日志（`condition-hits.*`）。

## 条件属性

| 前缀 | 属性 |
| --- | --- |
| `subject.*` | `userid`、`username`、`tenantcode`、`rolecodes`、`departmentcodes`、`maindepartmentcode`、`positioncodes`、`isimpersonating` … |
| `environment.*` | `ip`、`geo.country` / `province` / `city` / `isp`（IP 归属地）、`client.kind`、`client.useragent`、`date`、`time`、`hour`、`weekday` |
| `resource.*` | `operation`、`permission`、`permissioncodes`、`path`、`method`、`service`、`action` |

条件的时段、日期按 `BasicApp:Access:ConditionTimeZone`（缺省 `Asia/Shanghai`）判定。`policies.attributes` 与 `permission-conditions.attributes` 返回属性目录供界面选择。

::: warning IP 类条件与反向代理
宿主目前没有启用转发头处理，放在反向代理后时 `environment.ip` 与归属地看到的是代理地址，见[部署](../deployment)。
:::

## 访问策略

### 求值规则

- 平台层作用于全部调用（含租户的调用），租户层只作用于已登录的本租户调用；平台层拦下即不看租户层。
- 层内：有放行命中即不拒绝，否则第一条命中的拒绝策略拦下。
- 时机按策略引用的属性推出：只引用认证前就取得到的属性（地址、归属地、客户端、时间）的是「认证前」，否则「认证后」；另有登录时机，在 `auth.login` 等开会话之前由登录守卫求值。5.x 分认证前、认证后两个中间件，6.0 认证在 Tenant 阶段之前，合成一个中间件。
- 动作：`Deny` 拒绝、`Allow` 放行、`LogOnly` 只记录；状态 `Active` / `Disabled`；按优先级排序。
- 判定本身出错时放行并记警告。

### 操作

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `policies.list` / `get` / `templates` / `attributes` | `access.policies.read` | |
| `policies.create` / `update` / `delete` | `access.policies.create` / `update` / `delete` | 可从模板新建（一律先只记录） |
| `policies.set-action` / `set-status` | `access.policies.update` | 改动作、启停 |
| `policies.trial` | `access.policies.trial` | 试运行：按模拟的请求属性看会命中哪些策略 |

防自锁：改完仍是启用中的拒绝策略、且会拦下当前这次请求时拒绝保存。

### 六个模板

| 模板 | 用途 |
| --- | --- |
| `preset.ip-blacklist` | IP 黑名单（示例网段需替换） |
| `preset.admin-office-network` | 管理员只能从办公网络调用 |
| `preset.sensitive-ops-network` | 导出、删除、模仿、授权等敏感操作只能从办公网络调用 |
| `preset.off-hours` | 非工作时间访问 |
| `preset.overseas` | 境外访问 |
| `preset.block-scripts` | 脚本与爬虫（`curl`、`python-requests` 等 User-Agent） |

种子 `access.policies` 按这六个模板在平台层写入六条策略：拒绝类停用、只记录类启用。启用拒绝类之前替换示例网段，并先试运行。

### 登录失败自动封禁

系统参数 `access.security.login-auto-ban`（只认平台的，JSON 值，缺省关闭）：窗口分钟数、同一地址失败上限、首次封禁时长等。达到上限后该地址的请求在访问策略之前即被拒绝（503 `ACCESS_UNAVAILABLE_001`，带重试秒数），并记一条编码 `login-auto-ban` 的命中。平台层有放行命中的调用，认证失败不计入封禁。

| 操作 | 权限码 |
| --- | --- |
| `login-bans.list` | `access.login-bans.read` |
| `login-bans.lift` | `access.login-bans.lift` |

## 权限条件

### 结构与语义

条件挂在授权绑定上（`SysPermissionCondition`）：组内 AND、组间 OR。框架的权限过滤器拿不到请求属性，所以分两步：

1. 过滤器乐观放行带条件的权限码；
2. `PermissionConditionMiddleware` 在框架授权之后，按调用来源（地址、User-Agent、路径与方法）、主体与时刻精确判定操作声明的那个权限码。

`resource.*` 属性在列表查询里由查询条件补算。

### 模式

`BasicApp:Access:PermissionConditionMode`：

| 值 | 不满足时 |
| --- | --- |
| `Enforce`（缺省） | 拒绝，记命中 |
| `LogOnly` | 放行，记命中 |
| `Disabled` | 不判定 |

### 操作

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `permission-conditions.list` / `attributes` | `access.permission-conditions.read` | |
| `permission-conditions.create` / `update` / `delete` | `access.permission-conditions.create` / `update` / `delete` | |
| `permission-conditions.trial` | `access.permission-conditions.trial` | 按模拟的时间、IP 等试算某人某权限的结论 |
| `permission-conditions.batch-trial` | `access.permission-conditions.trial` | 影响试算：一组主体同时试算 |
| `permission-conditions.inventory` | `access.permission-conditions.trial` | 盘点：找出未登记的属性、旧名字、不合法的条件与失效的绑定 |

## 条件命中日志

`condition-hits.list` / `get`（`auditing.condition-hits.read`）：哪条策略或条件、动作、是否拦截、请求属性快照。平台层的命中落在平台一侧。

## 下一步

- [权限管理](./permission) · [约束规则与审批](./approval) · [日志审计](./logging)
