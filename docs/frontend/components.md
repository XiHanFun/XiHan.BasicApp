# 常用组件

除 [Schema 页面引擎](./schema-page) 之外的公共组件与全局挂件，按用途分组速查。组件源码在 `packages/components/`、`packages/layouts/` 与 `packages/iconify/`。

## 编辑器

| 组件 | 底层 | 用途 |
| --- | --- | --- |
| `RichTextEditor.vue` | Tiptap | 富文本。含 Link / Image / Highlight / TextAlign / Underline 等扩展 |
| `MdEditor.vue` | md-editor-v3 | Markdown |
| `JsonEditor.vue` | vue3-ts-jsoneditor | JSON 编辑与查看 |

通知公告正文用 Markdown 编辑器（文件库的 Markdown 预览也复用它）；富文本与 JSON 编辑器目前只在编辑器演示页 `packages/views/_core/editor-demo/` 使用。三者都以 `defineAsyncComponent` 懒加载，不进主依赖图。

## 表单增强

| 组件 | 用途 |
| --- | --- |
| `CronExpression.vue` | Cron 表达式：输入框 + 可视化弹窗，不必手写表达式。[任务调度](../backend/scheduling)页在用 |
| `XEditModal` + `xh-edit-form-grid` | **统一的新增/编辑弹窗外壳**：两列布局、10px 间距、11px 标签、无反馈占位，跨列用 `xh-span-2` |
| `IconPicker` | 图标选择器，可按需懒加载图标集 |
| `XLogoUpload`（`src/components/LogoUpload.vue`） / `XUserAvatar` | 图片上传与头像展示（内置 URL 解析与首字母兜底） |

::: danger 新增/编辑弹窗一律用 `XEditModal`
全仓已统一，**不要再手写 `NModal` 外壳**。统一外壳保证了弹窗尺寸、间距、标签宽度、按钮位置在所有页面一致，也让后续调整只改一处。
:::

## 消息中心 UI

三个层次（`~/layouts/basic/` 与 `src/views/message/`）：

| 层 | 组件 | 行为 |
| --- | --- | --- |
| **顶部横幅** | `NotificationBanner.vue` + `use-banner-notices.ts` | 数据由服务端按有效期、角色/部门定向过滤后下发；按严重度 + 优先级取前 3 条轮播（5s，悬停暂停）。关闭记忆按公告 id 存 localStorage（30 天清理），后台重发即新 id、自然重现 |
| **强制阅读 + 登录弹窗** | `NotificationGate.vue` | 未读必读公告以遮罩拦截、逐条「我已阅读」（最高优先级）；清空后再逐条弹出普通登录后公告 |
| **通知中心页** | `src/views/message/notification/` | 发布、定向、统计等运营闭环 |

业务设计见 [后端手册 · 消息通知](../backend/messaging)。

## 全局挂件

挂在 `App.vue` 上、全应用生效：

| 组件 | 说明 |
| --- | --- |
| `LockScreen.vue` | 锁屏遮罩。后端返回 **`423`** 时拉起——身份仍有效，解锁而非重新登录 |
| `AppWatermark.vue` | 水印 |
| `DynamicIsland.vue` | 灵动岛：全局操作反馈（进行中/成功/失败、进度环、岛内按钮、服务端任务进度） |

全局搜索（`AppGlobalSearch.vue`）挂在布局顶栏工具条上，`Ctrl/Cmd + K` 唤起。

## 文件与图片

| 工具 | 说明 |
| --- | --- |
| `useAvatarUrl` | 头像 URL 解析（fileId → 预签名 URL，直链原样；空值/失败返回空串，由 `XUserAvatar` 首字母兜底） |
| `toAbsoluteFileUrl` | 文件 URL 绝对化 |
| `SchemaImportDialog` | CSV 导入对话框：模板下载 → 解析 → 预校验 → 逐行创建 |

::: warning 本地存储的 URL 是根相对路径
本地存储返回 `/uploads/...`。前后端同源没问题，**线上不同源时必须拼上 `VITE_API_BASE_URL` 的 origin**，否则浏览器拿前端域名去请求而 404。上面两个工具已处理，自己拼 URL 时别忘了。
:::

## 图编辑

`packages/diagram` 基于 AntV X6 封装通用图编辑能力（工作流设计器在用）。应用侧只依赖本包导出的 `XDiagram` / `DiagramApi` / `DiagramData` / `registerVueShape`，不直接依赖 `@antv/x6`。

## 布局

`packages/layouts/` 提供整套布局外壳：侧边栏、顶栏、标签页、内容区、页脚。

::: tip 整屏页用 `h-full` 或 `XPageShell`
布局根是定高的、内容容器内部滚动（滚动源不是 `window`）。需要吸顶/吸底的整屏页直接用 `h-full` 或 `XPageShell` 即可，**不要再写 per-page 的 JS 定高 hack**。
:::

## 相关页面

- [Schema 驱动页面](./schema-page)：列表页引擎
- [布局与主题](./theme)：布局配置与主题定制
- [实时通信](./realtime)：消息中心的推送通道
- [字体图标](./icon)：图标用法与离线模式限制
