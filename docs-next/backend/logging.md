# 日志审计

本页说明八类日志的来源与内容、链路追踪、写入方式与保留清理，适用于后端开发者与运维、审计人员。

Auditing 包（Tier 2）负责日志的写入队列与查询面。八类日志里，七类是 Auditing 自己的按月分表，操作日志则是框架的操作审计。所有日志页都是只读的，权限码只有 `read`，平台与租户两侧都有。

## 八类日志

| 日志 | 表（按月分表，表名里的日期是当月 1 日） | 写入方 | 操作 / 权限码 |
| --- | --- | --- | --- |
| 访问日志 | `sys_access_log_{year}{month}{day}` | `AuditLogMiddleware`：每次调用的协议、目标、状态、耗时、主体、客户端 | `access-logs.*` / `auditing.access-logs.read` |
| 操作日志 | `audit_record`（框架 `Auditing.Database`） | 框架 `AuditMiddleware`，标了审计的操作才记 | `operation-logs.*` / `auditing.operation-logs.read` |
| 登录日志 | `sys_login_log_{year}{month}{day}` | Identity 在调用里登记 | `login-logs.*` / `auditing.login-logs.read` |
| 异常日志 | `sys_exception_log_{year}{month}{day}` | `AuditLogMiddleware`：未预期的失败与异常 | `exception-logs.*` / `auditing.exception-logs.read` |
| 数据变更日志 | `sys_diff_log_{year}{month}{day}` | SqlSugar 差异事件（`OnDiffLogEvent`），按调用缓冲、成功才落库 | `diff-logs.*` / `auditing.diff-logs.read` |
| 权限变更日志 | `sys_permission_change_log_{year}{month}{day}` | Access / Tenancy 在调用里登记，成功才落库 | `permission-change-logs.*` / `auditing.permission-change-logs.read` |
| 条件命中 | `sys_condition_hit_{year}{month}{day}` | 访问策略与权限条件 | `condition-hits.*` / `auditing.condition-hits.read` |
| 开放接口日志 | `sys_open_api_log_{year}{month}{day}` | OpenPlatform 的验签中间件，每次调用无论成败 | `open-api-logs.*` / `auditing.open-api-logs.read` |

每类都是 `list`（`POST :list`，分页筛选）与 `get`（`GET /{id}`）两个操作。

### 隔离

日志是租户库实体，严格按上下文隔离：平台主体看平台一侧（平台主体、匿名调用、找不到租户的登录都记在 `platform`），租户主体看本租户。要看某个租户的日志，先切换到该租户。

### 各类的内容

- 操作日志：操作 ID、本地化的描述（取操作的 `.title`，按调用方语言）、结果与错误码、耗时、登录名与会话，以及客户端 IP、浏览器、系统、设备、归属地（经 `IIpRegionResolver` 离线库）。
- 登录日志：事件 `Login`、`Refresh`、`Logout`、`SwitchTenant`、`ImpersonationStart` / `Stop`、`ChangePassword`、`TwoFactorChallenge` / `Enable` / `Disable` / `Reset`、`RoleActivation`；验证码错误、节流拒绝与第二步码错都作为失败的登录带错误码记下。
- 数据变更日志：类型 `Insert`、`Update`、`Delete`、`Restore`（软删除标记由真变假）；修改按主键配对前后值，敏感列打码；只动了时间戳一类列的修改不记；会话、个人偏好与账号安全不记；作业与种子的写入不记；属性可用 `[DiffLogIgnored]` 排除。
- 权限变更日志：`RoleGrant` / `RoleRevoke` / `RoleDeny`、`RoleAssign` / `RoleUnassign` / `RoleAssignmentWindow`、`RoleDataScope`、`RoleDelete`、`RoleInheritance`、`UserGrant` / `UserDeny` / `UserRevoke`、`UserDataScope`、`Delegation`、`FieldSecurity`、`ConditionChange`、`ConstraintViolation`、`EditionPermissions`、`TenantEdition`、`TenantOwner`。
- 条件命中：来源（访问策略 / 权限条件）、时机（认证前、认证后、登录、授权）、动作与是否拦截、请求属性快照。

## 链路追踪

`traces.timeline`（`GET`，`auditing.traces.read`）：按一个维度把多类日志串成时间线。

| 维度 | 值 |
| --- | --- |
| trace-id | 一次请求的全部日志 |
| 用户 / 登录名 / 会话 / 客户端地址 | 某人、某端在一段时间里的行为 |

可限定时间范围、日志类型与每类条数。操作日志没有会话与地址列，按其他日志里出现过的 trace-id 反查。客户端每个请求自带 `traceparent`，失败响应的 `traceId` 与之对应，排障时直接拿它查。前端的链路页另有流向图与时间分布。

## 写入

- 七类分表日志走有界队列（`BasicApp:Auditing:QueueCapacity` / `BatchSize` / `FlushIntervalSeconds`），后台按租户批量落库；共库与平台一侧直接写，独立库租户先经 `TenantShardScope` 绑定分片。
- 写日志不改变调用结果。
- 访问日志可整体关闭或按操作 ID 排除（`BasicApp:Auditing:AccessLog:Enabled` / `Exclude`）。
- 普通端点（上传、下载）由宿主的端点过滤器登记审计身份，授权阶段就被拒绝的普通端点调用不进访问日志。

## 保留清理

作业 `auditing.retention`（缺省每天 03:00 UTC）读系统参数 `auditing.retention-days`（没建即 `BasicApp:Auditing:DefaultRetentionDays`，180 天；不合法即失败），平台与每个租户逐批删除过期日志，再清操作审计表。

## 页面

`src/modules/auditing`：链路追踪与八个日志页（SchemaPage），详情抽屉、追踪跳转、时间线与流向图。

## 下一步

- [请求生命周期](./request-lifecycle) · [健康与可观测性](./health-observability) · [访问策略与权限条件](./access-policy)
