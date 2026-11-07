# 组织架构

本页说明 Organization 包（Tier 1，依赖 Identity 与 Tenancy）提供的部门、岗位与部门成员，适用于后端开发者。三者都是租户库实体：每个租户一套，平台一侧没有组织架构。

## 部门

| 实体 | 说明 |
| --- | --- |
| `SysDepartment` | 编码（租户内唯一，删了可重建）、名称、类型（集团、总部、公司、分公司、事业部、中心、小组等 16 种，缺省部门）、上级、负责人、排序、状态、联系电话、邮箱、地址、说明 |
| `SysDepartmentHierarchy` | 闭包表：每对「祖先—后代」一行（含自身），带深度 |

闭包表让「部门及其下级」成为一次索引查询，数据范围的 `DepartmentAndChildren`、字段安全的部门规则、通知的定向投递都依赖它。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `departments.tree` | `organization.departments.read` | 部门树（`GET`，声明只读），带直接成员数，不分页。`keyword` 不分大小写匹配编码、名称、电话、邮箱、地址与说明，只留命中的部门，上级没命中的当作顶层 |
| `departments.get` | `organization.departments.read` | 详情 |
| `departments.create` | `organization.departments.create` | 创建并挂到上级（为空即顶层） |
| `departments.update` | `organization.departments.update` | 改名称、类型、负责人、排序、联系方式等，不改上级 |
| `departments.move` | `organization.departments.update` | 移到新上级：整棵子树重算闭包，拒绝移到自己或子孙之下 |
| `departments.delete` | `organization.departments.delete` | 批量；还有子部门或成员的逐项 `ORGANIZATION_CONFLICT_002`，删除即软删并摘除闭包行 |

负责人是本租户的用户（`leaderUserId`）。前端编辑弹窗按关键字远程检索用户（需要 `identity.users.read`，看不了用户时这一栏不显示）。

## 岗位

`SysPosition`：编码（租户内唯一）、名称、排序、启停、说明。操作 `positions.create / get / list / update / delete`，权限 `organization.positions.*`。

## 部门成员

`SysUserDepartment`：用户 × 部门（同一用户同一部门一条），带岗位、是否主部门、工号、职级、入职时间（UTC）、部门内排序与备注。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `memberships.list` | `organization.memberships.read` | 按部门、用户或岗位筛选，可按主部门筛选、按排序 / 创建时间 / 入职时间排序，按数据范围收窄；不支持关键字（用户名在平台库，分库时不能联表） |
| `memberships.create` | `organization.memberships.create` | 用户须在当前租户（注册在这里或[外部成员](./multi-tenancy#外部成员)）；用户的第一个归属自动是主部门，显式设主时其他归属取消主标记 |
| `memberships.update` | `organization.memberships.update` | 改岗位、主部门、工号、职级、入职时间、备注 |
| `memberships.delete` | `organization.memberships.delete` | 批量移出 |

加入与移出部门时记领域事件 `basicapp.organization.membership-changed`（`Contracts` 的 `MembershipChanged`：用户、部门、加入还是移出、发生时间），随写入进发件箱；改岗位、主部门等不发。Chat 据此同步部门群成员，见[在线聊天](./chat#部门群同步)。

`departments.get`、`departments.tree`、`positions.get`、`positions.list` 与 `memberships.list` 同时是 MCP / AI 工具。

## 使用方

| 使用方 | 用法 |
| --- | --- |
| Access 数据范围 | 本部门 / 本部门及下级 / 自定义部门 |
| Access 字段安全 | 部门目标的规则认所在部门及其上级 |
| Access 条件属性 | `subject.departmentcodes`、`subject.maindepartmentcode`、`subject.positioncodes` |
| Identity 用户列表 | 按部门筛选（含下级），经 `Contracts` 的 `IRecipientSource` |
| Notification | 按部门定向投递（含下级） |
| Workflow | 组织类受理人，经下面的组织目录 |

## 组织目录

`Contracts` 的 `IOrganizationDirectory`（Organization 实现）给别的包按编码取部门成员、部门负责人、岗位任职，按用户取主部门往上的部门链，以及选人用的部门与岗位清单。只读调用所在租户、只取启用的部门与岗位（不含下级部门），平台一侧为空；不做权限与数据范围判定，候选人要不要再与可见的在用账号求交由调用方定。

## 演示数据

`BasicApp:Seed:EnableDemoData` 打开时，种子 `organization.demo` 在演示租户 `demo` 里写部门 `hq → rd / mkt / hr`、岗位 `gm / manager / engineer / specialist` 与负责人，并给演示成员排主部门任职（流程演示要用）：

| 账号 | 部门 / 岗位 | 负责人 |
| --- | --- | --- |
| `admin` | 总部 / 总经理 | 总部 |
| `manager` | 研发部 / 经理 | 研发部 |
| `lead` | 研发部 / 工程师 | — |
| `hr` | 人事部 / 专员 | — |

只首写：租户里已有部门即跳过。

## 前端

`src/modules/organization`：部门页是 SchemaPage 树表（关键字搜索、详情抽屉里的成员维护；编辑时改了上级，保存后调 `departments.move`），岗位页是 SchemaPage 列表。成员列表显示岗位、工号、职级与入职日期（按本地日期显示）。搜索时上级部门的候选与详情里的子部门仍取整棵树。

## 下一步

- [数据权限与字段安全](./data-permission) · [多租户 SaaS](./multi-tenancy)
