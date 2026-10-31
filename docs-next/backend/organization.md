# 组织架构

Organization 包（Tier 1，依赖 Identity 与 Tenancy）管部门、岗位与部门成员。三者都是**租户库实体**：每个租户一套，平台一侧没有组织架构。

## 部门

| 实体 | 说明 |
| --- | --- |
| `SysDepartment` | 编码（租户内唯一）、名称、上级、负责人、排序、状态、说明 |
| `SysDepartmentHierarchy` | 闭包表：每对「祖先—后代」一行，带深度 |

闭包表让「部门及其下级」成为一次索引查询，数据范围的 `DepartmentAndChildren`、字段安全的部门规则、通知的定向投递都依赖它。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `departments.tree` | `organization.departments.read` | 整棵树（`GET`，声明只读；也进 MCP / AI 工具） |
| `departments.get` | `organization.departments.read` | 详情 |
| `departments.create` | `organization.departments.create` | 创建并挂到上级（为空即顶层） |
| `departments.update` | `organization.departments.update` | 改名称、负责人、排序等 |
| `departments.move` | `organization.departments.update` | 移到新上级：整棵子树重算闭包，拒绝移到自己或子孙之下 |
| `departments.delete` | `organization.departments.delete` | 批量删除，摘除闭包行 |

**负责人**是本租户的用户（`leaderUserId`）。前端编辑弹窗按关键字远程检索用户（需要 `identity.users.read`，看不了用户时这一栏不显示）。

## 岗位

`SysPosition`：编码（租户内唯一）、名称、排序、启停、说明。操作 `positions.create / get / list / update / delete`，权限 `organization.positions.*`；`positions.get` / `list` 也是 MCP / AI 工具。

## 部门成员

`SysUserDepartment`：用户 × 部门，带岗位、是否主部门、工号、职级、入职时间（UTC）与部门内排序。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `memberships.list` | `organization.memberships.read` | 按部门（可含下级）列成员，**按数据范围收窄** |
| `memberships.create` | `organization.memberships.create` | 用户注册地必须是当前租户；用户的第一个归属自动是主部门 |
| `memberships.update` | `organization.memberships.update` | 改岗位、主部门、工号、职级、入职时间 |
| `memberships.delete` | `organization.memberships.delete` | 批量移出 |

## 被谁用

| 使用方 | 怎么用 |
| --- | --- |
| Access 数据范围 | 本部门 / 本部门及下级 / 自定义部门 |
| Access 字段安全 | 部门目标的规则认所在部门及其上级 |
| Access 条件属性 | `subject.departmentcodes`、`subject.maindepartmentcode`、`subject.positioncodes` |
| Identity 用户列表 | 按部门筛选（含下级），经 `Contracts` 的 `IRecipientSource` |
| Notification | 按部门定向投递 |

## 演示数据

`BasicApp:Seed:EnableDemoData` 打开时，种子 `organization.demo` 在演示租户 `demo` 里写部门 `hq → rd / mkt`、岗位 `manager / engineer`，并把租户管理员放进总部。

## 前端

`src/modules/organization`：部门页是 SchemaPage 树表（成员抽屉、移动），岗位页是普通列表。成员列表显示岗位、工号、职级与入职日期（按本地日期显示）。

## 下一步

- [数据权限与字段安全](./data-permission) · [多租户 SaaS](./multi-tenancy)
