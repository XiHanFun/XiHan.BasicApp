# 约束规则与审批

本页说明约束规则、会话角色激活与授权申请，适用于后端开发者与权限配置人员。

::: info 本页范围
约束规则（职责分离、互斥、基数、条件）、会话角色激活与授权申请都在 Access 包。授权申请的审批走 Workflow（审批中心见[工作流](./workflow#待办与审批中心)），Workflow 关掉时授权申请照常列出、不能提交。
:::

## 约束规则

### 五种类型

| 类型 | 判定时机 | 含义 |
| --- | --- | --- |
| `Ssd` 静态职责分离 | 指派、授予、设上级、委托、恢复到期指派 | 同一主体不能同时持有规则里的若干项（按继承展开） |
| `Dsd` 动态职责分离 | 会话激活角色时 | 可以同时持有，但同一会话里不能同时激活 |
| `MutualExclusion` 互斥 | 同 SSD | 规则项两两互斥 |
| `Cardinality` 基数 | 同 SSD | 角色的成员数或主体的持有数上限 |
| `Condition` 条件 | 同 SSD | 满足 ABAC 条件时才允许（主体是被指派的用户） |

规则与规则项存在 `SysConstraintRule` / `SysConstraintRuleItem`，按侧（平台 / 租户）分开；启用的规则按侧缓存 60 秒、写入即失效，按优先级从大到小判定，规则带有效期。

### 违规处理

| 方式 | 行为 |
| --- | --- |
| `Deny` | 拦下变更，`ACCESS_CONFLICT_012` |
| `RequireApproval` | 拦下变更（审批未接入，按拒绝处理） |
| `Warning` | 照常执行，记权限变更日志（`ConstraintViolation`） |
| `Log` | 照常执行，只记录 |

`ConstraintEnforcer` 只拦这次变更新引入的违规，不因为历史数据已经违规而拒绝无关的变更。

### 操作

| 操作 | 权限码 |
| --- | --- |
| `constraint-rules.list` / `get` | `access.constraint-rules.read` |
| `constraint-rules.create` / `update` / `delete` | `access.constraint-rules.create` / `update` / `delete` |
| `constraint-rules.set-status` | `access.constraint-rules.update` |

## 会话角色激活（DSD）

- `SysSessionRole` 记每个会话激活了哪些角色；登录、刷新、切换租户与模仿时由 `Contracts` 的 `ISessionRoleActivator` 选定激活集。
- 令牌只带激活的角色，判定按激活集进行。
- 用户自助：`auth.session-roles` 查看、`auth.activate-roles` 换激活集（会话续用、刷新令牌轮换，违反 DSD 即 `ACCESS_CONFLICT_012`），激活集变化记登录日志（`RoleActivation`）。模仿态的会话不能切换。
- 管理：`session-roles.list` / `get`（`access.session-roles.read`）。

## 授权申请

用户为自己申请一个角色或一个权限码（二选一，照 5.x 的界面；都给是 `ACCESS_VALIDATION_056`，都不给是 `051`），提交即在同一个事务里发起审批业务 `permission-request` 的流程；审批通过即按期望的有效期指派角色、直授权限，驳回或中止即完结。它是 Contracts 审批业务接入（`IWorkflowBusinessHandler`）的参照实现，表 `sys_permission_request` 在租户库（平台一侧租户为 `platform`）。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `permission-requests.list` / `get` | `access.permission-requests.read` | 这一侧全部的申请，带审批进度；关键字匹配申请人（登录名、显示名，经用户目录在这一侧按登录名取前 200 个，含停用的账号）、申请原因与备注，可按申请人、角色、权限码与状态过滤 |
| `permission-requests.create` | `.create` | 申请人要是这一侧的在用账号；角色存在且启用、权限码在这一侧的目录里，期望失效时间晚于现在；同一对象只能有一条待审批的申请 |
| `permission-requests.update` | `.update` | 修改自己待审批的申请并重新提交：撤回旧流程、按新内容重新发起 |
| `permission-requests.withdraw` | `.withdraw` | 批量；撤回自己待审批的申请，同时撤回流程 |
| `permission-requests.set-status` | `.status` | 批量；管理员手工驳回或撤回别人待审批的申请（不能置为已批准、不能处理自己的、要能授出申请的对象），同时撤回流程 |

- **缺省流程**：当前分区第一次提交时建并发布「管理员审批」（角色 `admin`，不含申请人自己），可在流程定义里新建版本调整。找不到审批人（如这一侧只有申请人一个管理员）即发起故障 `WORKFLOW_CONFLICT_101`，申请随之回滚。
- **审批**：审批人不能审批自己的申请，且要能授出申请的对象（角色要 `access.roles.assign`，权限要 `access.user-grants.grant`）。通过时重核对象仍可用，角色经指派（过约束规则，拦截类违规即失败）、权限经直授写入，任一步失败即结束节点故障、申请保持待审批（`ACCESS_CONFLICT_015`），处理后在流程实例里重试。直授的权限立即生效，角色在申请人下次换发令牌时生效。
- **中止**：管理员取消、终止流程或发起人在审批中心撤回时，待审批的申请置为已撤回。
- **状态**：待审批、已批准、已拒绝、已撤回；5.x 的「已过期」从未写入过，6.0 去掉，列表照 5.x 按期望失效时间算出「已过期」标记。
- **模仿会话**：模仿会话里对授权申请的写操作一律 409 `ACCESS_CONFLICT_016`（6.0 新加，5.x 没有）：提交、修改重提、撤回、手工处理，以及这张审批单上的同意 / 驳回、转办、加签、发起人撤回，管理员的取消、终止、重试、挂起与恢复（批量里那一项记失败、流程不动，不会出现流程已中止而申请还待审批）。其他业务的流程不拦。5.x 那套「授出模仿登录权限的限制」没有搬，6.0 的手工指派与直授也没有这项检查。
- **Workflow 关掉**：列表照常、进度为空并标「审批未启用」，提交与重新提交 503 `ACCESS_UNAVAILABLE_002`。
- **页面**：「授权申请」页（页面码与路由仍是 `access.delegations` / `/access/delegations`，菜单权限 `access.permission-requests.read`）：「申请」页签在前，「委托」页签按 `access.delegations.read` 显示。审批中心里的单据组件是 `access/permission-request-approval/index`。
- 约束规则的「需审批」照 5.x 等同拒绝，不改为发起审批。

## 下一步

- [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [工作流](./workflow)
