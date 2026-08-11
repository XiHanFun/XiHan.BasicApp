# 打印模板

打印能力由独立模块 `modules/XiHan.BasicApp.Printing` 承载（后端）与 `frontend/src/modules/printing` + `@xihan/printing` 功能包承载（前端）：可视化设计打印模板、按编码解析、浏览器预览打印与本机静默直打。不需要此能力的部署可整体卸载模块（见仓库 README「卸载可选模块」）。

## 能力与边界

| 能力 | 依赖 | 说明 |
| --- | --- | --- |
| 模板设计器 | 仅浏览器 | hiprint 可视化拖拽设计，模板 JSON 存 `Sys_Print_Template` |
| 浏览器预览打印 | 仅浏览器 | `previewPrintByCode` 打开系统打印预览窗口，任何部署形态可用 |
| 打印机列表 / 静默直打 | **electron-hiprint 桌面客户端** | `listPrinters` / `refreshPrinters` / `directPrintByCode` 要求每台操作机安装并运行 [electron-hiprint](https://gitee.com/CcSimple/electron-hiprint) 客户端，前端经 WebSocket（默认 `http://localhost:17521`）连接；客户端未连接时这三条路径明确报错，不静默回退 |

::: warning 部署前提
静默直打是**企业内网桌面场景**的能力：纯 Web 部署（用户不装桌面客户端）只有设计器与浏览器预览可用。客户端连接地址与令牌经 Vite 环境变量 `VITE_HIPRINT_HOST` / `VITE_HIPRINT_TOKEN` 配置（省略时用内置默认值 `http://localhost:17521` / `vue-plugin-hiprint`）。该令牌随前端产物分发、可在浏览器端看到，仅是本机客户端的握手口令，不是保密凭据。
:::

## 作用域模型

模板分两种作用域，解析顺序由 `PrintTemplateScope` 控制（`Application/Services/PrintTemplateResolver.cs`）：

- **Tenant**：租户私有模板，`TenantId` = 当前租户；
- **Global**：平台全局模板（`TenantId = 0`），`AllowTenantUse = true` 时对业务租户开放解析；
- **Auto**（默认）：先找租户私有，未命中回退开放的全局模板。

同租户内启用模板的 `TemplateCode` 唯一；解析结果带分布式缓存（30 分钟，写路径经事务感知失效器整体清除）。

## 权限码

前缀 `print-template:`（`Domain/Permissions/PrintingPermissionCodes.cs`）：`read` / `create` / `update` / `status` / `delete` / `use` 可授予租户（模块角色权限种子默认授 `tenant_admin`）；`global-manage` 为平台专属（管理全局模板与租户开放状态，经 `SaasPlatformPermissions.ContributePlatformOnly` 登记进统一排除口径）。

## 数据源目录

模板可绑定「代码注册数据源」获得字段素材与样例数据：数据源在前端**编译期注册**（`src/modules/printing/setup.ts` 调 `registerPrintDataSource`），后端只存 `DataSourceCode` 字符串、不校验其存在。当前内置一个示例数据源 `system.print-demo`；业务数据源随业务模块开发时注册。由此推出的两条约束：

- 前端发版删除/改名数据源会使既有模板的绑定静默失效（打印时报「数据源未注册」）；
- 不绑数据源的「自由模板」按模板内标准字段绑定推断样例表单，不受上述影响。

## 已知限制

- `previewPrintByCode` 的完成信号仅表示预览调用已发起，无法得知用户在系统对话框中点了打印还是取消（上游引擎限制）；
- 直打依赖的打印机名单来自客户端所在机器，跨机器无共享；打印机偏好存浏览器 `localStorage`（按 用户×租户×模板 隔离，不上传服务端）。
