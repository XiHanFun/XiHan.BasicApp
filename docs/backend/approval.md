# 约束规则与审批

本页说明约束规则、会话角色激活与授权申请，适用于后端开发者与权限配置人员。

::: info 本页范围
三者都在 Access 包。授权申请的审批走 Workflow（审批中心见[工作流](./workflow#待办与审批中心)）；Workflow 关掉时授权申请照常列出、不能提交。
:::

## 约束规则

### 五种类型

| 类型 | 判定时机 | 含义 |
| --- | --- | --- |
| `Ssd` 静态职责分离 | 指派角色、改指派有效期、委托角色、设上级 | 同一用户持有的角色（没过期的指派与角色委托，按继承展开）里，同一组不能超过上限个 |
| `Dsd` 动态职责分离 | 会话激活角色 | 可以同时持有，但同一会话激活的角色（按继承展开）里，同一组不能超过上限个 |
| `MutualExclusion` 权限互斥 | 给角色或用户直接授予权限码 | 同一角色或用户直接持有的权限码里，同一组不能超过上限个 |
| `Cardinality` 基数 | 指派角色、改指派有效期、委托角色 | 角色的成员数上限，或用户持有的角色数上限 |
| `Condition` 条件 | 指派或委托规则项里的角色 | 按条件引擎判定被指派的用户与发起这次变更的请求，不满足即违规 |

- 规则存在 `SysConstraintRule`，规则项（角色 ID、权限码或用户 ID，带组号）存在 `SysConstraintRuleItem`，每条规则至多 200 项；按侧（平台 / 租户）分开。
- 编码在本侧唯一、不含空白；条件类的条件写法同[访问策略](./access-policy#条件属性)。
- 规则带优先级（越大越先求值）与有效期；启用的规则按侧缓存 60 秒，写入在提交之后失效。

### 违规处理

| 方式 | 行为 |
| --- | --- |
| `Deny` | 拦下变更，`ACCESS_CONFLICT_012` |
| `RequireApproval` | 拦下变更，等同拒绝，不发起审批 |
| `Warning` | 照常执行，记一条权限变更日志（`ConstraintViolation`） |
| `Log` | 照常执行，同样记日志，只是级别不同 |

`ConstraintEnforcer` 只拦这次变更新带进来的违规，不因为已有数据已经违规而拒绝无关的变更。批量授予权限时逐个过权限互斥，被拦的那一项失败、其余照常。

### 操作

| 操作 | 权限码 |
| --- | --- |
| `constraint-rules.list` / `get` | `access.constraint-rules.read` |
| `constraint-rules.create` / `update` | `access.constraint-rules.create` / `update` |
| `constraint-rules.set-status` | `access.constraint-rules.update`（启用时重新校验规则项） |
| `constraint-rules.delete` | `access.constraint-rules.delete`（批量，连同规则项） |

权限码两侧都有，写操作进操作日志。页面 `access.constraint-rules`（`/access/constraint-rules`）。

## 会话角色激活

- `SysSessionRole` 记每个会话激活了哪些角色（会话 × 角色一行）。登录、刷新、切换租户与模仿时由 Contracts 的 `ISessionRoleActivator`（Access 实现）选定激活集：持有的角色按排序号、再按编码逐个尝试，加上它会违反拦截类 DSD 规则的不激活。
- 令牌的 `role` 声明只带激活的角色，权限判定与数据范围都按它。
- 开会话与续签时有角色被 DSD 停掉或已不再持有，另记一条登录日志（`RoleActivation`）。
- 停用与到期的行保留作查询，原因如 `dsd:<规则编码>`、`not-held`。

| 操作 | 权限 | 说明 |
| --- | --- | --- |
| `auth.session-roles` | 登录即可 | 此刻持有的角色与本会话激活的角色 |
| `auth.activate-roles` | 登录即可 | 换激活集：会话续用、刷新令牌轮换；要激活的须此刻持有（`ACCESS_VALIDATION_050`），违反 DSD 即 `ACCESS_CONFLICT_012`；记登录日志（`RoleActivation`）；模仿态的会话不能切换 |
| `session-roles.list` / `get` | `access.session-roles.read` | 管理员查看各会话的激活记录 |

前端的「切换角色」在用户菜单里。

## 授权申请

用户为自己申请一个角色或一个权限码（二选一：都给是 `ACCESS_VALIDATION_056`，都不给是 `ACCESS_VALIDATION_051`），提交即在同一个事务里发起审批业务 `permission-request` 的流程；审批通过即按期望的有效期指派角色或直授权限，驳回或中止即完结。

它是 Contracts 审批业务接入（`IWorkflowBusinessHandler`）的参照实现，见[工作流](./workflow#审批业务接入)。表 `sys_permission_request` 在租户库，平台一侧的申请租户为 `platform`。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `permission-requests.list` / `get` | `access.permission-requests.read` | 这一侧全部的申请，带审批进度。关键字匹配申请人（经用户目录在这一侧按登录名与显示名搜，含停用的账号，取前 200 个）、申请原因与备注；可按申请人、角色、权限码与状态筛选，缺省按提交时间降序 |
| `permission-requests.candidates` | `access.permission-requests.create` 或 `.update` 其一（策略 `access.permission-request-candidates`） | 只读；申请弹窗的候选：这一侧启用的角色与可授予的权限（租户侧按套餐收窄，与 `permissions.list` 同一口径）。申请人不必能读角色列表与权限目录 |
| `permission-requests.create` | `access.permission-requests.create` | 申请人要是这一侧的在用账号；角色存在且启用、权限码在这一侧的目录里，期望失效时间晚于现在；同一对象只能有一条待审批的申请（`ACCESS_CONFLICT_013`） |
| `permission-requests.update` | `access.permission-requests.update` | 修改自己待审批的申请并重新提交：撤回旧流程、按新内容重新发起，已有的审批意见作废 |
| `permission-requests.withdraw` | `access.permission-requests.withdraw` | 批量；撤回自己待审批的申请，同时撤回流程 |
| `permission-requests.set-status` | `access.permission-requests.status` | 批量；管理员把别人待审批的申请置为已驳回或已撤回（不能置为已批准，`ACCESS_VALIDATION_054`），同时撤回流程 |

权限码两侧都有，写操作进操作日志。

- **缺省流程**：当前分区第一次提交时建并发布「管理员审批」：角色 `admin` 审批，不含申请人自己。可在流程定义里新建版本调整。找不到审批人（如这一侧只有申请人一个管理员）即发起故障 `WORKFLOW_CONFLICT_101`，申请随之回滚。
- **审批与手工处理**：处理人不能处理自己的申请（`ACCESS_DENIED_009`），且要能授出申请的对象：角色要 `access.roles.assign`，权限要 `access.user-grants.grant`（`ACCESS_DENIED_010`）。
- **通过**：重核对象仍可用，角色经指派写入（过约束规则，拦截类违规即失败）、权限经直授写入；任一步失败即结束节点故障、申请保持待审批（`ACCESS_CONFLICT_015`），处理后在流程实例里重试。直授的权限立即生效，角色在申请人下次换发令牌时生效。
- **中止**：管理员取消、终止流程或发起人在审批中心撤回时，待审批的申请置为已撤回。
- **状态**：`Pending` 待审批、`Approved` 已批准、`Rejected` 已驳回、`Withdrawn` 已撤回。期望失效时间已过的另带 `isExpectedExpired` 标记，不是状态。
- **模仿会话**：对授权申请的写操作一律 409 `ACCESS_CONFLICT_016`：提交、修改重提、撤回、手工处理，以及这张审批单上的同意 / 驳回、转办、加签、发起人撤回，管理员的取消、终止、重试、挂起与恢复（批量里那一项记失败、流程不动）。其他业务的流程不拦。
- **Workflow 关掉**：列表照常、进度为空并标「审批未启用」（`approvalEnabled` 为假），提交与重新提交 503 `ACCESS_UNAVAILABLE_002`。
- **页面**：「授权申请」页（页面码 `access.delegations`、路由 `/access/delegations`，菜单权限 `access.permission-requests.read`）：「申请」页签在前，「委托」页签按 `access.delegations.read` 显示。审批中心里的单据组件是 `access/permission-request-approval/index`。

## 下一步

- [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [工作流](./workflow)
