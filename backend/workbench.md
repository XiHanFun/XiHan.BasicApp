来源：https://basicapp.docs.xihanfun.com/backend/workbench

# 工作台

本页说明工作台小组件的提供者契约、操作、已登记的小组件与布局同步，适用于为能力包添加小组件的后端开发者。

Workbench 是 Tier 3 的可选模块（`BasicApp:Modules:Workbench:Enabled`，缺省开）：各包登记自己的小组件并负责取数，Workbench 只负责汇总、核权限与转发，自己没有表也没有权限码。

## 提供者契约

`Contracts/Workbench/IWorkbenchWidgetProvider`：

| 成员 | 说明 |
| --- | --- |
| `Widgets` | 本包登记的小组件（`WorkbenchWidget`）：键、取数形态与所需权限码（为空即登录就能看） |
| `GetAsync(key, query, context)` | 按键返回 `WorkbenchWidgetData`；权限已由工作台核过，键一定是本包登记的 |

取数形态 `WorkbenchWidgetKind`：

| 形态 | 数据 |
| --- | --- |
| `Metric` | 指标值与昨日值（前端算涨跌） |
| `Chart` | 类目加序列，或构成 |
| `List` | 条目（可带总数） |

数据只取当前一侧（租户主体是本租户、平台主体是平台一侧），与各包列表页同一口径。标题、图标、宽度、图表类型等呈现全在前端，后端只给数据。

## 操作

| 操作 | 说明 |
| --- | --- |
| `workbench.widgets` | `GET /api/workbench:widgets`，自助、只读：按调用方的权限列出可见的小组件 |
| `workbench.widget-data` | `GET /api/workbench:widget-data`，自助、只读：找不到键 `WORKBENCH_NOTFOUND_001`，没有权限 `WORKBENCH_DENIED_001`，再交给登记它的包取数 |

`workbench.widget-data` 带统计天数 `days`（1–366，缺省 30，趋势与构成类用）与查看者的时区偏移 `offset`（分钟，-840–840，前端取应用时区此刻的偏移）：「今日」「按日」这类统计按查看者的本地日计算（库里按 UTC 小时分组再换算）。

## 已登记的小组件（16 个）

| 包 | 键 | 形态 | 权限 |
| --- | --- | --- | --- |
| Auditing | `auditing.logins-today`、`auditing.requests-today`、`auditing.exceptions-today` | Metric | 对应日志的 `read` |
| Auditing | `auditing.login-trend`、`auditing.login-failures` | Chart | `auditing.login-logs.read` |
| Auditing | `auditing.request-kpi`、`auditing.request-heatmap`、`auditing.top-operations` | Chart | `auditing.access-logs.read` |
| Auditing | `auditing.exception-types` | Chart | `auditing.exception-logs.read` |
| Identity | `identity.online-users` | Metric | `identity.sessions.read` |
| Identity | `identity.session-devices` | Chart | `identity.sessions.read` |
| Files | `files.storage-mix` | Chart | `files.files.read` |
| Maintenance | `maintenance.job-runs` | Chart | `maintenance.job-runs.read` |
| Notification | `notification.announcements` | List | —（本人收件箱里最近的几条） |
| Notification | `notification.deliveries` | Chart | `notification.emails.read`（短信那部分另要 `notification.sms.read`） |
| Workflow | `workflow.todo` | List | —（Workflow 开着才有） |

前端据此渲染公告轮播、今日统计与访问概况、在线设备、登录失败原因、登录趋势、存储占用、热门操作、访问时段、作业执行、异常类型、消息发送等图表；图表壳带取数中、出错与无数据的占位。

## 看板布局

不另建表，存放在 Identity 的用户设置中（`user-settings`，场景 `Page`、键 `workbench:widgets`），本地即时保存并跨端同步：同步开关在「偏好设置 › 同步 › 仪表盘看板」，开着时以后端那份为准，其他设备改了经 `UserSettingChanged` 实时应用，点「完成」且有改动才落地。

## 开关

```json
{ "BasicApp": { "Modules": { "Workbench": { "Enabled": false } } } }
```

关掉后宿主不装 `WorkbenchFeature`，菜单里没有工作台；前端把 `VITE_HOME_PATH` 留空，登录后落到第一个有权限的页面。各包的提供者照常登记，没有消费方。

## 为能力包添加小组件

1. 在包的 `Infrastructure/` 实现 `IWorkbenchWidgetProvider`，登记键（`<包>.<名>`）、形态与权限码；
2. 在 `Configure` 里 `context.Services.AddScoped<IWorkbenchWidgetProvider, XxxWidgets>()`；
3. 在前端 `src/modules/workbench` 里加呈现（标题、图标、尺寸、图表类型与文案）。

可选模块不改工作台：在模块的 `setup.ts` 里以 `registerWorkbenchWidget`（`~/stores`）登记列表型小组件——看板上的键、后端小组件键、标题 / 说明 / 空态文案键、图标、缺省宽度、所属页面与「查看全部」的路由；看板用同一个列表外观渲染（标题栏是总数与「查看全部」，正文是最近几条），排在内置小组件之后、进缺省看板，按所属页面是否下发决定显隐，模块关掉时随之隐藏。工作流的「我的待办」就是这样接的。

## 下一步

- [架构总览](./introduction) · [系统设置](./settings) · [前端框架简介](../frontend/introduction)
