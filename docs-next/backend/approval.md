# 约束规则与审批

::: info 本页范围
约束规则（职责分离、互斥、基数、条件）与会话角色激活**已落地**（Access 包）。**审批**——授权申请提交即发起审批流、审批中心——依赖 Workflow，属于 **N3，规划中**，当前不可用。
:::

## 约束规则

### 五种类型

| 类型 | 判定时机 | 含义 |
| --- | --- | --- |
| `Ssd` 静态职责分离 | 指派、授予、设上级、委托、恢复到期指派 | 同一主体不能同时持有规则里的若干项（按继承展开） |
| `Dsd` 动态职责分离 | 会话激活角色时 | 可以同时持有，但同一会话里不能同时**激活** |
| `MutualExclusion` 互斥 | 同 SSD | 规则项两两互斥 |
| `Cardinality` 基数 | 同 SSD | 角色的成员数或主体的持有数上限 |
| `Condition` 条件 | 同 SSD | 满足 ABAC 条件时才允许（主体是被指派的用户） |

规则与规则项存在 `SysConstraintRule` / `SysConstraintRuleItem`，按侧（平台 / 租户）分开；启用的规则按侧缓存 60 秒、写入即失效，按优先级从大到小判定，规则带有效期。

### 违规处理

| 方式 | 行为 |
| --- | --- |
| `Deny` | 拦下变更，`ACCESS_CONFLICT_012` |
| `RequireApproval` | 拦下变更（审批未接入，**按拒绝处理**） |
| `Warning` | 照常执行，记权限变更日志（`ConstraintViolation`） |
| `Log` | 照常执行，只记录 |

`ConstraintEnforcer` **只拦这次变更新引入的违规**，不因为历史数据已经违规而拒绝无关的变更。

### 操作

| 操作 | 权限码 |
| --- | --- |
| `constraint-rules.list` / `get` | `access.constraint-rules.read` |
| `constraint-rules.create` / `update` / `delete` | `access.constraint-rules.create` / `update` / `delete` |
| `constraint-rules.set-status` | `access.constraint-rules.update` |

## 会话角色激活（DSD）

- `SysSessionRole` 记每个会话激活了哪些角色；登录、刷新、切换租户与模仿时由 `Contracts` 的 `ISessionRoleActivator` 选定激活集。
- **令牌只带激活的角色**，判定按激活集进行。
- 用户自助：`auth.session-roles` 查看、`auth.activate-roles` 换激活集（会话续用、刷新令牌轮换，违反 DSD 即 `ACCESS_CONFLICT_012`），激活集变化记登录日志（`RoleActivation`）。模仿态的会话不能切换。
- 管理：`session-roles.list` / `get`（`access.session-roles.read`）。

## 审批（规划中 · N3）

方案中的设计：

- **授权申请**：用户申请某个权限或角色，提交即发起审批流；审批通过后由 Access 执行授予。Access 的授权申请是 `Contracts` 里 `IWorkflowBusinessHandler` 业务接入契约的参照实现。
- **审批中心**：待我审批、我已审批、我发起的、抄送我的四个视图；办理同意、驳回、转办、加签，发起人可撤回。
- **约束规则的 `RequireApproval`**：接入审批后改为发起审批而不是直接拒绝。

这些随 Workflow 包在 N3 落地，见[工作流](./workflow)。

## 下一步

- [权限管理](./permission) · [访问策略与权限条件](./access-policy) · [工作流](./workflow)
