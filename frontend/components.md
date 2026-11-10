来源：https://basicapp.docs.xihanfun.com/frontend/components

# 常用组件

本页列出前端常用的组件及其用途，适用于前端开发者。

组件统一来自 XiHan.UI（`@xihan-ui/vue` 等）。它是解剖式组件库：模板里的 `Xh*` 部件（如 `XhButton`、`XhDialogRoot`）由 Vite 插件 `XiHanUIResolver` 按需解析到具名导出，可以不写 import，摇树不受影响。组件 API、部件与令牌见 [XiHan.UI 文档](https://ui.docs.xihanfun.com)。

`packages/components`（`~/components`）是应用侧的薄封装：在 XiHan.UI 之上统一表单控件的尺寸、状态与文案，并提供列表引擎 SchemaPage。不引入其他组件库，也不复制第三方组件运行时；组件库的缺陷回 XiHan.UI 修。

## 列表与页面

| 组件 | 用途 |
| --- | --- |
| `SchemaPage` | Schema 驱动的列表页引擎，见 [Schema 驱动页面](./schema-page) |
| `XDataTable` | 不走 Schema 的次级表格（抽屉、弹窗、面板里）：缺省 `md` 尺寸 + 列分隔线，传 `pagination` 出与列表页同款的分页底栏；`rowKey` 缺省 `basicId`，契约行用 `id` 时要显式传 |
| `XPageShell` | 整屏页面外壳：头部吸顶、内容区内部滚动 |
| `XEditModal` | 新增 / 编辑弹窗：给 `form-id` 后保存钮成为表单的提交钮，校验通过才走表单的 `@submit`；打开时焦点落到第一个控件 |
| `XFieldLabel` | 字段名，可带 `help`：弹出层里针对单个字段的说明收进字段名后面的问号提示 |
| `XHelpTip` | 问号提示本体（偏好设置各项、`XFieldLabel` 都用它） |
| `renderTag(label, tone)` | 状态列等的标签渲染 |

字段级错误不由弹窗自动处理：页面把 `ApiError.errors` 存起来，用 `XhFieldRoot :invalid` + `XhFieldErrorText` 显示在对应字段下。

## 表单控件

`XInput`、`XNumberInput`、`XSelect`、`XCombobox`、`XCascader`、`XTreeSelect`、`XDatePicker`、`XDateRangePicker`、`XTagsInput`、`XSlider`、`XSegmented`、`XColorPicker`：统一尺寸、校验状态与占位文案。日期控件收发浏览器本地时区的时间戳（毫秒）。

## 授权与选择

| 组件 | 用途 |
| --- | --- |
| `XPermissionTransfer` / `XGrantTransfer` | 权限分配穿梭框（角色、用户直授、套餐）：条目来自权限目录 `permissions.list`，按 `groupPermissions` 分组 |
| `XTree` | 树（数据范围里的部门、缓存键、生成代码的文件） |
| `XUserAvatar` | 用户头像：值是文件 ID 时换签名地址，没有头像时显示首字母 |
| `XFileRefUpload` | 业务表单里的文件引用上传：上传到文件库，字段只存文件 ID |

## 其他组件

| 组件 | 用途 |
| --- | --- |
| `Icon`、`IconPicker` | 图标与图标选择器，见[字体图标](./icon) |
| `XCodeEditor` | 代码编辑器（monaco，异步加载）：只读预览与模板编辑 |
| `XMdEditor` / `XContentEditorField` | Markdown 编辑器（md-editor-v3，异步加载）与带摘要、字数的正文编辑字段（通知公告正文） |
| `XJsonBlock` | JSON 只读查看 |
| `XChartInView` | 进入视口才挂载的图表壳（工作台） |
| `XPopconfirm`、`XTooltip`、`XDropdown`、`XIconButton` | 交互小件 |

公共组件里没有富文本编辑器与 JSON 编辑器：正文用 Markdown 编辑器，代码与模板用 `XCodeEditor`。

以下组件不从 `~/components` 导出，按文件路径引用（`~/components/common/<文件>.vue`）：

| 组件 | 用途 |
| --- | --- |
| `CronExpression` | Cron 构建器（作业的新建与改计划），配合 `jobs.validate-schedule` 预览执行时间 |
| `LocaleSwitcher`、`TimezoneSwitcher` | 区域与时区切换（顶栏、悬浮工具栏、偏好设置） |
| `LockScreen`、`DynamicIsland`、`AppWatermark` | 锁屏、灵动岛、水印，挂在 `App.vue` |
| `SyncStatusBadge`、`PageLoader` | 设置同步状态、页面加载占位 |

## 写法约定

- 应用自有组件显式 import（`import { XEditModal } from '~/components'`），只有 XiHan.UI 的 `Xh*` 可由插件自动解析；
- 模板里 `xh-` 开头的小写标签是设计器的 Web Components（`vite.config.ts` 的 `isCustomElement`），组件库部件一律写 PascalCase；
- 弹窗、抽屉、浮层只用 frosted；玻璃拟态只给命令面板、登录与注册页的卡片和右上角工具条、灵动岛、标签总览遮罩、控制中心面板、页面切换的加载遮罩，统一用 `packages/design/ui.css` 的 `.app-glass` / `--app-glass-*`；
- 颜色、间距、圆角只用令牌，不写裸 px。

## 下一步

- [Schema 驱动页面](./schema-page) · [布局与主题](./theme) · [字体图标](./icon)
