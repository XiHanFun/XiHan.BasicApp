# 布局与主题

壳层与设计令牌**整体从 5.x 搬入**（方案修订（十一）），所以布局、配色、偏好中心的行为与 5.x 一致。XiHan.UI 是组件与样式契约的真源。

## 双层令牌

| 层 | 位置 | 作用 |
| --- | --- | --- |
| 应用令牌 | `packages/design/variables.css` | HSL 分量形式的应用令牌，是视觉真源 |
| 令牌桥 | `packages/design/xihan-ui.css` | 把 XiHan.UI 的 `--xh-*` 令牌改写到应用令牌上，组件库随之换色 |
| Tailwind | `src/styles/index.css` | 层序 + `@theme` 把工具类映射到 `hsl(var(--x))` |

主题色、圆角、明暗、密度由 `useTheme` 与 `useHtmlStyle` 写到 `<html>`：`data-theme`、`data-density` 与 `--radius` / `--radius-card` 等变量，组件库跟着走。

## 约束

| 项 | 规则 |
| --- | --- |
| 圆角 | control 4px、surface 8px、overlay 12px；pill 只用于明确的胶囊语义 |
| 按压 | 离散按钮按下 120ms 缩放到 0.97，释放 200ms |
| 浮层 | 透明浮层只用 frosted 柔和模糊，**禁止 glass** |
| 取值 | 颜色、间距、圆角、阴影、动效只用语义令牌 |
| 裸 px | 按文件计数的白名单（`scripts/px-allowlist.json`），只许减不许增；新文件不进白名单。描边 1px / 2px、`0px`、`9999px` 不计 |
| 令牌 | 引用的 `--xh-*` 必须存在于当前组件库版本（`pnpm token:check`） |
| 风格 | 管理端紧凑、任务导向，不在控制台页面用营销式大标题或装饰性 Hero |

## 布局

`packages/layouts/basic` 提供管理端壳：侧栏、顶栏、标签栏、面包屑、内容区、页脚。偏好中心可切换布局形态、侧栏宽度、标签样式、内容宽度等；`registerShellExtension` 是模块向壳层注入能力的扩展点。

壳层的常驻能力：

| 能力 | 说明 |
| --- | --- |
| 全局搜索 | 命令面板式，`Ctrl / ⌘ + K`，拼音首字母直达页面 |
| 收藏 | 页面收藏，跨端同步 |
| 分屏 | 两个页面左右并排 |
| 通知中心 | 顶栏，读收件箱，接 `ReceiveNotification` |
| 灵动岛 | 网络 / 实时连接状态、后台任务进度（`TaskProgress`） |
| 锁屏 | 调 `auth.lock` / `auth.unlock`，服务端强制 |
| 水印 | 偏好中心开关 |

## 偏好与同步

亮 / 暗 / 跟随系统、主题色、圆角、密度、色弱 / 灰度、布局等偏好存在本地，并经用户设置（`user-settings`）跨端同步：开着同步时以后端那份为准，其他设备改了经 `UserSettingChanged` 实时应用。

## 无障碍与适配

视觉或交互改动要在真实浏览器里检查：亮 / 暗色、窄视口（手机宽度）、键盘导航与焦点、减少动态效果（reduced motion）。

## 下一步

- [常用组件](./components) · [字体图标](./icon) · [国际化与时区](./i18n)
