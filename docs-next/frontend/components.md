# 常用组件

组件统一来自 **XiHan.UI**（`@xihan-ui/vue` 等）。它是解剖式组件库：模板里的 `Xh*` 标签（如 `XhButton`、`XhDialogRoot`）由 Vite 插件自动映射到具名导出，不必逐个 import，摇树不受影响。组件 API、部件与令牌见 [XiHan.UI 文档](https://ui.docs.xihanfun.com)。

`packages/components` 是从 5.x 搬入的**薄封装**：在 XiHan.UI 之上统一表单控件的尺寸、状态与文案，并提供列表引擎。不得引入 Naive UI 或复制第三方组件运行时；组件库的缺陷回 XiHan.UI 修。

## 列表与页面

| 组件 | 用途 |
| --- | --- |
| `SchemaPage` | Schema 驱动的列表页引擎，见 [Schema 驱动页面](./schema-page) |
| `XDataTable` | 表格（SchemaPage 内部也用它）；缺省 md 尺寸 + 列分隔线，弹窗里的表格与列表页同款 |
| `XPageShell` | 非列表页的页面外壳 |
| `XEditModal` | 新增 / 编辑弹窗：表单字段**都要有可见标签**，后端返回的字段级 `errors` 挂回对应字段 |
| `renderTag` | 状态列等的标签渲染 |

## 表单控件

`XInput`、`XNumberInput`、`XSelect`、`XCombobox`、`XCascader`、`XTreeSelect`、`XDatePicker`、`XDateRangePicker`、`XTagsInput`、`XSlider`、`XSegmented`、`XColorPicker`：统一尺寸、校验状态与占位文案的封装。

## 授权与选择

| 组件 | 用途 |
| --- | --- |
| `XPermissionTransfer` / `XGrantTransfer` | 权限分配：按包、资源分组（`groupPermissions`），数据来自生成的 `permissionDefinitions` |
| `XTree` | 树（部门、菜单） |
| `XUserAvatar` | 用户头像（签名地址） |

## 其他

| 组件 | 用途 |
| --- | --- |
| `CronExpression` | Cron 构建器（任务调度的新建与改计划），配合 `jobs.validate-schedule` 预览 |
| `IconPicker`（`~/iconify`） | 图标选择器 |
| `XChartInView` | 进入视口才渲染的图表壳（工作台） |
| `XJsonBlock` | JSON 查看 |
| `XPopconfirm`、`XTooltip`、`XDropdown`、`XIconButton` | 交互小件 |
| `LockScreen`、`DynamicIsland`、`AppWatermark`、`LocaleSwitcher`、`TimezoneSwitcher` | 壳层用 |

## 尚未搬入

- Markdown 编辑器（5.x 的通知公告正文编辑）；
- 流程设计器与打印设计器的接线（`packages/designers`、`packages/printing`），随 N3 的 Workflow、Printing。

## 写法约定

- 应用自有组件**显式 import**（`import { XEditModal } from '~/components'`），只有 XiHan.UI 的 `Xh*` 由插件自动解析；
- 弹窗、抽屉、浮层的透明效果只用 frosted；
- 颜色、间距、圆角只用令牌，不写裸 px。

## 下一步

- [Schema 驱动页面](./schema-page) · [布局与主题](./theme) · [字体图标](./icon)
