# 工作台（6.0 新增）

本页说明工作台小组件的提供者契约、操作、已登记的小组件与布局同步，适用于为能力包添加小组件的后端开发者。

5.x 的工作台后端为空实现，小组件的数据在前端写死；6.0 把它做成 Tier 3 的 Workbench 包：各包登记自己的小组件并负责取数，Workbench 只负责汇总与转发。它是第一个用 `BasicApp:Modules:<名>:Enabled` 开关的可选模块。

## 提供者契约

`Contracts/Workbench/IWorkbenchWidgetProvider`：

| 成员 | 说明 |
| --- | --- |
| `Widgets` | 本包登记的小组件：键、取数形态与所需权限码 |
| 取数方法 | 按键返回数据；权限已由工作台核过，键一定是本包登记的 |

取数形态：

| 形态 | 数据 |
| --- | --- |
| `Metric` | 指标值与昨日值（前端算涨跌） |
| `Chart` | 类目加序列，或构成 |
| `List` | 条目 |

数据只取当前一侧（租户主体是本租户、平台主体是平台一侧），与各包列表页同一口径。标题、图标、宽度、图表类型等呈现全在前端，后端只给数据。

## 操作

| 操作 | 说明 |
| --- | --- |
| `workbench.widgets` | 自助、只读：按调用方的权限列出可见的小组件 |
| `workbench.widget-data` | 自助、只读：先核权限（`WORKBENCH_DENIED_001`）、找不到键（`WORKBENCH_NOTFOUND_001`），再交给登记它的包取数 |

请求带查看者的时区偏移：「今日」「按小时」这类统计按查看者的本地日计算（库里按 UTC 小时分组再换算）。

## 已登记的小组件（15 个）

| 包 | 键 | 形态 | 权限 |
| --- | --- | --- | --- |
| Auditing | `auditing.logins-today`、`auditing.requests-today`、`auditing.exceptions-today` | Metric | 对应日志的 read |
| Auditing | `auditing.login-trend`、`auditing.login-failures` | Chart | `auditing.login-logs.read` |
| Auditing | `auditing.request-kpi`、`auditing.request-heatmap`、`auditing.top-operations` | Chart | `auditing.access-logs.read` |
| Auditing | `auditing.exception-types` | Chart | `auditing.exception-logs.read` |
| Identity | `identity.online-users` | Metric | `identity.sessions.read` |
| Identity | `identity.session-devices` | Chart | `identity.sessions.read` |
| Files | `files.storage-mix` | Chart | `files.files.read` |
| Maintenance | `maintenance.job-runs` | Chart | `maintenance.job-runs.read` |
| Notification | `notification.announcements` | List | — |
| Notification | `notification.deliveries` | Chart | `notification.emails.read` / `notification.sms.read` |

前端据此渲染公告轮播、今日统计与访问概况、在线设备、登录失败原因、登录趋势、存储占用、热门操作、访问时段、作业执行、异常类型、消息发送等图表；图表壳带取数中、出错与无数据的占位。

## 看板布局

不另建表，与 5.x 相同，存放在 Identity 的用户设置中（`user-settings`，场景 Page、键 `workbench:widgets`）：开着同步以后端那份为准，其他设备改了经 `UserSettingChanged` 实时应用，点「完成」且有改动才落地。

## 开关

```json
{ "BasicApp": { "Modules": { "Workbench": { "Enabled": false } } } }
```

关掉后宿主不装 `WorkbenchFeature`，菜单里没有工作台；前端把 `VITE_HOME_PATH` 留空，登录后落到第一个有权限的页面。

## 为能力包添加小组件

1. 在包的 `Infrastructure/` 实现 `IWorkbenchWidgetProvider`，登记键（`<包>.<名>`）、形态与权限码；
2. 在 `Configure` 里把提供者登记进容器；
3. 在前端 `src/modules/workbench` 里加呈现（标题、图标、尺寸、图表类型与文案）。

## 下一步

- [架构总览](./introduction) · [系统设置](./settings) · [前端框架简介](../frontend/introduction)
