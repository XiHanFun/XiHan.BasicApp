# 功能预览

本页说明如何在本机查看 6.0 已落地的页面，适用于希望直观了解 6.0 功能的读者。

::: info 在线演示
线上演示环境 [basicapp.xihanfun.com](https://basicapp.xihanfun.com) 部署的是 5.x。6.0 预览版只能在本地运行查看，截图会在发布前补上。
:::

6.0 的管理端壳层（布局、主题、偏好中心、全局搜索、标签栏、分屏、通知中心）是从 5.x 原样搬入的，所以界面观感与 5.x 一致；不同在于页面背后的数据全部来自 6.0 的操作，菜单与按钮按 6.0 的权限码门控。

## 本地查看

按[快速开始](./getting-started)启动后端、执行 `seed`、启动前端，用 `superadmin` 登录，之后可按下表查看各页面：

| 想看 | 页面路由 | 看点 |
| --- | --- | --- |
| 工作台 | 登录后的首页 `/workbench/dashboard` | 公告轮播、今日统计与 10 个图表都来自 `workbench.widget-data`；拖拽排布后在另一个浏览器里实时同步 |
| 控制中心 | 顶栏入口（`_core/control-center`） | 可进入的租户与平台，切换后整页重建 |
| 用户管理 | `/identity/users` | SchemaPage 高级搜索、列设置、行速览；权限直授（授予 / 拒绝）、数据范围、强制下线、模仿登录 |
| 角色与授权 | `/access/roles` | 权限分配抽屉、Deny、有效期、上级角色、成员指派有效期 |
| 字段安全 | `/access/field-security` | 给某个角色把手机号设成部分脱敏，回到用户列表看效果 |
| 访问策略 | `/access/policies` | 六个模板、试运行、条件命中日志 |
| 租户 | `/tenancy/tenants`、`/tenancy/editions` | 开通三步、套餐白名单、配额与我的订阅 |
| 文件 | `/files/library` | 上传、秒传、预览（签名地址）、副本与主存储 |
| 导出中心 | 任一列表页导出 → `/files/export-center` | 后台按发起人身份逐页导出，完成后推送进度 |
| 日志链路 | `/auditing/traces` | 按 trace-id 串起访问、操作、登录、异常、数据变更日志的时间线与流向图 |
| 任务调度 | `/maintenance/jobs` | 代码登记的作业、Cron 构建器、立即执行与运行历史 |
| 接口文档 | `http://localhost:9710/scalar` | 全量与按包分组的文档，说明按 `Accept-Language` 本地化 |

亮暗主题、窄视口、键盘导航与减少动态效果都随壳层可用，可以在偏好中心里切换。

## 规划中的模块

工作流（流程设计器、审批中心）、打印模板、代码生成、在线聊天与 AI 助手在 6.0 里还没有实现，菜单里不会出现。它们的设计见后端手册里标注「规划中」的各页。

## 相关

- [功能清单](./features) · [快速开始](./getting-started) · [5.x 功能预览](https://basicapp.docs.xihanfun.com/preview)
